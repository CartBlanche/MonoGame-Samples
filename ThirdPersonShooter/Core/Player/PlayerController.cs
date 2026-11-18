using System;
using Microsoft.Xna.Framework;
using ThirdPersonShooter.Physics;

namespace ThirdPersonShooter
{
    /// <summary>What other systems (rendering, UI, enemies, part B animation/shooting) read from the player.</summary>
    public interface IPlayerView
    {
        Vector3 Position { get; }          // feet, current fixed tick
        Vector3 Velocity { get; }
        float FacingYaw { get; }           // heading (radians, XNA RotationY convention, 0 = world -Z)
        bool IsOnFloor { get; }
        bool IsOnAir { get; }              // airborne_time > MIN_AIRBORNE_TIME (player.gd:105)
        Vector2 Motion { get; }            // smoothed input motion (player.gd:87), part B's blend-space input
        float FadeAlpha { get; }           // fall fade (player_input.gd:101-104)
        Matrix ModelWorld(float alpha);    // interpolated model transform
        Vector3 RenderPosition(float alpha);
        event Action OnJump;
        event Action OnLand;
    }

    /// <summary>
    /// Kinematic capsule player, ticked at a fixed 60 Hz. Port of player.gd apply_input() (:86-178) minus animation/root motion/shooting.
    /// Part B seams: <see cref="DesiredHorizontalVelocity"/> is where root motion plugs in; <see cref="TempClip"/> is replaced by the AnimationTree port.
    /// </summary>
    public sealed class PlayerController : IPlayerView
    {
        readonly CharacterMotor _motor;
        readonly Vector3 _initialPosition;
        Vector3 _prevPos, _pos; Quaternion _prevFace, _face = Quaternion.Identity;
        Vector3 _vel; Vector2 _motion; float _airborne = 100f; bool _onFloor, _onAir;
        public float FadeAlpha { get; private set; }

        public event Action OnJump;
        public event Action OnLand;

        public Vector3 Position => _pos;
        public Vector3 Velocity => _vel;
        public bool IsOnFloor => _onFloor;
        public bool IsOnAir => _onAir;
        public Vector2 Motion => _motion;
        public float FacingYaw => YawOf(_face);
        public CharacterMotor Motor => _motor;
        /// <summary>Max Y reached since the last <see cref="ResetApex"/> (test aid).</summary>
        public float ApexY { get; private set; }

        public PlayerController(CollisionWorld world, Vector3 spawn, float spawnYaw, PlayerAnimator animator = null)
        {
            Animator = animator;
            _motor = new CharacterMotor(world, PlayerConstants.CapsuleHeight, PlayerConstants.CapsuleRadius,
                PlayerConstants.SlopeLimitDeg, PlayerConstants.StepOffset, PlayerConstants.SkinWidth) { Position = spawn };
            _initialPosition = spawn; _pos = _prevPos = spawn; ApexY = spawn.Y;
            _face = _prevFace = Quaternion.CreateFromAxisAngle(Vector3.Up, spawnYaw);
        }

        public void ResetApex() => ApexY = _pos.Y;

        int _handBone = -2;
        /// <summary>World position of the Marker3D ShootFrom (player.tscn:548-549: (0,0.4,0) under a BoneAttachment3D on hand.R), at the current tick pose.</summary>
        public Vector3 MuzzlePosition()
        {
            if (_handBone == -2) _handBone = Animator != null ? Animator.Model.BoneIndex("hand.R") : -1;
            if (_handBone < 0) return _pos + Vector3.Up * 1.2f + Vector3.Transform(Vector3.Forward, _face) * 0.6f;
            return (Matrix.CreateTranslation(0f, PlayerConstants.MuzzleForwardLocal, 0f) * Animator.Pose.BoneModel(_handBone) * ModelWorld(1f)).Translation;
        }

        static float YawOf(Quaternion q) { var f = Vector3.Transform(Vector3.Forward, q); return MathF.Atan2(-f.X, -f.Z); }

        /// <summary>Animation state machine (null = no horizontal motion; headless tests supply one built from a GPU-less model load).</summary>
        public PlayerAnimator Animator { get; }
        /// <summary>Aim mode (part B2 sets this and adds the aim facing/shoot branch of player.gd:118-135); only feeds the animator for now.</summary>
        public bool Aiming;
        Vector3 _rootMotion; // player.gd `root_motion`: model-space delta of the last grounded frame, kept while airborne (so jumps keep their run momentum)
        static readonly Quaternion ModelFront = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi); // glb front +Z vs heading -Z

