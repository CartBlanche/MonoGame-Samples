using System;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Enemies;
using Shooter.Physics;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of the player's Health, milestone 4c-3d-1 (run as part of --test-physics): data, damage rules, enemy hits, death.</summary>
    public static class PlayerHealthSelfTest
    {
        private const float Dt = 1f / 60f;
        private static string F(float v) { return PhysicsSelfTest.F(v); }

        public static void Run(LoadedScene loaded)
        {
            Console.WriteLine("Player health (4c-3d-1)");
            PlayerSettings ps = PlayerSettings.FromScene(loaded.Data).Clone();
            var player = new PlayerController(loaded.Collision, ps);
            var director = new EnemyDirector(loaded);
            director.SetPlayer(player, ps);

            // 1. data from the Player prefab, and the Damageable rules with the player's 0.25 self-damage sensibility
            {
                Health h = director.PlayerHealth;
                Damageable d = director.PlayerDamageable;
                object enemy = director.AllEnemies[0];
                d.InflictDamage(10f, false, enemy);
                float afterEnemy = h.CurrentHealth;
                d.InflictDamage(20f, false, director.PlayerActor);   // own explosion / projectile: x0.25
                float afterSelf = h.CurrentHealth;
                d.InflictDamage(20f, true, enemy);
                bool critical = h.IsCritical();
                PhysicsSelfTest.Check("player Health 100 (critical 0.3) and Damageable (x1, self damage x0.25) come from the Player prefab",
                    h.MaxHealth == 100f && h.CriticalHealthRatio == 0.3f && d.DamageMultiplier == 1f && d.SensibilityToSelfdamage == 0.25f && afterEnemy == 90f && afterSelf == 85f && h.CurrentHealth == 65f && !critical && ReferenceEquals(d.Health, h),
                    "max " + F(h.MaxHealth) + ", critical " + F(h.CriticalHealthRatio) + ", self " + F(d.SensibilityToSelfdamage) + ", 10 from an enemy -> " + F(afterEnemy) + ", 20 own -> " + F(afterSelf) + ", 20 explosion -> " + F(h.CurrentHealth));
                h.Heal(1000f);
                h.TakeDamage(75f, null);
                PhysicsSelfTest.Check("critical health starts at 30 %", h.CurrentHealth == 25f && h.IsCritical() && h.GetRatio() == 0.25f, "health " + F(h.CurrentHealth));
            }

            // 2. Reset gives a fresh Health and the controller follows it (a reload of the scene)
            {
                Health old = director.PlayerHealth;
                old.Kill();
                bool dead = player.IsDead;
                director.Reset();
                player.Respawn(ps.StartPosition, ps.StartYawDegrees);
                bool fresh = director.PlayerHealth != old && director.PlayerHealth.CurrentHealth == 100f;
                bool aliveAfterRespawn = !player.IsDead;
                director.PlayerHealth.TakeDamage(100f, null);
                PhysicsSelfTest.Check("scene reload: fresh player Health at 100, and the controller dies with the new one", dead && fresh && aliveAfterRespawn && player.IsDead,
                    "died with the first " + dead + ", fresh " + fresh + ", alive after respawn " + aliveAfterRespawn + ", dead with the new one " + player.IsDead);
                director.Reset();
                player.Respawn(ps.StartPosition, ps.StartYawDegrees);
            }

            // 3. enemy shots that reach the capsule damage the player (HoverBot 10, Turret 3)
            {
                Enemy bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
                int hits = 0; float damageSeen = 0f; object lastSource = null;
                director.PlayerHealth.OnDamaged += (dmg, src) => { damageSeen += dmg; lastSource = src; };
                director.EnemyProjectiles.PlayerHit += (imp, src) => hits++;
                // stand 8 m from the HoverBot with a clear line: it detects, attacks and its shots reach the capsule
                Vector3 spot = FindSpotNear(loaded, bot.DetectionSourcePosition, 8f);
                player.Respawn(spot, 0f);
                for (int i = 0; i < 30; i++) player.Update(Dt, default(PlayerInput));
                float t = 0f;
                while (t < 4f && director.PlayerHealth.CurrentHealth > 0f)
                {
                    player.Update(Dt, default(PlayerInput));
                    director.Update(Dt);
                    t += Dt;
                }
                float expected = Math.Max(0f, 100f - damageFromHits(director));
                PhysicsSelfTest.Check("real level: enemy hits on the capsule are applied to the player's Health with the enemy as the source",
                    hits >= 1 && Math.Abs(director.PlayerHealth.CurrentHealth - expected) < 1e-3f && Math.Abs(damageSeen - (100f - director.PlayerHealth.CurrentHealth)) < 1e-3f && (lastSource is Enemy),
                    "hits " + hits + ", health " + F(director.PlayerHealth.CurrentHealth) + " (expected " + F(expected) + "), damage events " + F(damageSeen) + ", source " + (lastSource != null ? lastSource.GetType().Name : "null"));
                director.Reset();
                player.Respawn(ps.StartPosition, ps.StartYawDegrees);
            }
        }

        private static float damageFromHits(EnemyDirector director)
        {
            float total = 0f;
            foreach (var kv in director.PlayerHits)
            {
                total += kv.Key.Damage;
            }
            return total;
        }

        // a floor spot 'distance' m from the point with a clear line to it
        internal static Vector3 FindSpotNear(LoadedScene loaded, Vector3 from, float distance)
        {
            for (int i = 0; i < 48; i++)
            {
                float a = i * MathHelper.TwoPi / 48f;
                Vector3 c = new Vector3(from.X + (float)Math.Cos(a) * distance, from.Y + 3f, from.Z + (float)Math.Sin(a) * distance);
                if (!loaded.Collision.Raycast(c, Vector3.Down, 12f, out CollisionWorld.RayHit floor) || Math.Abs(floor.Normal.Y) < 0.9f) continue;
                c.Y = floor.Point.Y + 0.01f;
                Vector3 aim = c + new Vector3(0f, 1.09f, 0f);
                Vector3 dir = Vector3.Normalize(aim - from);
                bool clear = !loaded.Collision.Raycast(from, dir, (aim - from).Length(), out CollisionWorld.RayHit blk);
                if (clear && !loaded.Collision.OverlapsIgnoring(c + Vector3.Up * 0.35f, c + Vector3.Up * 1.45f, 0.35f, -2))
                {
                    return c;
                }
            }
            return from;
        }
    }
}
