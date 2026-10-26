using System;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Physics;
using Shooter.Scene;

namespace Shooter.Gameplay.Weapons
{
    /// <summary>
    /// Port of PlayerWeaponsManager: weapon slots, put-up/put-down switching, aiming (socket move + FOV zoom), walking bob,
    /// recoil, shooting. The weapon socket is kept in UNITY camera-local coordinates (z forward) exactly like the script and
    /// converted when read (<see cref="SocketLocal"/>), so every formula is a line-for-line copy.
    /// Not ported: reload input (the Blaster reloads automatically), weapon pickups / extra slots content, IsPointingAtEnemy
    /// (needs enemies), audio, layer assignment (the weapon is drawn by its own camera pass already).
    /// </summary>
    public class PlayerWeaponsManager
    {
        public enum WeaponSwitchState { Up, Down, PutDownPrevious, PutUpNew }

        // Inspector values (PlayerWeaponsManager component)
        public float BobFrequency = 10f, BobSharpness = 10f, DefaultBobAmount = 0.05f, AimingBobAmount = 0.02f;
        public float RecoilSharpness = 50f, MaxRecoilDistance = 0.5f, RecoilRestitutionSharpness = 10f;
        public float AimingAnimationSpeed = 10f, DefaultFov = 60f, WeaponFovMultiplier = 1f, WeaponSwitchDelay = 1f;
        public float AimingRotationMultiplier = 0.4f;

        private readonly PlayerController _player;
        private readonly PlayerSettings _playerSettings;
        private readonly ProjectileManager _projectiles;
        private readonly ProjectileSettings _projectileSettings;
        private readonly CollisionWorld _world;
        private readonly Random _rng = new Random(12345);

        private readonly WeaponController[] _slots = new WeaponController[9];
        private readonly Vector3 _muzzleInRootMG;
        private readonly Vector3 _defaultPos, _aimingPos, _downPos;   // Unity camera-local

        private float _time;
        private float _weaponBobFactor;
        private Vector3 _lastCharacterPosition;
        private Vector3 _mainLocal, _bobLocal, _recoilLocal, _accumulatedRecoil;   // Unity camera-local
        private float _timeStartedSwitch;
        private WeaponSwitchState _switchState = WeaponSwitchState.Down;
        private int _switchNewIndex;
        private float _fov;
        private Vector3 _lastMuzzleWorld;
        private Vector3 _muzzleVelocity;
        private float _lastDt = 1f / 60f;

        public bool IsAiming { get; private set; }
        public int ActiveWeaponIndex { get; private set; }
        public WeaponSwitchState SwitchState { get { return _switchState; } }
        public float Fov { get { return _fov; } }
        public float WeaponFov { get { return _fov * WeaponFovMultiplier; } }
        public int ShotsFired { get; private set; }

        /// <summary>PlayerWeaponsManager.IsPointingAtEnemy: the screen-centre ray hits something with Health.</summary>
        public bool IsPointingAtEnemy { get; private set; }
        public ProjectileSettings ProjectileSettings { get { return _projectileSettings; } }
        /// <summary>Tells whether a ray hit belongs to an enemy (Health in the collider's parents). ShooterGame sets it from the enemy hitbox tags; the default says no.</summary>
        public Func<CollisionWorld.RayHit, bool> IsEnemyHit = hit => false;

        public event Action<WeaponController> SwitchedToWeapon;
        /// <summary>A shot left the muzzle (muzzle world position + direction); VFX/audio hook.</summary>
        public event Action<Vector3, Vector3> ShotFired;

