using System;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Weapons;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>
    /// Port of the Shared WeaponController as an enemy uses it (not physical bullets, automatic): ammo refills after AmmoReloadDelay,
    /// <see cref="HandleShootInputs"/> fires when there is ammo and DelayBetweenShots has passed. The weapon GameObject is turned towards
    /// the look position by EnemyController.OrientWeaponsTowards (forward = look - GunRoot position); the muzzle hangs at a fixed local z
    /// behind its pivot, so the shot starts there and flies along the weapon's forward.
    /// </summary>
    public class EnemyWeapon
    {
        public readonly WeaponSettings Settings;
        public float CurrentAmmo { get; private set; }
        public float LastTimeShot { get; private set; } = float.NegativeInfinity;
        public Vector3 Forward;   // world, MonoGame space
        public int ShotsFired { get; private set; }

        private readonly float _muzzleLocalZ;   // Unity local z of GunMuzzle (-0.026) times the weapon's world scale
        private readonly Func<Matrix> _weaponWorld, _gunRootWorld;
        private readonly Func<float> _time;

        public EnemyWeapon(WeaponSettings settings, Func<Matrix> weaponWorld, Func<Matrix> gunRootWorld, float muzzleLocalZ, Func<float> time)
        {
            Settings = settings;
            _weaponWorld = weaponWorld;
            _gunRootWorld = gunRootWorld;
            _muzzleLocalZ = muzzleLocalZ;
            _time = time;
            CurrentAmmo = settings.MaxAmmo;   // Awake: m_CurrentAmmo = MaxAmmo
        }

        public Vector3 GunRootWorld(Enemy e) { return _gunRootWorld().Translation; }
        public Vector3 PivotWorld(Enemy e) { return _weaponWorld().Translation; }

        /// <summary>Muzzle position: pivot + local z along the weapon's forward.</summary>
        public Vector3 MuzzleWorld(Enemy e) { return PivotWorld(e) + Forward * _muzzleLocalZ; }

        public void OrientTowards(Enemy e, Vector3 lookPosition)
        {
            Vector3 d = lookPosition - GunRootWorld(e);
            if (d.LengthSquared() > 1e-12f)
            {
                Forward = Vector3.Normalize(d);
            }
        }

        public void Update(float dt)
        {
            // UpdateAmmo
            if (Settings.AutomaticReload && LastTimeShot + Settings.AmmoReloadDelay < _time() && CurrentAmmo < Settings.MaxAmmo)
            {
                CurrentAmmo = MathHelper.Clamp(CurrentAmmo + Settings.AmmoReloadRate * dt, 0f, Settings.MaxAmmo);
            }
        }

        /// <summary>HandleShootInputs(false, true, false): holding the trigger. Returns true when a shot left the weapon (the caller spawns the projectiles).</summary>
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            switch (Settings.ShootType)
            {
                case WeaponShootType.Manual:
                    return inputDown && TryShoot();
                case WeaponShootType.Automatic:
                    return inputHeld && TryShoot();
                default:
                    return false;   // charge weapons are not used by enemies
            }
        }

        private bool TryShoot()
        {
            if (CurrentAmmo >= 1f && LastTimeShot + Settings.DelayBetweenShots < _time())
            {
                CurrentAmmo -= 1f;
                LastTimeShot = _time();
                ShotsFired++;
                return true;
            }
            return false;
        }
    }
}
