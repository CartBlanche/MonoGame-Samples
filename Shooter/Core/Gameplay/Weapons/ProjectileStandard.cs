using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Physics;

namespace Shooter.Gameplay.Weapons
{
    public class ProjectileSettings
    {
        public float Radius = 0.01f;
        public float MaxLifeTime = 5f;
        public float Speed = 20f;
        public float GravityDownAcceleration;
        public float TrajectoryCorrectionDistance = -1f;
        public bool InheritWeaponVelocity;
        public float Damage = 40f;
        public float ImpactVfxSpawnOffset = 0.1f;
        public string ImpactVfx;          // VFX prefab name (Tools/ParticleBake file), null = none
        public float ImpactVfxLifetime = 5f;
        public string ImpactSfx;          // content name, null = none
        public Vector3 TipInRoot;   // Tip transform relative to the root, MonoGame space (before rotation)

        public static ProjectileSettings From(JsonElement c, Vector3 tipInRoot)
        {
            var s = new ProjectileSettings { TipInRoot = tipInRoot };
            s.Radius = JsonFields.Float(c, "Radius", s.Radius);
            s.MaxLifeTime = JsonFields.Float(c, "MaxLifeTime", s.MaxLifeTime);
            s.Speed = JsonFields.Float(c, "Speed", s.Speed);
            s.GravityDownAcceleration = JsonFields.Float(c, "GravityDownAcceleration", s.GravityDownAcceleration);
            s.TrajectoryCorrectionDistance = JsonFields.Float(c, "TrajectoryCorrectionDistance", s.TrajectoryCorrectionDistance);
            s.InheritWeaponVelocity = JsonFields.Bool(c, "InheritWeaponVelocity", false);
            s.Damage = JsonFields.Float(c, "Damage", s.Damage);
            s.ImpactVfxSpawnOffset = JsonFields.Float(c, "ImpactVfxSpawnOffset", s.ImpactVfxSpawnOffset);
            s.ImpactVfxLifetime = JsonFields.Float(c, "ImpactVfxLifetime", s.ImpactVfxLifetime);
            s.ImpactVfx = JsonFields.AssetFileName(c, "ImpactVfx");
            s.ImpactSfx = JsonFields.AssetName(c, "ImpactSfxClip");
            return s;
        }
    }

    public struct ProjectileImpact
    {
        public Vector3 Point;
        public Vector3 Normal;
        public float Damage;
        public ProjectileSettings Settings;   // the projectile's data (impact VFX / SFX)
        public bool FromCameraRay;   // hit found by the camera-to-muzzle check at spawn time
        public int HitOwner;         // collision tag of the collider that was hit (-1 = level geometry, else an enemy hitbox)
    }

    /// <summary>
    /// Port of ProjectileStandard + ProjectileBase for the player's Blaster shot. Targets are the level and enemy hitboxes (the
    /// manager applies the damage); the sphere cast is a ray (the projectile radius is 1 cm). Hits are reported through
    /// <see cref="ProjectileManager.Impact"/>.
    /// </summary>
    public class ProjectileStandard
    {
        private readonly ProjectileSettings _s;
        private readonly CollisionWorld _world;

        public Vector3 Position;       // = Root position (the prefab root is the origin)
        public Vector3 Forward;
        public bool Alive = true;

        /// <summary>Collision tag reported when <see cref="ExtraTarget"/> is hit (the player).</summary>
        public const int PlayerTag = -100;

        /// <summary>ProjectileBase.Owner: the damage source (null = the manager's owner).</summary>
        public object Owner;
        /// <summary>The owner's own colliders (collision tags): ignored like ProjectileStandard.ownerColliders.</summary>
        public IList<int> IgnoreOwners;
        /// <summary>Colliders outside the triangle world (the player's capsule): (origin, direction, max distance, sphere radius) -> hit distance and normal.</summary>
        public Func<Vector3, Vector3, float, float, (bool Hit, float Distance, Vector3 Normal)> ExtraTarget;

        private Vector3 _velocity;
        private Vector3 _lastRootPosition;
        private bool _hasTrajectoryOverride;
        private Vector3 _correctionVector;
        private Vector3 _consumedCorrection;
        private float _age;
        private readonly Vector3 _inheritedMuzzleVelocity;