        public PlayerWeaponsManager(SceneFile scene, PlayerController player, PlayerSettings playerSettings, CollisionWorld world, ProjectileManager projectiles)
        {
            _player = player;
            _playerSettings = playerSettings;
            _world = world;
            _projectiles = projectiles;

            if (scene.Player.Components != null && scene.Player.Components.TryGetValue("PlayerWeaponsManager", out JsonElement c))
            {
                BobFrequency = JsonFields.Float(c, "BobFrequency", BobFrequency);
                BobSharpness = JsonFields.Float(c, "BobSharpness", BobSharpness);
                DefaultBobAmount = JsonFields.Float(c, "DefaultBobAmount", DefaultBobAmount);
                AimingBobAmount = JsonFields.Float(c, "AimingBobAmount", AimingBobAmount);
                RecoilSharpness = JsonFields.Float(c, "RecoilSharpness", RecoilSharpness);
                MaxRecoilDistance = JsonFields.Float(c, "MaxRecoilDistance", MaxRecoilDistance);
                RecoilRestitutionSharpness = JsonFields.Float(c, "RecoilRestitutionSharpness", RecoilRestitutionSharpness);
                AimingAnimationSpeed = JsonFields.Float(c, "AimingAnimationSpeed", AimingAnimationSpeed);
                DefaultFov = JsonFields.Float(c, "DefaultFov", DefaultFov);
                WeaponFovMultiplier = JsonFields.Float(c, "WeaponFovMultiplier", WeaponFovMultiplier);
                WeaponSwitchDelay = JsonFields.Float(c, "WeaponSwitchDelay", WeaponSwitchDelay);
            }
            if (scene.Player.Components != null && scene.Player.Components.TryGetValue("PlayerCharacterController", out JsonElement pc))
            {
                AimingRotationMultiplier = JsonFields.Float(pc, "AimingRotationMultiplier", AimingRotationMultiplier);
            }

            WeaponDto w = scene.Weapon;
            _defaultPos = UnitySocket(w.RootInCamera);
            _aimingPos = UnitySocket(w.AimingInCamera ?? w.RootInCamera);
            _downPos = UnitySocket(w.DownInCamera ?? w.RootInCamera);
            _muzzleInRootMG = new Vector3(w.MuzzleInRoot[0], w.MuzzleInRoot[1], w.MuzzleInRoot[2]);

            WeaponDto pj = scene.Projectile;
            _projectileSettings = ProjectileSettings.From(pj.Components["ProjectileStandard"], new Vector3(pj.TipInRoot[0], pj.TipInRoot[1], pj.TipInRoot[2]));

            // Start(): ActiveWeaponIndex = -1, Down; add the starting weapon; SwitchWeapon(true)
            ActiveWeaponIndex = -1;
            _fov = DefaultFov;
            AddWeapon(new WeaponController(WeaponSettings.From(w.Components["WeaponController"])));
            SwitchWeapon(true);
        }

        // MonoGame camera-space translation of a socket matrix -> Unity camera-local position (z negated back)
        private static Vector3 UnitySocket(float[] m)
        {
            return new Vector3(m[12], m[13], -m[14]);
        }

        /// <summary>Socket position relative to the main camera, MonoGame camera space (rotation is identity).</summary>
        public Vector3 SocketLocal
        {
            get
            {
                Vector3 u = _mainLocal + _bobLocal + _recoilLocal;
                return new Vector3(u.X, u.Y, -u.Z);
            }
        }

        public WeaponController ActiveWeapon { get { return GetWeaponAtSlotIndex(ActiveWeaponIndex); } }
        public WeaponController GetWeaponAtSlotIndex(int index) { return index >= 0 && index < _slots.Length ? _slots[index] : null; }

        /// <summary>True while the viewmodel should be drawn (a weapon is selected, even while it is being raised).</summary>
        public bool WeaponVisible { get { WeaponController w = ActiveWeapon; return w != null && w.IsWeaponActive; } }

        public bool AddWeapon(WeaponController weapon)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    weapon.ShowWeapon(false);
                    weapon.SpawnBullet = bullet => SpawnProjectile(weapon);
                    _slots[i] = weapon;
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ Update()

