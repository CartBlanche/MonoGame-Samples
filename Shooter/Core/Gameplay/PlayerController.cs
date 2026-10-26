using System;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Physics;

namespace Shooter.Gameplay
{
    /// <summary>
    /// Port of Unity's PlayerCharacterController (Assets/FPS/Scripts/Gameplay). Update() mirrors its Update():
    /// kill height, ground check, landing, crouch, height lerp, then look + movement + Move + impact projection.
    /// Everything is in MonoGame world space (right-handed, Unity z negated). Yaw is kept in Unity degrees
    /// (positive = turn right/clockwise from above) and the camera pitch in Unity degrees (positive = look down),
    /// so values read from the Unity Inspector can be used as they are.
    /// Not ported yet (no health/weapons/audio): death handling beyond the flag, aiming slow-down (set
    /// <see cref="AimingMultiplier"/>), SFX, weapon bob.
    /// </summary>
    public class PlayerController
    {
        private const float JumpGroundingPreventionTime = 0.2f;
        private const float GroundCheckDistanceInAir = 0.07f;

        private readonly PlayerSettings _s;
        private readonly CharacterMotor _motor;
        private Health _health;

        private Vector3 _groundNormal = Vector3.Up;
        private Vector3 _latestImpactSpeed;
        private float _lastTimeJumped;
        private float _time;
        private float _targetHeight;
        private float _cameraLocalY;

        public Vector3 CharacterVelocity;
        public bool IsGrounded { get; private set; }
        public bool HasJumpedThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }
        public float YawDegrees { get; set; }
        public float CameraPitchDegrees { get; private set; }
        /// <summary>1 normally, AimingRotationMultiplier while aiming (set by the weapon manager later).</summary>
        public float AimingMultiplier = 1f;

        /// <summary>Raised on landing with the fall damage (0 when fall damage is off or the fall was soft).</summary>
        public event Action<float, float> Landed;   // (fall speed, damage)
        public event Action Jumped;
        /// <summary>A footstep sound is due (PlayerCharacterController: every 1 / frequency metres travelled on the ground).</summary>
        public event Action Footstep;
        private float _footstepDistanceCounter;
        public event Action Died;

        public PlayerController(CollisionWorld world, PlayerSettings settings)
        {
            _s = settings;
            _motor = new CharacterMotor(world, settings.CapsuleHeightStanding, settings.Radius, settings.SlopeLimit, settings.StepOffset, settings.SkinWidth);
            Respawn(settings.StartPosition, settings.StartYawDegrees);
        }

        /// <summary>
        /// The player's Health (Start: GetComponent, OnDie += OnDie). Without one (physics tests) the kill height sets IsDead directly and
        /// falls do no damage.
        /// </summary>
        public void BindHealth(Health health)
        {
            if (_health != null)
            {
                _health.OnDie -= OnDie;
            }
            _health = health;
            if (_health != null)
            {
                _health.OnDie += OnDie;
            }
        }

        private void OnDie()
        {
            IsDead = true;
            if (Died != null) Died();
        }

        public CharacterMotor Motor { get { return _motor; } }
        public Vector3 Position { get { return _motor.Position; } }
        public float Height { get { return _motor.Height; } }
        public float CameraLocalHeight { get { return _cameraLocalY; } }

        public void Respawn(Vector3 position, float yawDegrees)
        {
            _motor.Position = position;
            _motor.Height = _s.CapsuleHeightStanding;
            _targetHeight = _s.CapsuleHeightStanding;
            _cameraLocalY = _targetHeight * _s.CameraHeightRatio;   // UpdateCharacterHeight(force)
            YawDegrees = yawDegrees;
            CameraPitchDegrees = 0f;
            CharacterVelocity = Vector3.Zero;
            _latestImpactSpeed = Vector3.Zero;
            _groundNormal = Vector3.Up;
            _footstepDistanceCounter = 0f;
            IsGrounded = false;
            IsDead = false;
            IsCrouching = false;
            _lastTimeJumped = 0f;
            _time = 0f;
            _motor.Recover();   // enableOverlapRecovery: the baked start overlaps the floor
        }

        // yaw about +Y as seen from above in Unity (turn right = clockwise) -> MonoGame (z negated)
        public Vector3 Forward { get { return YawVector(YawDegrees, 0f, 1f); } }
        public Vector3 Right { get { return YawVector(YawDegrees, 1f, 0f); } }

