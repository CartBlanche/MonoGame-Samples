using System;

namespace Shooter.Gameplay.Combat
{
    /// <summary>Port of Game/Shared/Health.cs. The damage source is an opaque object (the player's Actor, an enemy, null for Kill()).</summary>
    public class Health
    {
        public float MaxHealth = 10f;
        public float CriticalHealthRatio = 0.3f;

        public event Action<float, object> OnDamaged;   // (true damage, source)
        public event Action<float> OnHealed;
        public event Action OnDie;

        public float CurrentHealth { get; set; }
        public bool Invincible { get; set; }
        public bool CanPickup() { return CurrentHealth < MaxHealth; }
        public float GetRatio() { return CurrentHealth / MaxHealth; }
        public bool IsCritical() { return GetRatio() <= CriticalHealthRatio; }

        private bool _isDead;

        public Health(float maxHealth, float criticalHealthRatio)
        {
            MaxHealth = maxHealth;
            CriticalHealthRatio = criticalHealthRatio;
            CurrentHealth = maxHealth;   // Start()
        }

        public void Heal(float healAmount)
        {
            float before = CurrentHealth;
            CurrentHealth = Math.Max(0f, Math.Min(MaxHealth, CurrentHealth + healAmount));
            float trueHeal = CurrentHealth - before;
            if (trueHeal > 0f && OnHealed != null)
            {
                OnHealed(trueHeal);
            }
        }

        public void TakeDamage(float damage, object damageSource)
        {
            if (Invincible)
            {
                return;
            }
            float before = CurrentHealth;
            CurrentHealth = Math.Max(0f, Math.Min(MaxHealth, CurrentHealth - damage));
            float trueDamage = before - CurrentHealth;
            if (trueDamage > 0f && OnDamaged != null)
            {
                OnDamaged(trueDamage, damageSource);
            }
            HandleDeath();
        }

        public void Kill()
        {
            CurrentHealth = 0f;
            if (OnDamaged != null)
            {
                OnDamaged(MaxHealth, null);
            }
            HandleDeath();
        }

        private void HandleDeath()
        {
            if (_isDead)
            {
                return;
            }
            if (CurrentHealth <= 0f)
            {
                _isDead = true;
                if (OnDie != null)
                {
                    OnDie();
                }
            }
        }
    }
}
