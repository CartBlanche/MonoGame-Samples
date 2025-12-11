using System;
using System.Collections.Generic;
using System.Numerics;
using Shooter.Core;
using Shooter.Core.Plugins.Physics;
using Shooter.Core.Services;
using Shooter.Core.Entities;
using Shooter.Gameplay.Components;

namespace Shooter.Gameplay.Systems
{
    /// <summary>
    /// System for handling projectiles, raycasts, and bullet physics.
    /// Provides hitscan (instant raycast) and physics-based projectile support.
    /// 
    /// HITSCAN:
    /// - Instant hit detection using raycasts
    /// - No travel time
    /// - Used for fast weapons (pistols, rifles, lasers)
    /// 
    /// PHYSICS PROJECTILES:
    /// - Simulated bullet travel with physics
    /// - Affected by gravity (ballistics)
    /// - Used for grenades, rockets, slow projectiles
    /// </summary>
    public class ProjectileSystem
    {
        private readonly IPhysicsProvider _physicsProvider;

        // Visual feedback for debugging (optional)
        private readonly List<DebugRay> _debugRays = new();
        private const float DEBUG_RAY_DURATION = 0.1f; // seconds

        // Event fired when a hitscan successfully hits an entity with Health
        public event Action<Entity>? OnHitEntity;

        // Event fired when any hitscan impact occurs (for particles, decals, etc.)
        public event Action<Vector3, Vector3>? OnImpact; // position, normal

        public ProjectileSystem(IPhysicsProvider physicsProvider)
        {
            _physicsProvider = physicsProvider ?? throw new ArgumentNullException(nameof(physicsProvider));
        }

        /// <summary>
        /// Perform a hitscan raycast from origin in direction.
        /// Returns information about what was hit.
        /// </summary>
        public RaycastHit Raycast(Vector3 origin, Vector3 direction, float maxDistance = 1000f)
        {
            direction = Vector3.Normalize(direction);

            // Perform physics raycast
            bool didHit = _physicsProvider.Raycast(origin, direction, maxDistance, out RaycastHit hit);

            // Add debug visualization
            _debugRays.Add(new DebugRay
            {
                Start = origin,
                End = origin + direction * (didHit ? hit.Distance : maxDistance),
                ExpiryTime = DateTime.Now.AddSeconds(DEBUG_RAY_DURATION),
                DidHit = didHit
            });

            return hit;
        }

        /// <summary>
        /// Fire a hitscan shot (instant hit).
        /// Used by weapons like pistols and rifles.
        /// </summary>
        public RaycastHit FireHitscan(Vector3 origin, Vector3 direction, float damage, float maxRange = 1000f, Entity? attacker = null)
        {
            var hit = Raycast(origin, direction, maxRange);

            if (hit.Body != null)
            {
                Console.WriteLine($"[Projectile] Hitscan hit at distance {hit.Distance:F2}");

                // Trigger impact event for visual effects (particles, decals)
                OnImpact?.Invoke(hit.Point, hit.Normal);

                // Try to get the entity from the physics body's user data
                Entity? hitEntity = hit.UserData as Entity;
                
                if (hitEntity != null)
                {
                    // Check if the entity has a Health component
                    var healthComponent = hitEntity.GetComponent<Health>();
                    if (healthComponent != null)
                    {
                        // Create damage info with full details
                        var damageInfo = DamageInfo.CreateFromHit(
                            damage,
                            DamageType.Bullet,
                            attacker,
                            hit.Point,
                            direction,
                            hit.Normal
                        );

                        // Apply damage
                        healthComponent.TakeDamage(damageInfo);

                        // Trigger hit event for feedback (hit markers, sounds, etc.)
                        OnHitEntity?.Invoke(hitEntity);
                    }
                    else
                    {
                        Console.WriteLine($"[Projectile] Hit {hitEntity.Name} but it has no Health component");
                    }
                }
            }

            return hit;
        }

        /// <summary>
        /// Update the projectile system (cleanup expired debug rays, update physics projectiles, etc.)
        /// </summary>
        public void Update(Core.Components.GameTime gameTime)
        {
            // Remove expired debug rays
            _debugRays.RemoveAll(r => DateTime.Now > r.ExpiryTime);

            // TODO: Update physics-based projectiles when implemented
        }

        /// <summary>
        /// Get debug rays for visualization (optional rendering)
        /// </summary>
        public IReadOnlyList<DebugRay> GetDebugRays()
        {
            return _debugRays.AsReadOnly();
        }

        /// <summary>
        /// Clear all debug visualization
        /// </summary>
        public void ClearDebugRays()
        {
            _debugRays.Clear();
        }

        // Helper struct for debug visualization
        public struct DebugRay
        {
            public Vector3 Start;
            public Vector3 End;
            public DateTime ExpiryTime;
            public bool DidHit;
        }
    }
}