        public ProjectileStandard(ProjectileSettings settings, CollisionWorld world, Vector3 position, Vector3 forward, Vector3 inheritedMuzzleVelocity)
        {
            _s = settings;
            _world = world;
            Position = position;
            Forward = Vector3.Normalize(forward);
            _inheritedMuzzleVelocity = inheritedMuzzleVelocity;
        }

        public ProjectileSettings Settings { get { return _s; } }

        public Vector3 TipPosition
        {
            get
            {
                Matrix rot = Matrix.CreateWorld(Vector3.Zero, Forward, Vector3.Up);
                return Position + Vector3.TransformNormal(_s.TipInRoot, rot);
            }
        }

        /// <summary>OnShoot() for a shot by an enemy: no trajectory override and no camera-to-muzzle check (those are only for the player's weapons manager).</summary>
        public void ShootAsEnemy(float dt)
        {
            _lastRootPosition = Position;
            _velocity = Forward * _s.Speed;
            Position += _inheritedMuzzleVelocity * dt;
            _hasTrajectoryOverride = false;
        }

        /// <summary>OnShoot(): initial velocity, spawn-time muzzle velocity push, camera trajectory override, camera-to-muzzle wall check.</summary>
        public void Shoot(float dt, Vector3 cameraPosition, Vector3 cameraForward, Action<ProjectileStandard, ProjectileImpact> onHit)
        {
            Vector3 initialPosition = Position;
            _lastRootPosition = Position;
            _velocity = Forward * _s.Speed;
            Position += _inheritedMuzzleVelocity * dt;

            _hasTrajectoryOverride = true;
            Vector3 cameraToMuzzle = initialPosition - cameraPosition;
            _correctionVector = ProjectOnPlane(-cameraToMuzzle, cameraForward);
            if (_s.TrajectoryCorrectionDistance == 0f)
            {
                Position += _correctionVector;
                _consumedCorrection = _correctionVector;
            }
            else if (_s.TrajectoryCorrectionDistance < 0f)
            {
                _hasTrajectoryOverride = false;
            }

            float len = cameraToMuzzle.Length();
            if (len > 1e-6f && _world.Raycast(cameraPosition, cameraToMuzzle / len, len, out CollisionWorld.RayHit hit))
            {
                OnHit(hit.Point, hit.Normal, hit.Owner, true, onHit);
            }
        }

        public void Update(float dt, Action<ProjectileStandard, ProjectileImpact> onHit)
        {
            if (!Alive)
            {
                return;
            }
            _age += dt;
            if (_age >= _s.MaxLifeTime)
            {
                Alive = false;
                return;
            }

            Position += _velocity * dt;
            if (_s.InheritWeaponVelocity)
            {
                Position += _inheritedMuzzleVelocity * dt;
            }

            // drift towards the camera-centred trajectory (the weapon is offset from the screen centre)
            if (_hasTrajectoryOverride && _consumedCorrection.LengthSquared() < _correctionVector.LengthSquared())
            {
                Vector3 left = _correctionVector - _consumedCorrection;
                float distanceThisFrame = (Position - _lastRootPosition).Length();
                Vector3 step = (distanceThisFrame / _s.TrajectoryCorrectionDistance) * _correctionVector;
                float maxLen = left.Length();
                if (step.Length() > maxLen)
                {
                    step = Vector3.Normalize(step) * maxLen;
                }
                _consumedCorrection += step;
                if (_consumedCorrection.LengthSquared() == _correctionVector.LengthSquared())
                {
                    _hasTrajectoryOverride = false;
                }
                Position += step;
            }

            if (_velocity.LengthSquared() > 1e-12f)
            {
                Forward = Vector3.Normalize(_velocity);
            }

            if (_s.GravityDownAcceleration > 0f)
            {
                _velocity += Vector3.Down * _s.GravityDownAcceleration * dt;
            }

            // hit detection from the last root position to the tip
            Vector3 displacement = TipPosition - _lastRootPosition;
            float dist = displacement.Length();
            if (dist > 1e-6f)
            {
                Vector3 dir = displacement / dist;
                bool worldHit = _world.Raycast(_lastRootPosition, dir, dist, out CollisionWorld.RayHit hit, IgnoreOwners);
                if (ExtraTarget != null)
                {
                    var extra = ExtraTarget(_lastRootPosition, dir, worldHit ? hit.Distance : dist, _s.Radius);
                    if (extra.Hit)
                    {
                        // the sphere cast reaches the capsule first: report the sphere centre's surface contact
                        OnHit(_lastRootPosition + dir * extra.Distance, extra.Normal, PlayerTag, false, onHit);
                        return;
                    }
                }
                if (worldHit)
                {
                    Vector3 point = hit.Point, normal = hit.Normal;
                    if (hit.Distance <= 0f)
                    {
                        point = Position;
                        normal = -Forward;
                    }
                    OnHit(point, normal, hit.Owner, false, onHit);
                    return;
                }
            }

            _lastRootPosition = Position;
        }

