namespace Shooter.Gameplay.Combat
{
    /// <summary>Port of Game/Shared/Damageable.cs: a hitbox that forwards damage to the Health of its owner.</summary>
    public class Damageable
    {
        public float DamageMultiplier = 1f;
        public float SensibilityToSelfdamage = 0.5f;
        public Health Health { get; private set; }
        /// <summary>What "Health.gameObject == damageSource" compares against: the object that owns the Health.</summary>
        public object HealthOwner { get; private set; }

        public Damageable(Health health, object healthOwner, float damageMultiplier = 1f, float sensibilityToSelfdamage = 0.5f)
        {
            Health = health;
            HealthOwner = healthOwner;
            DamageMultiplier = damageMultiplier;
            SensibilityToSelfdamage = sensibilityToSelfdamage;
        }

        public void InflictDamage(float damage, bool isExplosionDamage, object damageSource)
        {
            if (Health == null)
            {
                return;
            }
            float total = damage;
            if (!isExplosionDamage)
            {
                total *= DamageMultiplier;   // skip the crit multiplier for explosions
            }
            if (damageSource != null && ReferenceEquals(HealthOwner, damageSource))
            {
                total *= SensibilityToSelfdamage;
            }
            Health.TakeDamage(total, damageSource);
        }
    }
}
