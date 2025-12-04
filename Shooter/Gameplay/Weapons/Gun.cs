using System;
using System.Numerics;

namespace Shooter.Gameplay.Weapons
{
    /// <summary>
    /// Firing mode for guns (semi-automatic or fully automatic)
    /// </summary>
    public enum FiringMode
    {
        SemiAutomatic,  // One shot per trigger pull
        FullyAutomatic  // Continuous fire while trigger held
    }

    /// <summary>
    /// Standard gun implementation with configurable firing modes.
    /// Uses hitscan (raycast) for instant bullet hits.
    /// </summary>
    public class Gun : Weapon
    {
        public FiringMode FiringMode { get; set; }
        public float RecoilAmount { get; protected set; }
        public float Spread { get; protected set; } // Degrees of spread/inaccuracy

        // Events for external systems to hook into
        public event Action<Vector3, Vector3, float>? OnHitscanFired; // origin, direction, damage

        public Gun(
            string name,
            float damage,
            float fireRate,
            int magazineSize,
            int maxAmmo,
            float reloadTime,
            float range,
            FiringMode firingMode = FiringMode.SemiAutomatic,
            float recoilAmount = 0.5f,
            float spread = 0.0f)
            : base(name, damage, fireRate, magazineSize, maxAmmo, reloadTime, range)
        {
            FiringMode = firingMode;
            RecoilAmount = recoilAmount;
            Spread = spread;
        }

        /// <summary>
        /// Implement hitscan firing logic
        /// </summary>
        protected override void OnFire(Vector3 origin, Vector3 direction)
        {
            // Apply spread if configured
            Vector3 finalDirection = direction;
            if (Spread > 0f)
            {
                finalDirection = ApplySpread(direction, Spread);
            }

            // Normalize the direction
            finalDirection = Vector3.Normalize(finalDirection);

            // Fire the hitscan event (other systems like ProjectileSystem will handle the actual raycast)
            OnHitscanFired?.Invoke(origin, finalDirection, Damage);

            // Debug output
            Console.WriteLine($"{Name} fired! Ammo: {CurrentAmmoInMag}/{CurrentReserveAmmo}");
        }

        /// <summary>
        /// Apply random spread to the firing direction
        /// </summary>
        private Vector3 ApplySpread(Vector3 direction, float spreadDegrees)
        {
            Random random = new Random();
            
            // Convert spread from degrees to radians
            float spreadRadians = spreadDegrees * (float)(Math.PI / 180.0);

            // Generate random angles within the spread cone
            float randomAngle = (float)(random.NextDouble() * Math.PI * 2);
            float randomDistance = (float)(random.NextDouble() * spreadRadians);

            // Create perpendicular vectors for the spread
            Vector3 up = Vector3.UnitY;
            if (Math.Abs(Vector3.Dot(direction, up)) > 0.99f)
            {
                up = Vector3.UnitX;
            }

            Vector3 right = Vector3.Cross(direction, up);
            right = Vector3.Normalize(right);
            up = Vector3.Cross(right, direction);
            up = Vector3.Normalize(up);

            // Apply the spread
            float spreadX = (float)Math.Cos(randomAngle) * randomDistance;
            float spreadY = (float)Math.Sin(randomAngle) * randomDistance;

            Vector3 spreadDirection = direction + (right * spreadX) + (up * spreadY);
            return spreadDirection;
        }

        /// <summary>
        /// Create a default pistol preset
        /// </summary>
        public static Gun CreatePistol()
        {
            var pistol = new Gun(
                name: "Pistol",
                damage: 25f,
                fireRate: 3f,        // 3 rounds per second
                magazineSize: 12,
                maxAmmo: 120,
                reloadTime: 1.5f,
                range: 100f,
                firingMode: FiringMode.SemiAutomatic,
                recoilAmount: 0.3f,
                spread: 0.0f         // No spread for testing
            );
            pistol.FireSound = "Audio/SFX/Weapons/Blaster_Shot";
            return pistol;
        }

        /// <summary>
        /// Create a default assault rifle preset
        /// </summary>
        public static Gun CreateAssaultRifle()
        {
            var rifle = new Gun(
                name: "Assault Rifle",
                damage: 20f,
                fireRate: 10f,       // 10 rounds per second
                magazineSize: 30,
                maxAmmo: 300,
                reloadTime: 2.0f,
                range: 150f,
                firingMode: FiringMode.FullyAutomatic,
                recoilAmount: 0.5f,
                spread: 2.0f
            );
            rifle.FireSound = "Audio/SFX/Weapons/Shotgun_Shot";
            return rifle;
        }

        /// <summary>
        /// Create a default shotgun preset
        /// </summary>
        public static Gun CreateShotgun()
        {
            return new Gun(
                name: "Shotgun",
                damage: 15f,         // Per pellet (typically 8 pellets)
                fireRate: 1f,        // 1 round per second
                magazineSize: 8,
                maxAmmo: 80,
                reloadTime: 3.0f,
                range: 30f,
                firingMode: FiringMode.SemiAutomatic,
                recoilAmount: 1.5f,
                spread: 10.0f        // Wide spread
            );
        }

        /// <summary>
        /// Create enemy HoverBot "Eye Lazers" weapon (Unity specs)
        /// </summary>
        public static Gun CreateEnemyEyeLazers()
        {
            var weapon = new Gun(
                name: "Eye Lazers",
                damage: 15f,         // Moderate damage
                fireRate: 1.25f,     // 0.8s between shots = 1.25 shots/sec
                magazineSize: 999,   // Infinite ammo for enemies
                maxAmmo: 999,
                reloadTime: 0f,      // No reload for enemies
                range: 100f,
                firingMode: FiringMode.SemiAutomatic,
                recoilAmount: 0f,
                spread: 3.0f         // Some inaccuracy for gameplay balance
            );
            weapon.FireSound = "Audio/SFX/Weapons/Blaster_Shot";
            return weapon;
        }

        /// <summary>
        /// Create enemy Turret "Machine Gun" weapon (Unity specs)
        /// </summary>
        public static Gun CreateEnemyMachineGun()
        {
            var weapon = new Gun(
                name: "Machine Gun",
                damage: 10f,         // Lower damage but rapid fire
                fireRate: 16.67f,    // 0.06s between shots = ~16.67 shots/sec
                magazineSize: 999,   // Infinite ammo for enemies
                maxAmmo: 999,
                reloadTime: 0f,      // No reload for enemies
                range: 100f,
                firingMode: FiringMode.FullyAutomatic,
                recoilAmount: 0f,
                spread: 5.0f         // Higher spread for balance (very fast fire rate)
            );
            weapon.FireSound = "Audio/SFX/Weapons/Shotgun_Shot";
            return weapon;
        }
    }
}
