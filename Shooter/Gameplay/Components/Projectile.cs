using System;
using System.Numerics;
using Shooter.Core.Components;

namespace Shooter.Gameplay.Components
{
    /// <summary>
    /// Component for physics-based projectiles (grenades, rockets, slow bullets).
    /// For instant-hit weapons, use ProjectileSystem.FireHitscan() instead.
    /// 
    /// This component represents a physical projectile that travels through space,
    /// affected by gravity and physics, and can collide with objects.
    /// </summary>
    public class Projectile : EntityComponent
    {
        // Projectile properties
        public float Damage { get; set; } = 10f;
        public float Speed { get; set; } = 50f;
        public float Lifetime { get; set; } = 5f; // seconds before auto-destroy
        public bool AffectedByGravity { get; set; } = true;
        public float ExplosionRadius { get; set; } = 0f; // 0 = no explosion

        // State tracking
        private Vector3 _velocity;
        private float _spawnTime;
        private Transform3D? _transform;
        private Rigidbody? _rigidbody;

        // Events
        public event Action<Vector3>? OnHit; // Called when projectile hits something
        public event Action? OnExpire; // Called when lifetime expires

        public override void Initialize()
        {
            base.Initialize();

            _transform = Owner?.GetComponent<Transform3D>();
            _rigidbody = Owner?.GetComponent<Rigidbody>();
            
            if (_transform == null)
            {
                throw new InvalidOperationException("Projectile requires a Transform3D component.");
            }
        }

        /// <summary>
        /// Launch the projectile in a direction
        /// </summary>
        public void Launch(Vector3 direction, float speed)
        {
            Speed = speed;
            _velocity = Vector3.Normalize(direction) * speed;
            _spawnTime = 0f; // Will be set on first Update
        }

        public override void Update(Core.Components.GameTime gameTime)
        {
            base.Update(gameTime);

            if (_transform == null)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float currentTime = (float)gameTime.TotalGameTime.TotalSeconds;

            // Set spawn time on first update
            if (_spawnTime == 0f)
            {
                _spawnTime = currentTime;
            }

            // Check lifetime
            if (currentTime - _spawnTime >= Lifetime)
            {
                OnExpire?.Invoke();
                Owner?.Destroy();
                return;
            }

            // Move projectile (if no Rigidbody, we handle movement manually)
            if (_rigidbody == null)
            {
                // Apply gravity if enabled
                if (AffectedByGravity)
                {
                    _velocity += new Vector3(0, -9.81f, 0) * deltaTime;
                }

                // Update position
                _transform.Position += _velocity * deltaTime;
            }
            else
            {
                // Let physics handle movement
                // Rigidbody will handle velocity and collisions
            }

            // TODO: Check for collisions when physics collision callbacks are implemented
        }

        /// <summary>
        /// Called when projectile collides with something
        /// </summary>
        public void OnCollision(Vector3 hitPoint)
        {
            OnHit?.Invoke(hitPoint);

            // TODO: Apply explosion damage if ExplosionRadius > 0

            // Destroy the projectile
            Owner?.Destroy();
        }
    }
}