        public void Update(float dt, PlayerInput input)
        {
            _time += dt;
            _lastDt = dt;
            WeaponController active = ActiveWeapon;
            foreach (WeaponController w in _slots)
            {
                if (w != null) w.Update(_time, dt);
            }

            if (active != null && _switchState == WeaponSwitchState.Up)
            {
                IsAiming = input.AimHeld;
                bool fired = active.HandleShootInputs(input.FireDown, input.FireHeld, input.FireReleased);
                if (fired)
                {
                    ShotsFired++;
                    _accumulatedRecoil += new Vector3(0f, 0f, -1f) * active.Settings.RecoilForce;   // Vector3.back
                    if (_accumulatedRecoil.Length() > MaxRecoilDistance)
                    {
                        _accumulatedRecoil = Vector3.Normalize(_accumulatedRecoil) * MaxRecoilDistance;
                    }
                }
            }

            if (!IsAiming && (active == null || !active.IsCharging) && (_switchState == WeaponSwitchState.Up || _switchState == WeaponSwitchState.Down))
            {
                if (input.SwitchWeapon != 0)
                {
                    SwitchWeapon(input.SwitchWeapon > 0);
                }
                else if (input.SelectWeapon != 0 && GetWeaponAtSlotIndex(input.SelectWeapon - 1) != null)
                {
                    SwitchToWeaponIndex(input.SelectWeapon - 1);
                }
            }

            _player.AimingMultiplier = IsAiming ? AimingRotationMultiplier : 1f;

            // Pointing at enemy handling (Unity casts from the weapon camera, which sits on the main camera)
            IsPointingAtEnemy = false;
            if (active != null)
            {
                Matrix cam = _player.CameraWorld;
                Vector3 forward = Vector3.Normalize(Vector3.TransformNormal(Vector3.Forward, cam));
                if (_world.Raycast(cam.Translation, forward, 1000f, out CollisionWorld.RayHit hit) && IsEnemyHit(hit))
                {
                    IsPointingAtEnemy = true;
                }
            }
        }

        // ------------------------------------------------------------------ LateUpdate()

        public void LateUpdate(float dt)
        {
            UpdateWeaponAiming(dt);
            UpdateWeaponBob(dt);
            UpdateWeaponRecoil(dt);
            UpdateWeaponSwitching();

            // muzzle velocity for InheritedMuzzleVelocity (WeaponController.Update)
            Vector3 muzzle = MuzzleWorld(_player.CameraWorld);
            if (dt > 0f)
            {
                _muzzleVelocity = (muzzle - _lastMuzzleWorld) / dt;
            }
            _lastMuzzleWorld = muzzle;
        }

        public Vector3 MuzzleWorld(Matrix cameraWorld)
        {
            return Vector3.Transform(_muzzleInRootMG + SocketLocal, cameraWorld);
        }

        private void SetFov(float fov)
        {
            _fov = fov;
        }

        private void UpdateWeaponAiming(float dt)
        {
            if (_switchState != WeaponSwitchState.Up)
            {
                return;
            }
            WeaponController w = ActiveWeapon;
            if (IsAiming && w != null)
            {
                _mainLocal = Vector3.Lerp(_mainLocal, _aimingPos + w.Settings.AimOffsetUnity, Math.Min(1f, AimingAnimationSpeed * dt));
                SetFov(MathHelper.Lerp(_fov, w.Settings.AimZoomRatio * DefaultFov, Math.Min(1f, AimingAnimationSpeed * dt)));
            }
            else
            {
                _mainLocal = Vector3.Lerp(_mainLocal, _defaultPos, Math.Min(1f, AimingAnimationSpeed * dt));
                SetFov(MathHelper.Lerp(_fov, DefaultFov, Math.Min(1f, AimingAnimationSpeed * dt)));
            }
        }

        private void UpdateWeaponBob(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }
            Vector3 velocity = (_player.Position - _lastCharacterPosition) / dt;
            float movementFactor = 0f;
            if (_player.IsGrounded)
            {
                movementFactor = MathHelper.Clamp(velocity.Length() / (_playerSettings.MaxSpeedOnGround * _playerSettings.SprintSpeedModifier), 0f, 1f);
            }
            _weaponBobFactor = MathHelper.Lerp(_weaponBobFactor, movementFactor, Math.Min(1f, BobSharpness * dt));

            float bobAmount = IsAiming ? AimingBobAmount : DefaultBobAmount;
            float hBob = (float)Math.Sin(_time * BobFrequency) * bobAmount * _weaponBobFactor;
            float vBob = (((float)Math.Sin(_time * BobFrequency * 2f) * 0.5f) + 0.5f) * bobAmount * _weaponBobFactor;
            _bobLocal.X = hBob;
            _bobLocal.Y = Math.Abs(vBob);
            _lastCharacterPosition = _player.Position;
        }