        /// <summary>One fixed physics tick (player.gd apply_input). <paramref name="input"/> = (right-left, back-forward), <paramref name="camYaw"/> = camera view yaw (XNA RotationY).</summary>
        public void Tick(float dt, Vector2 input, float camYaw, bool jump, float aimPitch = 0f)
        {
            _prevPos = _pos; _prevFace = _face;
            _motion = Vector2.Lerp(_motion, input, PlayerConstants.MotionInterpolateSpeed * dt); // player.gd:87

            // flattened camera axes (player.gd:89-96); view direction = XNA Forward rotated by yaw
            Vector3 camF = new Vector3(-MathF.Sin(camYaw), 0, -MathF.Cos(camYaw));
            Vector3 camR = new Vector3(MathF.Cos(camYaw), 0, -MathF.Sin(camYaw));
            Vector3 target = camR * _motion.X + camF * -_motion.Y; // motion.y = back-forward

            // jump / in-air logic (player.gd:99-114)
            _airborne += dt;
            if (_onFloor)
            {
                if (_airborne > PlayerConstants.LandAirborneTime) OnLand?.Invoke();
                _airborne = 0;
            }
            _onAir = _airborne > PlayerConstants.MinAirborneTime;
            if (!_onAir && jump)
            {
                _vel.Y = PlayerConstants.JumpSpeed; _onAir = true; _airborne = PlayerConstants.MinAirborneTime;
                OnJump?.Invoke();
                if (Animator != null) Animator.JumpPressed = true;
            }

            // facing (player.gd:143-152, walking branch; aim branch is part B): slerp toward the movement direction
            if (!_onAir && Aiming) // player.gd:118-124 aim branch (facing only; shooting is B2): slerp toward the camera yaw
                _face = Quaternion.Slerp(_face, Quaternion.CreateFromAxisAngle(Vector3.Up, camYaw), Math.Min(1f, dt * PlayerConstants.RotationInterpolateSpeed));
            else if (!_onAir && target.Length() > 0.001f)
            {
                var to = Quaternion.CreateFromAxisAngle(Vector3.Up, MathF.Atan2(-target.X, -target.Z));
                _face = Quaternion.Slerp(_face, to, Math.Min(1f, dt * PlayerConstants.RotationInterpolateSpeed));
            }

            // animation tree + root motion (player.gd:78-84,131-163): h_velocity = (orientation * root_motion).origin / delta
            Vector3 h = Vector3.Zero;
            if (Animator != null)
            {
                Animator.MoveDir = _motion; Animator.OnAir = _onAir; Animator.VelocityY = _vel.Y; Animator.Aiming = Aiming; Animator.AimPitch = aimPitch;
                Animator.Update(dt);
                if (!_onAir) _rootMotion = Animator.RootMotion; // airborne: the stale root_motion keeps applying
                h = Vector3.Transform(Vector3.Transform(_rootMotion, ModelFront), _face) / dt;
            }
            _vel.X = h.X; _vel.Z = h.Z;
            _vel.Y -= PlayerConstants.Gravity * dt;                 // player.gd:164
            MoveAndSlide(dt);

            if (_pos.Y > ApexY) ApexY = _pos.Y;
            if (_pos.Y < PlayerConstants.RespawnY) { _pos = _initialPosition; _motor.Position = _pos; _vel = Vector3.Zero; _prevPos = _pos; } // player.gd:175-176
            FadeAlpha = _pos.Y < PlayerConstants.FallFadeY ? Math.Min((PlayerConstants.FallFadeY - _pos.Y) / 15f, 1f) : FadeAlpha * (1f - dt * 4f);
        }

        /// <summary>CharacterBody3D.move_and_slide equivalent: slide, then floor test, floor snap (0.1), reset vertical velocity on floor/ceiling.</summary>
        void MoveAndSlide(float dt)
        {
            bool wasOnFloor = _onFloor;
            Vector3 motion = _vel * dt;
            if (wasOnFloor && _vel.Y <= 0f) motion.Y = 0f; // stay put on the floor; the snap below handles descents (avoids a 60 Hz bob)
            Vector3 before = _motor.Position;
            _motor.Move(motion, preventClimbing: wasOnFloor);
            float movedY = _motor.Position.Y - before.Y;
            if (_vel.Y > 0f && movedY < motion.Y * 0.5f) _vel.Y = 0f; // head bump

            bool floor = false;
            if (_vel.Y <= 0f)
            {
                float snap = wasOnFloor ? PlayerConstants.SnapLength : 0.05f;
                if (_motor.CapsuleCast(Vector3.Down, snap + PlayerConstants.SkinWidth, out float d, out Vector3 n) && _motor.IsWalkable(n))
                {
                    floor = true;
                    if (wasOnFloor && d > PlayerConstants.SkinWidth) _motor.Position += Vector3.Down * (d - PlayerConstants.SkinWidth * 0.5f);
                }
            }
            if (floor) _vel.Y = 0f;
            _onFloor = floor;
            _pos = _motor.Position;
        }

        public Vector3 RenderPosition(float alpha) => Vector3.Lerp(_prevPos, _pos, alpha);

        /// <summary>Model world: glb front is +Z, heading uses -Z as forward, hence the 180 deg turn (see ThirdPersonShooterGame comment).</summary>
        public Matrix ModelWorld(float alpha) =>
            Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateFromQuaternion(Quaternion.Slerp(_prevFace, _face, alpha)) * Matrix.CreateTranslation(RenderPosition(alpha));

        /// <summary>Test aid: teleport.</summary>
        public void Teleport(Vector3 p) { _motor.Position = p; _pos = _prevPos = p; _vel = Vector3.Zero; _onFloor = false; _airborne = 100f; ApexY = p.Y; }
    }
}
