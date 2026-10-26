using System;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Effects;
using Shooter.Gameplay.Enemies;
using Shooter.Gameplay.Pickups;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of health pickups and loot, milestone 4c-3d-2 (run as part of --test-physics).</summary>
    public static class PickupSelfTest
    {
        private const float Dt = 1f / 60f;
        private static string F(float v) { return PhysicsSelfTest.F(v); }

        private static int Count(PickupManager pm, string kind)
        {
            int n = 0;
            foreach (Pickup p in pm.All) if (p.Kind == kind) n++;
            return n;
        }

        private static Pickup First(PickupManager pm, string kind)
        {
            foreach (Pickup p in pm.All) if (p.Kind == kind) return p;
            return null;
        }

        private static Capsule At(Vector3 feet, float radius = 0.35f)
        {
            return new Capsule { A = feet + Vector3.Up * radius, B = feet + Vector3.Up * (1.8f - radius), Radius = radius };
        }

        public static void Run(LoadedScene loaded)
        {
            Console.WriteLine("Pickups and loot (4c-3d-2)");
            SceneFile data = loaded.Data;
            // the baked placement of the second pickup's mesh straight from the data (the game already animates the render items); the mesh pose is derived from the first
            Matrix bakedSecond = SceneLoader.ToMatrix(data.ModelInstances.FindAll(m => m.Kind == "pickup")[1].M);

            // 1. values from the scene
            var pm = new PickupManager();
            pm.AddFromScene(data);
            {
                Pickup p = pm.All.Count > 0 ? pm.All[0] : null;
                bool ok = Count(pm, "Health") == 2 && pm.All.Count == 3 && p != null && p.HealAmount == 40f && p.VerticalBobFrequency == 1f && p.BobbingAmount == 0.3f && p.RotatingSpeed == 360f &&
                          p.SfxName == "Audio/SFX/Pickups/Pickup_Health" && p.VfxPrefab == "VFX_HealthPickup" &&
                          Vector3.Distance(p.TriggerCenter, new Vector3(0f, 1f, 0f)) < 1e-4f && Vector3.Distance(p.TriggerHalfSize, Vector3.One) < 1e-4f && pm.Template == p;
                PhysicsSelfTest.Check("health pickups: 2 in the scene (plus the jetpack pickup), heal 40, bob 0.3 at 1 Hz, spin 360 deg/s, 2 x 2 x 2 trigger centred 1 m above the root, sound and VFX names", ok,
                    "pickups " + pm.All.Count + (p != null ? ", heal " + F(p.HealAmount) + ", bob " + F(p.BobbingAmount) + " @ " + F(p.VerticalBobFrequency) + ", spin " + F(p.RotatingSpeed) + ", trigger " + p.TriggerCenter + " +- " + p.TriggerHalfSize + ", sfx " + p.SfxName + ", vfx " + p.VfxPrefab : ""));
            }

            // 2. bobbing and spinning (Pickup.Update)
            {
                Pickup p = pm.All[0];
                for (int i = 0; i < 90; i++) pm.Update(Dt);   // 1.5 s
                float expectedBob = ((float)Math.Sin(pm.Time * 1f) * 0.5f + 0.5f) * 0.3f;
                PhysicsSelfTest.Check("pickup bobs by (sin(t) / 2 + 0.5) x 0.3 and spins 360 degrees per second", Math.Abs(p.BobOffset - expectedBob) < 1e-5f && Math.Abs(p.AngleDegrees - 540f) < 0.01f,
                    "t " + F(pm.Time) + ", bob " + F(p.BobOffset) + " (expected " + F(expectedBob) + "), angle " + F(p.AngleDegrees));
            }

            // 3. the trigger is a box that spins with the root
            {
                var pk = new PickupManager();
                pk.AddFromScene(data);
                Pickup p = pk.All[0];
                Vector3 c = p.Position + p.TriggerCenter;
                Vector3 probe = new Vector3(c.X + 1.3f, p.Position.Y, c.Z);   // 1.3 m from the centre along x, at the box's lowest point
                p.AngleDegrees = 0f;
                bool axis = PickupManager.Overlaps(p, At(probe - new Vector3(0f, 0.0f, 0f), 0.1f));
                p.AngleDegrees = 45f;
                bool diagonal = PickupManager.Overlaps(p, At(probe, 0.1f));
                p.AngleDegrees = 0f;
                bool radius = PickupManager.Overlaps(p, At(probe, 0.35f));
                bool far = PickupManager.Overlaps(p, At(new Vector3(c.X + 3f, p.Position.Y, c.Z), 0.35f));
                bool over = PickupManager.Overlaps(p, At(p.Position + new Vector3(0f, 3.2f, 0f), 0.35f));
                PhysicsSelfTest.Check("trigger box: 1.3 m out misses a 0.1 m capsule but hits at 45 degrees (corner) and with the 0.35 m radius; 3 m away and 2.2 m above miss", !axis && diagonal && radius && !far && !over,
                    "axis " + axis + ", 45 deg " + diagonal + ", radius 0.35 " + radius + ", far " + far + ", above " + over);
            }

            // 4. OnTriggerEnter + HealthPickup.OnPicked
            {
                var pk = new PickupManager();
                pk.AddFromScene(data);
                Pickup p = pk.All[0];
                var health = new Health(100f, 0.3f);
                Capsule cap = At(p.Position);
                pk.GetPlayerCapsule = () => cap;
                pk.GetPlayerHealth = () => health;
                int picked = 0; float healed = 0f;
                pk.Picked += x => picked++;
                health.OnHealed += h => healed += h;
                for (int i = 0; i < 10; i++) pk.Update(Dt);                 // standing in it at full health: nothing
                bool fullNothing = picked == 0 && !p.Destroyed && healed == 0f;
                health.TakeDamage(50f, null);
                for (int i = 0; i < 10; i++) pk.Update(Dt);                 // still inside, no new OnTriggerEnter
                bool noReenter = picked == 0 && !p.Destroyed;
                cap = At(p.Position + new Vector3(20f, 0f, 0f));
                pk.Update(Dt);
                cap = At(p.Position);
                pk.Update(Dt);                                              // entering again
                bool taken = picked == 1 && p.Destroyed && healed == 40f && health.CurrentHealth == 90f && Count(pk, "Health") == 1;
                // the second pickup heals only what fits (80 -> 100)
                Pickup q = First(pk, "Health");
                health.CurrentHealth = 80f;
                cap = At(q.Position);
                pk.Update(Dt);
                bool clamped = health.CurrentHealth == 100f && picked == 2 && Math.Abs(healed - 60f) < 1e-4f && Count(pk, "Health") == 0;
                PhysicsSelfTest.Check("health pickup: ignored at full health and while still inside (OnTriggerEnter fires once per entry), heals 40 on a new entry, is destroyed, heals only what fits",
                    fullNothing && noReenter && taken && clamped, "full health " + fullNothing + ", still inside " + noReenter + ", re-entry " + taken + " (health 90), clamp " + clamped + " (health " + F(health.CurrentHealth) + ")");
            }

            // 5. presentation state: mesh follows the root, picked = hidden (no particles / audio headless)
            {
                var pk = new PickupManager();
                pk.AddFromScene(data);
                Pickup p = pk.All[0];
                var fx = new PickupEffects(loaded, pk, null, null, null);
                int idx = p.DynamicIndex;
                for (int i = 0; i < 45; i++) { pk.Update(Dt); fx.Update(Dt); }
                Matrix expected = loaded.PickupMeshRelative * p.Root;
                float err = (loaded.DynamicItems[idx].Item.World - expected).Translation.Length() + Math.Abs((loaded.DynamicItems[idx].Item.World.M11 - expected.M11));
                bool boundsFollow = loaded.DynamicItems[idx].Bounds.Contains(p.Position + new Vector3(0f, 1f, 0f)) == ContainmentType.Contains;
                // at rest the formula gives back the baked pose
                Matrix rest = loaded.PickupMeshRelative * Matrix.CreateTranslation(new Vector3(data.Pickups[1].Position[0], data.Pickups[1].Position[1], data.Pickups[1].Position[2]));
                Matrix baked = bakedSecond;
                bool formulaOk = idx >= 0;
                var health = new Health(100f, 0.3f);
                health.TakeDamage(60f, null);
                pk.GetPlayerHealth = () => health;
                pk.GetPlayerCapsule = () => At(p.Position);
                pk.Update(Dt); fx.Update(Dt);
                bool hidden = !loaded.DynamicItems[idx].Active && health.CurrentHealth == 80f;
                fx.Dispose();
                PhysicsSelfTest.Check("pickup mesh follows bob and spin (root pose x the baked mesh-in-root pose), its bounds follow, and it disappears when picked", err < 1e-3f && boundsFollow && formulaOk && hidden,
                    "pose error " + F(err) + ", bounds follow " + boundsFollow + ", hidden after pickup " + hidden);
                new PickupEffects(loaded, new PickupManager(), null, null, null).Dispose();   // a reload brings the baked meshes back
                bool back = loaded.DynamicItems[idx].Active;
                PhysicsSelfTest.Check("a reload (new effects) makes the baked pickup meshes visible again", back, "active again " + back);
                float restErr = (rest - baked).Translation.Length() + Math.Abs(rest.M12 - baked.M12) + Math.Abs(rest.M21 - baked.M21) + Math.Abs(rest.M11 - baked.M11) + Math.Abs(rest.M33 - baked.M33);
                PhysicsSelfTest.Check("at angle 0 and bob 0 the first pickup's mesh pose, moved to the second pickup, reproduces the second's baked mesh placement", restErr < 1e-4f, "difference " + F(restErr));
            }

            // 6. loot: the HoverBot drops Loot_Health where it died, the Turret nothing; Reset clears it
            {
                PlayerSettings ps = PlayerSettings.FromScene(data).Clone();
                var player = new PlayerController(loaded.Collision, ps);
                var director = new EnemyDirector(loaded);
                director.SetPlayer(player, ps);
                Enemy bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
                Enemy turret = director.AllEnemies.Find(e => e.Data.Kind == "Turret");
                int drops = 0; Pickup loot = null; Enemy by = null;
                director.LootDropped += (e, p) => { drops++; loot = p; by = e; };
                int before = director.Pickups.All.Count;
                turret.Health.Kill();
                bool turretNone = drops == 0 && director.Pickups.All.Count == before;
                Vector3 at = bot.Position;
                bot.Health.Kill();
                bool botDrop = drops == 1 && by == bot && loot != null && loot.Name == "Loot_Health" && loot.Start == at && loot.HealAmount == 40f && director.Pickups.All.Count == before + 1 && System.Linq.Enumerable.Contains(director.Pickups.All, loot);
                director.Reset();
                bool reset = Count(director.Pickups, "Health") == 2 && director.Pickups.All.Count == 3;
                PhysicsSelfTest.Check("loot: the HoverBot (Loot_Health, DropRate 1) leaves a heal-40 pickup where it died, the Turret (no LootPrefab) none; a reload restores the scene pickups",
                    turretNone && botDrop && reset, "turret drop-free " + turretNone + ", bot drop " + botDrop + (loot != null ? " at " + loot.Start + " (died at " + at + ")" : "") + ", pickups after reset " + director.Pickups.All.Count);
            }
        }
    }
}
