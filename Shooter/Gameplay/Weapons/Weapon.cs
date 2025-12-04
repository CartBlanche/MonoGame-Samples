using System.Numerics;
using Shooter.Core.Components;
using Shooter.Core.Services;

namespace Shooter.Gameplay.Weapons
{
    /// <summary>
    /// Base class for all weapons in the game.
    /// Provides common functionality for firing mechanics, ammo management, and reloading.
    /// </summary>
    public abstract class Weapon
    {
        // Weapon Properties
        public string Name { get; protected set; }
        public float Damage { get; protected set; }
        public float FireRate { get; protected set; } // Rounds per second
        public int MagazineSize { get; protected set; }
        public int MaxAmmo { get; protected set; }
        public float ReloadTime { get; protected set; }
        public float Range { get; protected set; }
        public string? FireSound { get; set; } // Audio path for weapon fire sound

        // Current State
        public int CurrentAmmoInMag { get; protected set; }
        public int CurrentReserveAmmo { get; protected set; }
        public bool IsReloading { get; protected set; }
        public bool CanFire => !IsReloading && CurrentAmmoInMag > 0 && _currentTime >= _nextFireTime;

        // Internal Tracking
        protected float _nextFireTime = 0f;
        protected float _reloadStartTime = 0f;
        protected float _currentTime = 0f;

        protected Weapon(string name, float damage, float fireRate, int magazineSize, int maxAmmo, float reloadTime, float range)
        {
            Name = name;
            Damage = damage;
            FireRate = fireRate;
            MagazineSize = magazineSize;
            MaxAmmo = maxAmmo;
            ReloadTime = reloadTime;
            Range = range;

            // Start fully loaded
            CurrentAmmoInMag = MagazineSize;
            CurrentReserveAmmo = MaxAmmo - MagazineSize;
        }

        /// <summary>
        /// Update the weapon state (handle reload completion, etc.)
        /// </summary>
        public virtual void Update(Core.Components.GameTime gameTime)
        {
            _currentTime = (float)gameTime.TotalGameTime.TotalSeconds;

            // Check if reload has completed
            if (IsReloading && _currentTime >= _reloadStartTime + ReloadTime)
            {
                CompleteReload();
            }
        }

        /// <summary>
        /// Attempt to fire the weapon. Returns true if the weapon fired successfully.
        /// </summary>
        public virtual bool TryFire(Vector3 origin, Vector3 direction)
        {
            if (!CanFire)
                return false;

            // Consume ammo
            CurrentAmmoInMag--;

            // Set next fire time based on fire rate
            _nextFireTime = _currentTime + (1f / FireRate);

            // Play fire sound
            if (!string.IsNullOrEmpty(FireSound))
            {
                var audioService = ServiceLocator.Get<IAudioService>();
                audioService?.PlaySound(FireSound, origin, 0.7f);
            }

            // Perform the actual firing logic (implemented by derived classes)
            OnFire(origin, direction);

            return true;
        }

        /// <summary>
        /// Override this method to implement specific firing behavior (raycast, projectile spawn, etc.)
        /// </summary>
        protected abstract void OnFire(Vector3 origin, Vector3 direction);

        /// <summary>
        /// Start reloading the weapon
        /// </summary>
        public virtual void StartReload()
        {
            // Can't reload if already reloading or magazine is full or no reserve ammo
            if (IsReloading || CurrentAmmoInMag == MagazineSize || CurrentReserveAmmo <= 0)
                return;

            IsReloading = true;
            _reloadStartTime = _currentTime;
        }

        /// <summary>
        /// Complete the reload process
        /// </summary>
        protected virtual void CompleteReload()
        {
            int ammoNeeded = MagazineSize - CurrentAmmoInMag;
            int ammoToReload = System.Math.Min(ammoNeeded, CurrentReserveAmmo);

            CurrentAmmoInMag += ammoToReload;
            CurrentReserveAmmo -= ammoToReload;

            IsReloading = false;
        }

        /// <summary>
        /// Cancel an ongoing reload
        /// </summary>
        public virtual void CancelReload()
        {
            IsReloading = false;
        }

        /// <summary>
        /// Add ammo to the weapon's reserve
        /// </summary>
        public void AddAmmo(int amount)
        {
            CurrentReserveAmmo = System.Math.Min(CurrentReserveAmmo + amount, MaxAmmo - MagazineSize);
        }

        /// <summary>
        /// Get the total ammo (magazine + reserve)
        /// </summary>
        public int GetTotalAmmo()
        {
            return CurrentAmmoInMag + CurrentReserveAmmo;
        }
    }
}