        private void OnHit(Vector3 point, Vector3 normal, int owner, bool fromCameraRay, Action<ProjectileStandard, ProjectileImpact> onHit)
        {
            Alive = false;
            if (onHit != null)
            {
                onHit(this, new ProjectileImpact { Point = point, Normal = normal, Damage = _s.Damage, Settings = _s, FromCameraRay = fromCameraRay, HitOwner = owner });
            }
        }

        private static Vector3 ProjectOnPlane(Vector3 v, Vector3 n)
        {
            return v - n * (Vector3.Dot(v, n) / Vector3.Dot(n, n));
        }
    }

    /// <summary>Owns the live projectiles; Update() runs them and removes the dead ones.</summary>
    public class ProjectileManager
    {
        private readonly List<ProjectileStandard> _live = new List<ProjectileStandard>();
        private readonly List<ProjectileStandard> _spawned = new List<ProjectileStandard>();

        public IReadOnlyList<ProjectileStandard> Live { get { return _live; } }
        public int TotalFired { get; private set; }
        public int TotalImpacts { get; private set; }

        /// <summary>Raised for every impact with the level (point is the surface hit, normal faces the projectile).</summary>
        public event Action<ProjectileImpact> Impact;

        /// <summary>Who fired (ProjectileBase.Owner): the damage source, so Health can tell self damage and enemy fire apart.</summary>
        public object Owner;
        /// <summary>Collision tag -> Damageable (Unity: collider.GetComponent&lt;Damageable&gt;()); null for level geometry.</summary>
        public Func<int, Combat.Damageable> DamageableOf;

        /// <summary>Raised when a projectile reaches the player's capsule (the damage goes to the player's Health, 3d).</summary>
        public event Action<ProjectileImpact, object> PlayerHit;

        /// <summary>An enemy's shot: spawned at the muzzle, no player-only trajectory logic.</summary>
        public void SpawnEnemy(ProjectileStandard p, float dt)
        {
            TotalFired++;
            p.ShootAsEnemy(dt);
            _live.Add(p);
        }

        public void Spawn(ProjectileStandard p, float dt, Vector3 cameraPosition, Vector3 cameraForward)
        {
            TotalFired++;
            p.Shoot(dt, cameraPosition, cameraForward, HandleHit);
            if (p.Alive)
            {
                _live.Add(p);
            }
        }

        public void Update(float dt)
        {
            for (int i = 0; i < _live.Count; i++)
            {
                _live[i].Update(dt, HandleHit);
            }
            _live.RemoveAll(p => !p.Alive);
        }

        public void Clear()
        {
            _live.Clear();
        }

        private void HandleHit(ProjectileStandard p, ProjectileImpact impact)
        {
            TotalImpacts++;
            // ProjectileStandard.OnHit: InflictDamage(Damage, false, Owner) on the hit collider's Damageable
            object source = p.Owner ?? Owner;
            if (impact.HitOwner == ProjectileStandard.PlayerTag)
            {
                if (PlayerHit != null) PlayerHit(impact, source);
            }
            else
            {
                Combat.Damageable damageable = DamageableOf != null ? DamageableOf(impact.HitOwner) : null;
                if (damageable != null)
                {
                    damageable.InflictDamage(impact.Damage, false, source);
                }
            }
            if (Impact != null) Impact(impact);
        }
    }
}