        private static Vector3 YawVector(float yawDegrees, float localRight, float localForward)
        {
            float a = MathHelper.ToRadians(yawDegrees);
            float sin = (float)Math.Sin(a), cos = (float)Math.Cos(a);
            // Unity: right = (cos, 0, -sin), forward = (sin, 0, cos); z negated for MonoGame
            return new Vector3(localRight * cos + localForward * sin, 0f, localRight * sin - localForward * cos);
        }

        /// <summary>Camera-to-world matrix (looks down -Z), position = root + camera local height.</summary>
        public Matrix CameraWorld
        {
            get
            {
                Matrix rot = Matrix.CreateFromYawPitchRoll(-MathHelper.ToRadians(YawDegrees), -MathHelper.ToRadians(CameraPitchDegrees), 0f);
                return rot * Matrix.CreateTranslation(CameraPosition);
            }
        }

        public Vector3 CameraPosition { get { return _motor.Position + Vector3.Up * _cameraLocalY; } }

        // ------------------------------------------------------------------ Update()

        public void Update(float dt, PlayerInput input)
        {
            _time += dt;
            if (IsDead)
            {
                input = default(PlayerInput);   // GameFlowManager stops input once the game is ending
            }

            if (!IsDead && _motor.Position.Y < _s.KillHeight)
            {
                if (_health != null)
                {
                    _health.Kill();
                }
                else
                {
                    OnDie();
                }
            }

            HasJumpedThisFrame = false;

            bool wasGrounded = IsGrounded;
            GroundCheck();

            if (IsGrounded && !wasGrounded)
            {
                float fallSpeed = -Math.Min(CharacterVelocity.Y, _latestImpactSpeed.Y);
                float ratio = (fallSpeed - _s.MinSpeedForFallDamage) / (_s.MaxSpeedForFallDamage - _s.MinSpeedForFallDamage);
                float damage = 0f;
                if (_s.RecievesFallDamage && ratio > 0f)
                {
                    damage = MathHelper.Lerp(_s.FallDamageAtMinSpeed, _s.FallDamageAtMaxSpeed, Math.Min(1f, ratio));
                }
                if (damage > 0f && _health != null)
                {
                    _health.TakeDamage(damage, null);
                }
                if (Landed != null) Landed(fallSpeed, damage);
            }

            if (input.CrouchDown)
            {
                SetCrouchingState(!IsCrouching, false);
            }

            UpdateCharacterHeight(dt, false);

            HandleCharacterMovement(dt, input);
        }

        private void GroundCheck()
        {
            float chosen = IsGrounded ? (_motor.SkinWidth + _s.GroundCheckDistance) : GroundCheckDistanceInAir;

            IsGrounded = false;
            _groundNormal = Vector3.Up;

            if (_time >= _lastTimeJumped + JumpGroundingPreventionTime)
            {
                if (_motor.CapsuleCast(Vector3.Down, chosen, out float hitDistance, out Vector3 normal))
                {
                    _groundNormal = normal;
                    // only a valid ground if the normal points up and the slope is within the limit
                    if (normal.Y > 0f && _motor.IsWalkable(normal))
                    {
                        IsGrounded = true;
                        if (hitDistance > _motor.SkinWidth)
                        {
                            _motor.Move(Vector3.Down * hitDistance);   // snap to the ground
                        }
                    }
                }
            }
        }

