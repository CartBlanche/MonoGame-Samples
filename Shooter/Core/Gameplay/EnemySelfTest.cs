using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Enemies;
using Shooter.Gameplay.Weapons;
using Shooter.Physics;
using Shooter.Rendering;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of the enemy damage port, milestone 4c-3a (run as part of --test-physics): Health, Damageable, kills, objective, hitboxes.</summary>
    public static class EnemySelfTest
    {
        private static string F(float v) { return PhysicsSelfTest.F(v); }

        // world centre of the first hitbox collider that belongs to the enemy (nearest enemy, like SceneLoader.HitboxTag)
        private static Vector3 HitboxCentre(LoadedScene loaded, int enemyIndex, int tag, CollisionWorld world)
        {
            foreach (ColliderDto c in loaded.Data.Colliders)
            {
                if (c.Owner == null || !c.Owner.StartsWith("hitbox", StringComparison.OrdinalIgnoreCase) || c.Trigger || c.M == null)
                {
                    continue;
                }
                Matrix m = SceneLoader.ToMatrix(c.M);
                Vector3 centre = Vector3.Transform(new Vector3(c.Center[0], c.Center[1], c.Center[2]), m);
                // the collider belongs to this enemy when casting at it reports the tag from a clear side (resolved by the caller); keep the nearest to the enemy
                if (Vector3.Distance(m.Translation, SceneLoader.ToMatrix(loaded.Data.Enemies[enemyIndex].M).Translation) < 2f)
                {
                    if (c.Owner.Equals(loaded.Hitboxes.Find(h => h.Tag == tag).Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return centre;
                    }
                }
            }
            return Vector3.Zero;
        }

        // a point 3-8 m from the target with a clear line to it (the first thing the ray meets is the hitbox tag)
        private static bool FindClearOrigin(CollisionWorld world, Vector3 target, int tag, out Vector3 origin, out CollisionWorld.RayHit hit)
        {
            foreach (float radius in new[] { 3f, 5f, 8f })
            {
                for (int i = 0; i < 16; i++)
                {
                    float a = i * MathHelper.TwoPi / 16f;
                    Vector3 o = target + new Vector3((float)Math.Cos(a) * radius, 0.3f, (float)Math.Sin(a) * radius);
                    Vector3 d = Vector3.Normalize(target - o);
                    if (world.Raycast(o, d, radius * 2f, out hit) && hit.Owner == tag)
                    {
                        origin = o;
                        return true;
                    }
                }
            }
            origin = Vector3.Zero;
            hit = default(CollisionWorld.RayHit);
            return false;
        }

        private static void Shoot(CollisionWorld world, ProjectileManager pm, ProjectileSettings ps, Vector3 origin, Vector3 dir)
        {
            var p = new ProjectileStandard(ps, world, origin, dir, Vector3.Zero);
            pm.Spawn(p, 1f / 60f, origin, dir);
            for (int i = 0; i < 30 && pm.Live.Count > 0; i++)
            {
                pm.Update(1f / 60f);
            }
        }

        public static void Run(LoadedScene loaded)
        {
            Console.WriteLine("Enemies (4c-3a)");
            SceneFile scene = loaded.Data;
            CollisionWorld world = loaded.Collision;
            if (world == null)
            {
                Console.WriteLine("(no collision world: enemy tests skipped)");
                return;
            }
            var director = new EnemyDirector(loaded);

            // 1. data read from the scene
            {
                Enemy bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot"), turret = director.AllEnemies.Find(e => e.Data.Kind == "Turret");
                ObjectiveKillEnemies o = director.Objective;
                bool ok = director.AllEnemies.Count == 2 && bot != null && turret != null && bot.Health.MaxHealth == 100f && turret.Health.MaxHealth == 400f &&
                          Math.Abs(bot.Health.CriticalHealthRatio - 0.3f) < 1e-6f && o != null && o.MustKillAllEnemies && o.KillsToCompleteObjective == 6 &&
                          o.NotificationEnemiesRemainingThreshold == 1 && o.Title == "Eliminate all the enemies" && o.DelayVisible == 4f &&
                          director.Enemies.NumberOfEnemiesTotal == 2 && director.Enemies.NumberOfEnemiesRemaining == 2;
                PhysicsSelfTest.Check("enemy and objective data read from the scene", ok, director.AllEnemies.Count + " enemies, health " + (bot != null ? bot.Health.MaxHealth : -1) + " / " + (turret != null ? turret.Health.MaxHealth : -1) +
                                      ", objective " + (o != null ? o.Title + " kills " + o.KillsToCompleteObjective + " threshold " + o.NotificationEnemiesRemainingThreshold + " delay " + o.DelayVisible : "none"));
                int botTags = loaded.Hitboxes.FindAll(h => h.EnemyIndex == bot.Index).Count, turretTags = loaded.Hitboxes.FindAll(h => h.EnemyIndex == turret.Index).Count;
                PhysicsSelfTest.Check("hitboxes tagged per enemy (HoverBot HitBox; Turret Hitbox Top + Hitbox Base)", botTags == 1 && turretTags == 2 && loaded.Hitboxes.TrueForAll(h => h.DamageMultiplier == 1f),
                                      "HoverBot " + botTags + " tag(s), Turret " + turretTags + ", multipliers " + string.Join(",", loaded.Hitboxes.ConvertAll(h => F(h.DamageMultiplier))));
            }

            // 1b. the drawn mesh and the hitbox agree (the HoverBot model is raised 0.768 above its root; drawing it at the root put the hitbox a bot-height too high)
            {
                foreach (Enemy en in director.AllEnemies)
                {
                    RenderItem item = loaded.EnemyItems[en.Index].Value;
                    Vector3[] corners = item.Mesh.Bounds.GetCorners();
                    var pts = new Vector3[8];
                    for (int i = 0; i < 8; i++) pts[i] = Vector3.Transform(corners[i], item.World);
                    BoundingBox drawn = BoundingBox.CreateFromPoints(pts);
                    var detail = new List<string>();
                    bool ok = true;
                    foreach (HitboxInfo hb in loaded.Hitboxes.FindAll(h => h.EnemyIndex == en.Index))
                    {
                        Vector3 centre = HitboxCentre(loaded, en.Index, hb.Tag, world);
                        ok &= drawn.Contains(centre) == ContainmentType.Contains;
                        detail.Add(hb.Name + " " + PhysicsSelfTest.V(centre));
                    }
                    PhysicsSelfTest.Check(en.Data.Kind + " hitbox centres lie inside the drawn mesh bounds", ok, string.Join(", ", detail) + "; mesh bounds " + PhysicsSelfTest.V(drawn.Min) + " .. " + PhysicsSelfTest.V(drawn.Max));
                }
            }

            // 2. Health / Damageable rules (Health.cs, Damageable.cs)
            {
                var h = new Health(100f, 0.3f);
                int died = 0; float dmgSeen = 0f; object srcSeen = null; int healed = 0;
                h.OnDie += () => died++;
                h.OnDamaged += (d, s) => { dmgSeen = d; srcSeen = s; };
                h.OnHealed += d => healed++;
                var owner = new object(); var shooter = new object();
                var dm = new Damageable(h, owner, 2f, 0.5f);
                dm.InflictDamage(10f, false, shooter);    // x2 multiplier
                bool a = Math.Abs(h.CurrentHealth - 80f) < 1e-4f && dmgSeen == 20f && ReferenceEquals(srcSeen, shooter);
                dm.InflictDamage(10f, true, shooter);     // explosion: no multiplier
                bool b = Math.Abs(h.CurrentHealth - 70f) < 1e-4f;
                dm.InflictDamage(10f, false, owner);      // self damage: x2 x 0.5
                bool c = Math.Abs(h.CurrentHealth - 60f) < 1e-4f;
                h.Heal(1000f);
                bool d = h.CurrentHealth == 100f && healed == 1 && !h.CanPickup();
                h.Heal(5f);
                bool e = healed == 1;                      // nothing healed, no event
                h.Invincible = true; h.TakeDamage(50f, shooter); h.Invincible = false;
                bool f = h.CurrentHealth == 100f;
                h.TakeDamage(80f, shooter);
                bool g = Math.Abs(h.GetRatio() - 0.2f) < 1e-5f && h.IsCritical() && died == 0;
                h.TakeDamage(500f, shooter);
                h.TakeDamage(10f, shooter);
                bool i = h.CurrentHealth == 0f && died == 1 && dmgSeen == 20f;   // damage is clamped to the health that was left; OnDie fires once
                PhysicsSelfTest.Check("Health / Damageable follow the scripts (multiplier, explosion, self damage, heal, invincible, die once)", a && b && c && d && e && f && g && i,
                                      "a" + a + " b" + b + " c" + c + " d" + d + " e" + e + " f" + f + " g" + g + " i" + i);
            }

            // 3. blaster damage 15: the HoverBot (100) dies on the 7th hit, the Turret (400) on the 27th; kill events, objective texts
            {
                ProjectileSettings ps = ProjectileSettings.From(scene.Projectile.Components["ProjectileStandard"], new Vector3(scene.Projectile.TipInRoot[0], scene.Projectile.TipInRoot[1], scene.Projectile.TipInRoot[2]));
                Enemy bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot"), turret = director.AllEnemies.Find(e => e.Data.Kind == "Turret");
                var kills = new List<EnemyKillEvent>();
                var updates = new List<ObjectiveUpdateEvent>();
                director.Events.EnemyKilled += k => kills.Add(k);
                director.Events.ObjectiveUpdated += u => updates.Add(u);
                var player = director.PlayerActor;

                int botHits = 0;
                bool botAliveBefore7 = true;
                Damageable botBox = director.Enemies.DamageableOf(loaded.Hitboxes.Find(h => h.EnemyIndex == bot.Index).Tag);
                float hitDamage = ps.Damage;
                while (!bot.IsDestroyed && botHits < 50)
                {
                    botBox.InflictDamage(hitDamage, false, player);
                    botHits++;
                    if (botHits == 6)
                    {
                        botAliveBefore7 = !bot.IsDestroyed && Math.Abs(bot.Health.GetRatio() - 0.1f) < 1e-5f;
                    }
                }
                PhysicsSelfTest.Check("HoverBot (100 HP) dies on the 7th blaster hit of " + F(hitDamage), hitDamage == 15f && botHits == 7 && botAliveBefore7 && bot.IsDestroyed,
                                      botHits + " hits, ratio after 6 hits ok " + botAliveBefore7);
                bool ev = kills.Count == 1 && ReferenceEquals(kills[0].Enemy, bot) && kills[0].RemainingEnemyCount == 1 && director.Enemies.NumberOfEnemiesRemaining == 1 && director.Enemies.NumberOfEnemiesTotal == 2;
                ObjectiveKillEnemies obj = director.Objective;
                bool texts = updates.Count == 1 && updates[0].CounterText == "1 / 2" && updates[0].NotificationText == "One enemy left" && !updates[0].IsComplete && obj.KillsToCompleteObjective == 2 && !obj.IsCompleted;
                PhysicsSelfTest.Check("first kill: EnemyKillEvent (1 left), objective '1 / 2' + 'One enemy left'", ev && texts,
                                      kills.Count + " kill event(s), remaining " + (kills.Count > 0 ? kills[0].RemainingEnemyCount : -1) + "; update counter '" + (updates.Count > 0 ? updates[0].CounterText : "-") + "' notification '" + (updates.Count > 0 ? updates[0].NotificationText : "-") + "', goal " + obj.KillsToCompleteObjective);

                int turretHits = 0;
                Damageable turretBox = director.Enemies.DamageableOf(loaded.Hitboxes.Find(h => h.EnemyIndex == turret.Index).Tag);
                bool aliveAt26 = false;
                while (!turret.IsDestroyed && turretHits < 60)
                {
                    turretBox.InflictDamage(hitDamage, false, player);
                    turretHits++;
                    if (turretHits == 26)
                    {
                        aliveAt26 = !turret.IsDestroyed && Math.Abs(turret.Health.CurrentHealth - 10f) < 1e-3f;
                    }
                }
                PhysicsSelfTest.Check("Turret (400 HP) dies on the 27th blaster hit", turretHits == 27 && aliveAt26 && turret.IsDestroyed, turretHits + " hits, 10 HP left after 26: " + aliveAt26);
                bool done = kills.Count == 2 && kills[1].RemainingEnemyCount == 0 && obj.IsCompleted && updates.Count == 2 && updates[1].IsComplete && updates[1].CounterText == "2 / 2" &&
                            updates[1].NotificationText == "Objective complete : Eliminate all the enemies" && director.Enemies.NumberOfEnemiesRemaining == 0;
                PhysicsSelfTest.Check("last kill completes the objective ('2 / 2', 'Objective complete : ...')", done,
                                      "kills " + kills.Count + ", updates " + updates.Count + ", counter '" + (updates.Count > 1 ? updates[1].CounterText : "-") + "', completed " + obj.IsCompleted);
            }

            // 4. damage source: the player's hit is the attack (flash time), an enemy's or null's is not
            {
                director.Reset();
                Enemy bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
                Damageable box = director.Enemies.DamageableOf(loaded.Hitboxes.Find(h => h.EnemyIndex == bot.Index).Tag);
                int damagedEvents = 0;
                bot.Damaged += () => damagedEvents++;
                director.Update(2f);
                box.InflictDamage(1f, false, director.AllEnemies[1]);
                box.InflictDamage(1f, false, null);
                bool none = damagedEvents == 0 && float.IsNegativeInfinity(bot.LastTimeDamaged);
                box.InflictDamage(1f, false, director.PlayerActor);
                bool player = damagedEvents == 1 && Math.Abs(bot.LastTimeDamaged - 2f) < 1e-5f;
                PhysicsSelfTest.Check("only damage from the player counts as an attack (Damaged event, hit flash time)", none && player, "enemy/null source events " + (none ? 0 : 1) + ", player source events " + damagedEvents + ", time " + F(bot.LastTimeDamaged));
            }

            // 5. real hitboxes: rays and projectiles hit them, report the tag, and stop hitting after death
            {
                director.Reset();
                var results = new List<string>();
                bool all = true;
                ProjectileSettings ps = ProjectileSettings.From(scene.Projectile.Components["ProjectileStandard"], new Vector3(scene.Projectile.TipInRoot[0], scene.Projectile.TipInRoot[1], scene.Projectile.TipInRoot[2]));
                foreach (Enemy en in director.AllEnemies)
                {
                    HitboxInfo hb = loaded.Hitboxes.Find(h => h.EnemyIndex == en.Index);
                    Vector3 centre = HitboxCentre(loaded, en.Index, hb.Tag, world);
                    bool found = FindClearOrigin(world, centre, hb.Tag, out Vector3 origin, out CollisionWorld.RayHit hit);
                    bool tagOk = found && ReferenceEquals(director.Enemies.EnemyOf(hit.Owner), en) && hit.Distance > 0f && hit.Distance < (centre - origin).Length();
                    Vector3 dir = found ? Vector3.Normalize(centre - origin) : Vector3.Forward;

                    // a level wall is not an enemy
                    bool levelOk = world.Raycast(new Vector3(0f, 1.5f, 0f), Vector3.Down, 5f, out CollisionWorld.RayHit floor) && floor.Owner == -1 && director.Enemies.EnemyOf(floor.Owner) == null;

                    // a real projectile damages exactly one hit worth
                    var pm = new ProjectileManager { Owner = director.PlayerActor, DamageableOf = tag => director.Enemies.DamageableOf(tag) };
                    float before = en.Health.CurrentHealth;
                    int impacts = 0;
                    pm.Impact += i => impacts++;
                    if (found) Shoot(world, pm, ps, origin, dir);
                    bool dmgOk = found && impacts == 1 && Math.Abs(before - en.Health.CurrentHealth - 15f) < 1e-3f;

                    // kill it with unit damage, then the same ray must not meet the hitbox any more and the render item is gone
                    int itemsBefore = loaded.Items.Count;
                    bool blockedBefore = world.Overlaps(hit.Point, hit.Point + new Vector3(0, 0.05f, 0), 0.2f);   // the live hitbox blocks the player capsule
                    director.Enemies.DamageableOf(hb.Tag).InflictDamage(10000f, false, director.PlayerActor);
                    bool gone = en.IsDestroyed && loaded.Items.Count == itemsBefore - (loaded.EnemyItems[en.Index] != null ? 1 : 0) &&
                                !(world.Raycast(origin, dir, 20f, out CollisionWorld.RayHit after) && after.Owner == hb.Tag);
                    bool noCapsule = blockedBefore && !world.Overlaps(hit.Point, hit.Point + new Vector3(0, 0.05f, 0), 0.2f);   // ... and the dead one no longer does
                    all &= tagOk && levelOk && dmgOk && gone && noCapsule;
                    results.Add(en.Data.Kind + " ray " + (found ? F(hit.Distance) + " m tag " + hit.Owner : "no clear line") + ", impacts " + impacts + ", tagOk " + tagOk + " dmgOk " + dmgOk + " gone " + gone + " capsule " + noCapsule);
                }
                PhysicsSelfTest.Check("enemy hitboxes: ray reports the enemy, projectile deals 15, a dead enemy stops being hit and loses its mesh", all, string.Join("; ", results));
            }

            // 6. reloading the scene (Reset) brings everything back
            {
                director.Reset();
                int items = loaded.Items.Count;
                bool alive = director.Enemies.NumberOfEnemiesRemaining == 2 && director.Objective.KillTotal == 0 && !director.Objective.IsCompleted && director.AllEnemies.TrueForAll(e => e.Health.CurrentHealth == e.Health.MaxHealth && !e.IsDestroyed);
                bool owners = loaded.Hitboxes.TrueForAll(h => world.IsOwnerActive(h.Tag));
                int expectedItems = 0;
                foreach (var it in loaded.EnemyItems) if (it != null) expectedItems++;
                PhysicsSelfTest.Check("Reset restores enemies, colliders, render items and the objective", alive && owners && expectedItems == 2 && items >= expectedItems, "alive " + alive + ", colliders active " + owners + ", enemy items " + expectedItems);
            }
        }
    }
}
