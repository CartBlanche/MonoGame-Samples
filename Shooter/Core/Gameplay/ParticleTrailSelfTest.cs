using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Shooter.Rendering.Particles;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of the particle Trails module (3d-5b, run as part of --test-physics): the data of VFX_HoverBot_Explosion, ribbons follow the particles, taper, outlive them briefly and are freed.</summary>
    public static class ParticleTrailSelfTest
    {
        private static string F(float v) { return PhysicsSelfTest.F(v); }

        public static void Run()
        {
            Console.WriteLine("Particle trails (4c-3d-5b)");
            string path = Path.Combine(AppContext.BaseDirectory, "Content", "Particles", "VFX_HoverBot_Explosion.particles.json");
            if (!File.Exists(path))
            {
                Console.WriteLine("  (no particle content next to the executable: skipped)");
                return;
            }
            List<ParticleSystemData.Placement> placements;
            using (FileStream s = File.OpenRead(path)) placements = ParticleSystemData.Load(s);
            ParticleSystemData data = placements.Find(p => p.Data.TrailsEnabled).Data;

            // 1. data from the prefab: ratio 1, lifetime 0.15 s, a vertex every 0.35 m, width 1 -> 0, white -> transparent over the particle's life, trail material = TrailDeath
            PhysicsSelfTest.Check("VFX_HoverBot_Explosion trails: ratio 1, lifetime 0.15 s, minVertexDistance 0.35, width 1 -> 0 along the trail, size affects width, trail material TrailDeath",
                data.TrailsEnabled && data.TrailRatio == 1f && Math.Abs(data.TrailLifetime.Evaluate(0.5f, 0.5f) - 0.15f) < 1e-4f && Math.Abs(data.TrailMinVertexDistance - 0.35f) < 1e-4f
                && Math.Abs(data.TrailWidth.Evaluate(0f, 0.5f) - 1f) < 1e-3f && data.TrailWidth.Evaluate(1f, 0.5f) < 1e-3f && data.TrailSizeAffectsWidth && !data.TrailDieWithParticles && data.TrailMaterialName == "TrailDeath",
                "ratio " + F(data.TrailRatio) + ", lifetime " + F(data.TrailLifetime.Evaluate(0.5f, 0.5f)) + ", dist " + F(data.TrailMinVertexDistance) + ", width head " + F(data.TrailWidth.Evaluate(0f, 0.5f)) + " tail " + F(data.TrailWidth.Evaluate(1f, 0.5f)) + ", material " + data.TrailMaterialName);

            // 2. run it: ribbons exist while the particles fly, are valid strips that taper, and are camera facing (wider than zero, finite)
            var sys = new ParticleSystemInstance(data, null, Matrix.Identity, 7);
            var verts = new List<ParticleVertex>();
            var idx = new List<short>();
            int maxVerts = 0; bool finite = true, tapered = false, hadTrails = false;
            Vector3 cam = new Vector3(0f, 1f, 6f);
            for (int i = 0; i < 40; i++)   // 0.67 s
            {
                sys.Update(1f / 60f);
                verts.Clear(); idx.Clear();
                sys.BuildTrailGeometry(cam, verts, idx);
                hadTrails |= sys.HasTrails;
                maxVerts = Math.Max(maxVerts, verts.Count);
                foreach (ParticleVertex v in verts) finite &= !float.IsNaN(v.Position.X + v.Position.Y + v.Position.Z) && !float.IsInfinity(v.Position.X + v.Position.Y + v.Position.Z);
                // vertices come as (left, right) pairs per ribbon point, head first: the tail pair is narrower than the head pair
                for (int k = 0; k + 3 < verts.Count; k += 2)
                {
                    float wHead = Vector3.Distance(verts[k].Position, verts[k + 1].Position), wNext = Vector3.Distance(verts[k + 2].Position, verts[k + 3].Position);
                    if (wHead > 1e-4f && wNext < wHead * 0.999f) tapered = true;
                }
            }
            PhysicsSelfTest.Check("trails: ribbons appear behind the flying particles (valid triangle lists, finite) and narrow towards the tail",
                hadTrails && maxVerts >= 4 && idx.Count % 3 == 0 && finite && tapered,
                "max vertices " + maxVerts + ", finite " + finite + ", tapered " + tapered + ", indices " + idx.Count);

            // 3. after the particles die the ribbons fade out within the trail lifetime and the system is finished
            for (int i = 0; i < 360; i++) sys.Update(1f / 60f);   // +6 s: far past particle life + 0.15 s
            PhysicsSelfTest.Check("trails: once the particles have died the ribbons are freed and the system is no longer alive",
                !sys.HasTrails && !sys.IsAlive && sys.ParticleCount == 0, "trails " + sys.HasTrails + ", alive " + sys.IsAlive + ", particles " + sys.ParticleCount);

            // 4. a trail in the system's local space (the blaster's cylinders) goes with the system; in world space it would smear behind it
            string gunPath = Path.Combine(AppContext.BaseDirectory, "Content", "Particles", "VFX_GunCylinder.particles.json");
            if (File.Exists(gunPath))
            {
                float Smear(bool worldSpace)
                {
                    List<ParticleSystemData.Placement> pl;
                    using (FileStream fs = File.OpenRead(gunPath)) pl = ParticleSystemData.Load(fs);
                    ParticleSystemData gd = pl.Find(p => p.Data.TrailsEnabled).Data;
                    gd.TrailWorldSpace = worldSpace;
                    var gun = new ParticleSystemInstance(gd, null, Matrix.Identity, 3);
                    gun.RateOverTimeMultiplier = 30f;
                    float x = 0f, far = 0f;
                    var gv = new List<ParticleVertex>(); var gi = new List<short>();
                    for (int i = 0; i < 120; i++)   // 2 s, the gun swings 10 m/s sideways
                    {
                        x += 10f / 60f;
                        gun.SetWorld(Matrix.CreateTranslation(x, 0f, 0f));
                        gun.Update(1f / 60f);
                        gv.Clear(); gi.Clear();
                        gun.BuildTrailGeometry(new Vector3(x, 0f, 5f), gv, gi);
                        foreach (ParticleVertex v in gv) far = Math.Max(far, Vector3.Distance(v.Position, new Vector3(x, 0f, 0f)));
                    }
                    return far;
                }
                float local = Smear(false), world = Smear(true);
                PhysicsSelfTest.Check("trails: with worldSpace off (the gun cylinders) the ribbons stay with the moving system, with it on they are left behind",
                    local < 1.5f && world > local + 0.5f, "farthest ribbon vertex from the system: local space " + F(local) + " m, world space " + F(world) + " m");
            }

            // 5. ScalingMode Local: the blaster's steam (scale 0.45 from its parents, own scale 1) is drawn in its own units, not shrunk by the weapon's scale
            string steamPath = Path.Combine(AppContext.BaseDirectory, "Content", "Particles", "Weapon_Blaster.particles.json");
            if (File.Exists(steamPath))
            {
                float MaxSize(int scalingMode)
                {
                    List<ParticleSystemData.Placement> pl;
                    using (FileStream fs = File.OpenRead(steamPath)) pl = ParticleSystemData.Load(fs);
                    ParticleSystemData.Placement steamPlacement = pl.Find(p => p.Data.Name == "VFX_OverheatPrimary");
                    steamPlacement.Data.ScalingMode = scalingMode;
                    var steam = new ParticleSystemInstance(steamPlacement.Data, null, steamPlacement.World, 11);
                    steam.RateOverTimeMultiplier = 8f;
                    var rps = new List<ParticleSystemInstance.RenderParticle>();
                    float max = 0f;
                    for (int i = 0; i < 180; i++)
                    {
                        steam.Update(1f / 60f);
                        steam.GetRenderParticles(rps);
                        foreach (var rp in rps) max = Math.Max(max, rp.Size.X);
                    }
                    return max;
                }
                float local = MaxSize(1), hierarchy = MaxSize(0);
                PhysicsSelfTest.Check("scaling mode Local: the weapon steam puffs are about 1 / 0.45 times larger than with Hierarchy scaling (parents' 0.45 scale ignored), 0.5 - 1.5 units",
                    local > 0.45f && local < 1.6f && Math.Abs(local / hierarchy - 1f / 0.45f) < 0.15f, "largest puff: local " + F(local) + ", hierarchy " + F(hierarchy) + ", ratio " + F(local / hierarchy));
            }

            // 6. the green muzzle flash: a burst of flares 0.3 - 1 unit wide (scaling mode Local, own scale 1), 0.1 s life, fading out
            string flashPath = Path.Combine(AppContext.BaseDirectory, "Content", "Particles", "VFX_MuzzleFlashGreen.particles.json");
            if (File.Exists(flashPath))
            {
                List<ParticleSystemData.Placement> fl;
                using (FileStream fs = File.OpenRead(flashPath)) fl = ParticleSystemData.Load(fs);
                ParticleSystemData.Placement fp = fl[0];
                var flash = new ParticleSystemInstance(fp.Data, null, Matrix.CreateScale(0.45f), 5);   // parented under a 0.45 scaled weapon
                var rps = new List<ParticleSystemInstance.RenderParticle>();
                flash.Update(0.005f);
                int n = flash.GetRenderParticles(rps);
                float minSize = float.MaxValue, maxSize = 0f, alpha0 = 0f;
                foreach (var rp in rps) { minSize = Math.Min(minSize, rp.Size.X); maxSize = Math.Max(maxSize, rp.Size.X); alpha0 = Math.Max(alpha0, rp.Color.W); }
                for (int i = 0; i < 12; i++) flash.Update(0.01f);
                int left = flash.GetRenderParticles(rps);
                PhysicsSelfTest.Check("muzzle flash: a burst of 2 flares 0.3 - 1 unit wide (own scale 1, the weapon's 0.45 ignored), nearly opaque, gone after 0.12 s",
                    n == 2 && minSize >= 0.29f && maxSize <= 1.01f && maxSize > 0.5f && alpha0 > 0.8f && left == 0,
                    n + " particles, size " + F(minSize) + " - " + F(maxSize) + ", alpha " + F(alpha0) + ", after 0.12 s: " + left);
            }

            // 7. the scene's ambient emitters (VFX_Dust, VFX_Lava) loop from the start at their MainScene positions
            string scenePath = Path.Combine(AppContext.BaseDirectory, "Content", "Particles", "MainScene.particles.json");
            if (File.Exists(scenePath))
            {
                List<ParticleSystemData.Placement> sp;
                using (FileStream fs = File.OpenRead(scenePath)) sp = ParticleSystemData.Load(fs);
                string report = "";
                bool ok = true;
                foreach (string name in new[] { "VFX_Dust", "VFX_Lava" })
                {
                    ParticleSystemData.Placement pl = sp.Find(p => p.Data.Name == name);
                    var amb = new ParticleSystemInstance(pl.Data, null, pl.World, 9);
                    for (int i = 0; i < 300; i++) amb.Update(1f / 60f);   // 5 s
                    bool near = amb.ParticleCount > 0 && Vector3.Distance((amb.Bounds.Min + amb.Bounds.Max) * 0.5f, pl.World.Translation) < 40f;
                    ok &= pl.Active && pl.Data.Looping && near;
                    report += name + " " + amb.ParticleCount + " particles at " + pl.World.Translation + "; ";
                }
                PhysicsSelfTest.Check("ambient emitters VFX_Dust and VFX_Lava are active, looping and have live particles around their MainScene positions", ok, report);
            }
        }
    }
}
