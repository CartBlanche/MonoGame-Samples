using System.Numerics;
using Shooter.Core.Entities;

namespace Shooter.Gameplay.Systems
{
    /// <summary>
    /// Types of damage that can be dealt.
    /// Different damage types can have different effects or resistances.
    /// </summary>
    public enum DamageType
    {
        Generic,        // Standard damage
        Bullet,         // Projectile damage from guns
        Explosion,      // Area of effect explosion damage
        Melee,          // Close-range physical damage
        Fall,           // Falling damage
        Environmental,  // Hazards like lava, spikes, etc.
        Fire,           // Burning damage over time
        Poison          // Toxic damage over time
    }

    /// <summary>
    /// Information about a damage event.
    /// Contains all details about who dealt damage, how much, and what type.
    /// Similar to Unity's DamageMessage or collision data.
    /// </summary>
    public struct DamageInfo
    {
        /// <summary>
        /// Amount of damage to deal
        /// </summary>
        public float Amount { get; set; }

        /// <summary>
        /// Type of damage being dealt
        /// </summary>
        public DamageType DamageType { get; set; }

        /// <summary>
        /// Entity that dealt the damage (can be null for environmental damage)
        /// </summary>
        public Entity? Attacker { get; set; }

        /// <summary>
        /// World position where damage was applied
        /// </summary>
        public Vector3 HitPoint { get; set; }

        /// <summary>
        /// Direction the damage came from (for knockback, hit reactions)
        /// </summary>
        public Vector3 HitDirection { get; set; }

        /// <summary>
        /// Normal of the surface that was hit (for effects)
        /// </summary>
        public Vector3 HitNormal { get; set; }

        /// <summary>
        /// Whether this damage can be blocked/absorbed
        /// </summary>
        public bool CanBeBlocked { get; set; }

        /// <summary>
        /// Whether this damage ignores armor/shields
        /// </summary>
        public bool IgnoresArmor { get; set; }

        /// <summary>
        /// Create basic damage info with just amount and type
        /// </summary>
        public static DamageInfo Create(float amount, DamageType type = DamageType.Generic, Entity? attacker = null)
        {
            return new DamageInfo
            {
                Amount = amount,
                DamageType = type,
                Attacker = attacker,
                CanBeBlocked = true,
                IgnoresArmor = false,
                HitPoint = Vector3.Zero,
                HitDirection = Vector3.Zero,
                HitNormal = Vector3.UnitY
            };
        }

        /// <summary>
        /// Create damage info with full details (for hitscan/projectile hits)
        /// </summary>
        public static DamageInfo CreateFromHit(
            float amount, 
            DamageType type, 
            Entity? attacker, 
            Vector3 hitPoint, 
            Vector3 hitDirection,
            Vector3 hitNormal)
        {
            return new DamageInfo
            {
                Amount = amount,
                DamageType = type,
                Attacker = attacker,
                HitPoint = hitPoint,
                HitDirection = Vector3.Normalize(hitDirection),
                HitNormal = hitNormal,
                CanBeBlocked = true,
                IgnoresArmor = false
            };
        }
    }
}
