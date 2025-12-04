using System;
using Shooter.Core.Components;
using Shooter.Core.Services;
using Shooter.Gameplay.Systems;

namespace Shooter.Gameplay.Components
{
    /// <summary>
    /// Component that tracks entity health and handles damage/healing.
    /// Similar to Unity's Health component in many FPS games.
    /// 
    /// USAGE:
    /// - Attach to any entity that can take damage (player, enemies, destructibles)
    /// - Call TakeDamage() to apply damage
    /// - Subscribe to OnDeath event to handle entity death
    /// - Subscribe to OnDamaged event for hit reactions, sounds, effects
    /// </summary>
    public class Health : EntityComponent
    {
        // Health properties
        private float _maxHealth = 100f;
        private float _currentHealth = 100f;

        /// <summary>
        /// Maximum health this entity can have
        /// </summary>
        public float MaxHealth
        {
            get => _maxHealth;
            set
            {
                _maxHealth = Math.Max(0, value);
                // Clamp current health if it exceeds new max
                if (_currentHealth > _maxHealth)
                    _currentHealth = _maxHealth;
            }
        }

        /// <summary>
        /// Current health value
        /// </summary>
        public float CurrentHealth
        {
            get => _currentHealth;
            private set => _currentHealth = Math.Clamp(value, 0, _maxHealth);
        }

        /// <summary>
        /// Health as a percentage (0.0 to 1.0)
        /// </summary>
        public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;

        /// <summary>
        /// Is this entity currently alive?
        /// </summary>
        public bool IsAlive => _currentHealth > 0;

        /// <summary>
        /// Is this entity dead?
        /// </summary>
        public bool IsDead => _currentHealth <= 0;

        /// <summary>
        /// Can this entity be damaged? (for invincibility, respawn protection, etc.)
        /// </summary>
        public bool CanTakeDamage { get; set; } = true;

        /// <summary>
        /// Damage multiplier (for difficulty, armor, buffs, etc.)
        /// </summary>
        public float DamageMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Should this entity be destroyed when health reaches 0?
        /// </summary>
        public bool DestroyOnDeath { get; set; } = false;

        /// <summary>
        /// Time in seconds of invincibility after taking damage (for i-frames)
        /// </summary>
        public float InvincibilityDuration { get; set; } = 0f;

    // State tracking
        private bool _hasDied = false;
        private float _lastDamageTime = 0f;
        private float _currentGameTime = 0f;

        // Events
        /// <summary>
        /// Called when this entity takes damage. Parameters: (damageInfo, healthAfter)
        /// </summary>
        public event Action<DamageInfo, float>? OnDamaged;

        /// <summary>
        /// Called when this entity is healed. Parameters: (healAmount, healthAfter)
        /// </summary>
        public event Action<float, float>? OnHealed;

        /// <summary>
        /// Called when this entity dies. Parameter: (damageInfo that killed it)
        /// </summary>
        public event Action<DamageInfo>? OnDeath;

        /// <summary>
        /// Called when this entity respawns/revives
        /// </summary>
        public event Action? OnRespawn;

        public override void Initialize()
        {
            base.Initialize();
            _currentHealth = _maxHealth;
        }

        public override void Update(Core.Components.GameTime gameTime)
        {
            base.Update(gameTime);

            _currentGameTime = (float)gameTime.TotalGameTime.TotalSeconds;

            // Invincibility is handled in TakeDamage()
        }

        /// <summary>
        /// Apply damage to this entity
        /// </summary>
        /// <param name="damageInfo">Information about the damage</param>
        /// <returns>True if damage was applied, false if blocked/invincible</returns>
        public bool TakeDamage(DamageInfo damageInfo)
        {
            // Can't damage if dead or invincible
            if (IsDead || !CanTakeDamage)
                return false;

            // Check invincibility frames
            if (InvincibilityDuration > 0 && _currentGameTime - _lastDamageTime < InvincibilityDuration)
                return false;

            // Calculate actual damage
            float actualDamage = damageInfo.Amount;
            
            // Apply damage multiplier (unless damage ignores armor)
            if (!damageInfo.IgnoresArmor)
            {
                actualDamage *= DamageMultiplier;
            }

            // Apply damage
            float previousHealth = _currentHealth;
            CurrentHealth -= actualDamage;
            _lastDamageTime = _currentGameTime;

            Console.WriteLine($"[Health] {Owner?.Name} took {actualDamage:F1} damage ({_currentHealth:F1}/{_maxHealth:F1} HP remaining)");

            // Play damage sound (only for player)
            if (Owner?.Tag == "Player")
            {
                var audioService = ServiceLocator.Get<IAudioService>();
                audioService?.PlaySound("Audio/SFX/Player/Damage_Tick", null, 0.5f);
            }

            // Trigger damage event
            OnDamaged?.Invoke(damageInfo, _currentHealth);

            // Check for death
            if (_currentHealth <= 0 && !_hasDied)
            {
                Die(damageInfo);
            }

            return true;
        }

        /// <summary>
        /// Heal this entity
        /// </summary>
        /// <param name="amount">Amount to heal</param>
        /// <returns>Actual amount healed (capped by max health)</returns>
        public float Heal(float amount)
        {
            if (IsDead)
                return 0f;

            float previousHealth = _currentHealth;
            CurrentHealth += amount;
            float actualHealed = _currentHealth - previousHealth;

            if (actualHealed > 0)
            {
                Console.WriteLine($"[Health] {Owner?.Name} healed {actualHealed:F1} HP ({_currentHealth:F1}/{_maxHealth:F1})");
                OnHealed?.Invoke(actualHealed, _currentHealth);
            }

            return actualHealed;
        }

        /// <summary>
        /// Instantly kill this entity
        /// </summary>
        public void Kill()
        {
            if (_hasDied)
                return;

            CurrentHealth = 0;
            Die(DamageInfo.Create(9999f, DamageType.Generic));
        }

        /// <summary>
        /// Handle death
        /// </summary>
        private void Die(DamageInfo killingBlow)
        {
            if (_hasDied)
                return;

            _hasDied = true;
            Console.WriteLine($"[Health] {Owner?.Name} died!");

            // Trigger death event
            OnDeath?.Invoke(killingBlow);

            // Destroy entity if configured
            if (DestroyOnDeath && Owner != null)
            {
                // TODO: Add entity destruction when we have scene management
                Owner.Active = false;
            }
        }

        /// <summary>
        /// Respawn/revive this entity
        /// </summary>
        /// <param name="healthPercentage">Health percentage to respawn with (0-1)</param>
        public void Respawn(float healthPercentage = 1.0f)
        {
            _hasDied = false;
            CurrentHealth = _maxHealth * Math.Clamp(healthPercentage, 0f, 1f);
            CanTakeDamage = true;

            if (Owner != null)
                Owner.Active = true;

            Console.WriteLine($"[Health] {Owner?.Name} respawned with {_currentHealth:F1} HP");
            OnRespawn?.Invoke();
        }

        /// <summary>
        /// Fully restore health
        /// </summary>
        public void RestoreToFull()
        {
            Heal(_maxHealth - _currentHealth);
        }

        /// <summary>
        /// Set health to a specific value
        /// </summary>
        public void SetHealth(float health)
        {
            CurrentHealth = health;
        }

        /// <summary>
        /// Set health to a percentage of max health
        /// </summary>
        public void SetHealthPercentage(float percentage)
        {
            CurrentHealth = _maxHealth * Math.Clamp(percentage, 0f, 1f);
        }
    }
}