        private void HandleCharacterMovement(float dt, PlayerInput input)
        {
            float rotationMultiplier = AimingMultiplier;

            // horizontal rotation (about the root's local Y)
            YawDegrees += input.LookX * _s.RotationSpeed * rotationMultiplier;

            // vertical camera rotation
            CameraPitchDegrees += input.LookY * _s.RotationSpeed * rotationMultiplier;
            CameraPitchDegrees = MathHelper.Clamp(CameraPitchDegrees, -89f, 89f);

            bool isSprinting = input.SprintHeld;
            if (isSprinting)
            {
                isSprinting = SetCrouchingState(false, false);
            }
            float speedModifier = isSprinting ? _s.SprintSpeedModifier : 1f;

            Vector3 worldMove = Right * input.Move.X + Forward * input.Move.Y;

            if (IsGrounded)
            {
                Vector3 target = worldMove * _s.MaxSpeedOnGround * speedModifier;
                if (IsCrouching)
                {
                    target *= _s.MaxSpeedCrouchedRatio;
                }
                float mag = target.Length();
                Vector3 dir = mag > 1e-6f ? target / mag : Vector3.Zero;
                target = DirectionReorientedOnSlope(dir, _groundNormal) * mag;

                CharacterVelocity = Vector3.Lerp(CharacterVelocity, target, Math.Min(1f, _s.MovementSharpnessOnGround * dt));

                if (input.JumpDown)
                {
                    if (SetCrouchingState(false, false))
                    {
                        CharacterVelocity = new Vector3(CharacterVelocity.X, 0f, CharacterVelocity.Z);
                        CharacterVelocity += Vector3.Up * _s.JumpForce;
                        _lastTimeJumped = _time;
                        HasJumpedThisFrame = true;
                        IsGrounded = false;
                        _groundNormal = Vector3.Up;
                        if (Jumped != null) Jumped();
                    }
                }

                // footsteps sound, then keep track of the distance travelled
                float chosenFootstepSfxFrequency = isSprinting ? _s.FootstepSfxFrequencyWhileSprinting : _s.FootstepSfxFrequency;
                if (_footstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
                {
                    _footstepDistanceCounter = 0f;
                    if (Footstep != null) Footstep();
                }
                _footstepDistanceCounter += CharacterVelocity.Length() * dt;
            }
            else
            {
                CharacterVelocity += worldMove * _s.AccelerationSpeedInAir * dt;

                float vertical = CharacterVelocity.Y;
                var horizontal = new Vector3(CharacterVelocity.X, 0f, CharacterVelocity.Z);
                float max = _s.MaxSpeedInAir * speedModifier;
                if (horizontal.LengthSquared() > max * max)
                {
                    horizontal = Vector3.Normalize(horizontal) * max;
                }
                CharacterVelocity = horizontal + Vector3.Up * vertical;

                CharacterVelocity += Vector3.Down * _s.GravityDownForce * dt;
            }

            Vector3 bottomBefore = _motor.BottomHemisphere(_motor.Position);
            Vector3 topBefore = _motor.TopHemisphere(_motor.Position, _motor.Height);
            _motor.Move(CharacterVelocity * dt, IsGrounded);

            // detect obstructions to adjust the velocity accordingly
            _latestImpactSpeed = Vector3.Zero;
            float speed = CharacterVelocity.Length();
            if (speed > 1e-6f &&
                _motor.World.CapsuleCast(bottomBefore, topBefore, _motor.Radius, CharacterVelocity / speed, speed * dt, out _, out Vector3 hitNormal))
            {
                _latestImpactSpeed = CharacterVelocity;
                CharacterVelocity = ProjectOnPlane(CharacterVelocity, hitNormal);
            }
        }

        /// <summary>Unity GetDirectionReorientedOnSlope: cross(n, cross(dir, up)).normalized == dir*n.y - up*dot(n, dir), handedness independent.</summary>
        private static Vector3 DirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 d = direction * slopeNormal.Y - Vector3.Up * Vector3.Dot(slopeNormal, direction);
            float len = d.Length();
            return len > 1e-6f ? d / len : Vector3.Zero;
        }

        private static Vector3 ProjectOnPlane(Vector3 v, Vector3 n)
        {
            return v - n * (Vector3.Dot(v, n) / Vector3.Dot(n, n));
        }

        private void UpdateCharacterHeight(float dt, bool force)
        {
            if (force)
            {
                _motor.Height = _targetHeight;
                _cameraLocalY = _targetHeight * _s.CameraHeightRatio;
            }
            else if (_motor.Height != _targetHeight)
            {
                float t = Math.Min(1f, _s.CrouchingSharpness * dt);
                _motor.Height = MathHelper.Lerp(_motor.Height, _targetHeight, t);
                _cameraLocalY = MathHelper.Lerp(_cameraLocalY, _targetHeight * _s.CameraHeightRatio, t);
            }
        }

        /// <summary>Returns false if standing up is blocked.</summary>
        private bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            if (crouched)
            {
                _targetHeight = _s.CapsuleHeightCrouching;
            }
            else
            {
                if (!ignoreObstructions && _motor.OverlapCapsule(_motor.Position, _s.CapsuleHeightStanding))
                {
                    return false;
                }
                _targetHeight = _s.CapsuleHeightStanding;
            }
            IsCrouching = crouched;
            return true;
        }
    }
}
