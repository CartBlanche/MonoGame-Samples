using System;
using Microsoft.Xna.Framework;
using ThirdPersonShooter.Physics;

namespace ThirdPersonShooter
{
    /// <summary>Anything a bullet can damage (plan 03 IHittable). The part-C robots implement this; bullet.gd calls collider.hit().</summary>
    public interface IDamageable { void Hit(Vector3 point); }

    public struct Projectile
    {
        public Vector3 Position, Previous, Dir;
        public float Life;       // bullet.gd time_alive
        public int Owner;        // shooter id (-1 none): never hits its own owner (Godot add_collision_exception_with(self))
        public bool Alive;
    }

    /// <summary>
    /// Bullets of bullet.gd, simulated in the fixed tick: 20 m/s along the spawn direction, 5 s life, sphere radius 0.170413 swept against
    /// the level world (CollisionWorld.CapsuleCast with a zero-length axis). A hit or the end of the lifetime removes the bullet and raises
    /// <see cref="Impact"/> (the visual explosion). Targets (robots) plug in through <see cref="SweepTargets"/>.
    /// </summary>
    public sealed class ProjectileManager
    {
        /// <summary>Optional sweep against damageable targets (robots, part C): nearest hit along from -> to for a sphere of radius, ignoring the owner.</summary>
        public delegate bool TargetSweep(Vector3 from, Vector3 dir, float length, float radius, int owner, out float distance, out IDamageable target);

        readonly CollisionWorld _world;
        readonly Projectile[] _p = new Projectile[64];
        public TargetSweep SweepTargets;
        /// <summary>point (sphere centre at contact), surface normal, target (null for level geometry).</summary>
        public event Action<Vector3, Vector3, IDamageable> Impact;
        public int TotalSpawned { get; private set; }
        public int Count { get { int n = 0; for (int i = 0; i < _p.Length; i++) if (_p[i].Alive) n++; return n; } }
        public ReadOnlySpan<Projectile> All => _p;

        public ProjectileManager(CollisionWorld levelWorld) { _world = levelWorld; }

        public void Spawn(Vector3 origin, Vector3 dir, int owner = -1)
        {
            int slot = -1;
            for (int i = 0; i < _p.Length; i++) if (!_p[i].Alive) { slot = i; break; }
            if (slot < 0) slot = 0; // pool full: recycle the oldest-ish slot
            _p[slot] = new Projectile { Position = origin, Previous = origin, Dir = Vector3.Normalize(dir), Life = PlayerConstants.BulletLifetime, Owner = owner, Alive = true };
            TotalSpawned++;
        }

        public void Update(float dt)
        {
            for (int i = 0; i < _p.Length; i++)
            {
                if (!_p[i].Alive) continue;
                ref Projectile b = ref _p[i];
                b.Previous = b.Position;
                b.Life -= dt;
                if (b.Life < 0f) { b.Alive = false; Impact?.Invoke(b.Position, -b.Dir, null); continue; } // bullet.gd:25-28 explode at the end of life
                float len = PlayerConstants.BulletSpeed * dt;
                float best = float.MaxValue; Vector3 n = -b.Dir; IDamageable tgt = null; bool hit = false;
                if (_world.CapsuleCast(b.Position, b.Position, PlayerConstants.BulletRadius, b.Dir, len, out float d, out Vector3 wn)) { best = d; n = wn; hit = true; }
                if (SweepTargets != null && SweepTargets(b.Position, b.Dir, Math.Min(len, best), PlayerConstants.BulletRadius, b.Owner, out float td, out IDamageable t) && td < best) { best = td; tgt = t; n = -b.Dir; hit = true; }
                if (!hit) { b.Position += b.Dir * len; continue; }
                b.Position += b.Dir * best; b.Alive = false;
                tgt?.Hit(b.Position);                                                 // bullet.gd:29-31 collider.hit()
                Impact?.Invoke(b.Position, n, tgt);
            }
        }
    }
}