        private void UpdateWeaponRecoil(float dt)
        {
            if (_recoilLocal.Z >= _accumulatedRecoil.Z * 0.99f)
            {
                _recoilLocal = Vector3.Lerp(_recoilLocal, _accumulatedRecoil, Math.Min(1f, RecoilSharpness * dt));
            }
            else
            {
                _recoilLocal = Vector3.Lerp(_recoilLocal, Vector3.Zero, Math.Min(1f, RecoilRestitutionSharpness * dt));
                _accumulatedRecoil = _recoilLocal;
            }
        }

        private void UpdateWeaponSwitching()
        {
            float factor = WeaponSwitchDelay == 0f ? 1f : MathHelper.Clamp((_time - _timeStartedSwitch) / WeaponSwitchDelay, 0f, 1f);

            if (factor >= 1f)
            {
                if (_switchState == WeaponSwitchState.PutDownPrevious)
                {
                    WeaponController old = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (old != null) old.ShowWeapon(false);

                    ActiveWeaponIndex = _switchNewIndex;
                    factor = 0f;

                    WeaponController next = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    OnSwitched(next);
                    if (next != null)
                    {
                        _timeStartedSwitch = _time;
                        _switchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        _switchState = WeaponSwitchState.Down;
                    }
                }
                else if (_switchState == WeaponSwitchState.PutUpNew)
                {
                    _switchState = WeaponSwitchState.Up;
                }
            }

            if (_switchState == WeaponSwitchState.PutDownPrevious)
            {
                _mainLocal = Vector3.Lerp(_defaultPos, _downPos, factor);
            }
            else if (_switchState == WeaponSwitchState.PutUpNew)
            {
                _mainLocal = Vector3.Lerp(_downPos, _defaultPos, factor);
            }
        }

        // ------------------------------------------------------------------ switching

        public void SwitchWeapon(bool ascending)
        {
            int newIndex = -1;
            int closest = _slots.Length;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distance = DistanceBetweenSlots(ActiveWeaponIndex, i, ascending);
                    if (distance < closest)
                    {
                        closest = distance;
                        newIndex = i;
                    }
                }
            }
            SwitchToWeaponIndex(newIndex);
        }

        public void SwitchToWeaponIndex(int newIndex, bool force = false)
        {
            if (!(force || (newIndex != ActiveWeaponIndex && newIndex >= 0)))
            {
                return;
            }
            _switchNewIndex = newIndex;
            _timeStartedSwitch = _time;
            if (ActiveWeapon == null)
            {
                _mainLocal = _downPos;
                _switchState = WeaponSwitchState.PutUpNew;
                ActiveWeaponIndex = _switchNewIndex;
                OnSwitched(GetWeaponAtSlotIndex(_switchNewIndex));
            }
            else
            {
                _switchState = WeaponSwitchState.PutDownPrevious;
            }
        }

        private void OnSwitched(WeaponController w)
        {
            if (w != null) w.ShowWeapon(true);
            if (SwitchedToWeapon != null) SwitchedToWeapon(w);
        }

        private int DistanceBetweenSlots(int from, int to, bool ascending)
        {
            int d = ascending ? to - from : -(to - from);
            return d < 0 ? _slots.Length + d : d;
        }

        // ------------------------------------------------------------------ shooting

        private void SpawnProjectile(WeaponController weapon)
        {
            Matrix cam = _player.CameraWorld;
            Vector3 camPos = cam.Translation;
            Vector3 camForward = Vector3.TransformNormal(Vector3.Forward, cam);
            Vector3 muzzle = MuzzleWorld(cam);
            Vector3 direction = weapon.GetShotDirectionWithinSpread(camForward, _rng);
            var p = new ProjectileStandard(_projectileSettings, _world, muzzle, direction, _muzzleVelocity);
            _projectiles.Spawn(p, _lastDt, camPos, camForward);
            if (ShotFired != null) ShotFired(muzzle, direction);
        }
    }
}
