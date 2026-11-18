using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter.Physics
{
    /// <summary>
    /// Builds the collision worlds from Tools/GodotExtract/out/level.scene.json "colliders" (boxes + cylinders, all static).
    /// Godot layer bits (project.godot:186-188): Level=1, Player=2, PlayerOnly=4. Bullets/camera/robots collide with Level only,
    /// the player body with Level|PlayerOnly (player.tscn mask 7 minus Player), so two worlds are built.
    /// </summary>
    public sealed class LevelColliders
    {
        public readonly CollisionWorld LevelWorld = new CollisionWorld();  // layer 1 only (spring arm mask 1, bullets, sight rays)
        public readonly CollisionWorld PlayerWorld = new CollisionWorld(); // layers 1 | 4 (player body)
        public int BoxCount, CylinderCount;
        public Matrix PlayerSpawn = Matrix.Identity;
        /// <summary>level.scene.json spawns.robotSpawns (level.gd robot markers); Matrix rows = images of the axes, +Z row = the robot's front.</summary>
        public Matrix[] RobotSpawns = new Matrix[0];

        public static LevelColliders Load(string contentRoot)
        {
            var lc = new LevelColliders();
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(contentRoot, "Data", "level.scene.json")));
            foreach (var c in doc.RootElement.GetProperty("colliders").EnumerateArray())
            {
                int layer = c.GetProperty("layer").GetInt32();
                string t = c.GetProperty("type").GetString();
                var w = c.GetProperty("world"); var f = new float[16]; for (int i = 0; i < 16; i++) f[i] = w[i].GetSingle();
                var m = new Matrix(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]);
                if (Math.Abs(m.Determinant()) < 1e-8f) continue;
                foreach (var world in new[] { (layer & 1) != 0 ? lc.LevelWorld : null, (layer & 5) != 0 ? lc.PlayerWorld : null })
                {
                    if (world == null) continue;
                    if (t == "box") { var z = c.GetProperty("size"); world.AddBox(m, Vector3.Zero, new Vector3(z[0].GetSingle(), z[1].GetSingle(), z[2].GetSingle())); }
                    else if (t == "cylinder") AddCylinder(world, m, c.GetProperty("radius").GetSingle(), c.GetProperty("height").GetSingle());
                }
                if (t == "box") lc.BoxCount++; else if (t == "cylinder") lc.CylinderCount++;
            }
            var sp = doc.RootElement.GetProperty("spawns").GetProperty("playerSpawns")[0].GetProperty("world"); var g = new float[16];
            for (int i = 0; i < 16; i++) g[i] = sp[i].GetSingle();
            lc.PlayerSpawn = new Matrix(g[0], g[1], g[2], g[3], g[4], g[5], g[6], g[7], g[8], g[9], g[10], g[11], g[12], g[13], g[14], g[15]);
            var rs = doc.RootElement.GetProperty("spawns").GetProperty("robotSpawns"); lc.RobotSpawns = new Matrix[rs.GetArrayLength()];
            for (int k = 0; k < lc.RobotSpawns.Length; k++)
            {
                var rw = rs[k].GetProperty("world"); var q = new float[16]; for (int i = 0; i < 16; i++) q[i] = rw[i].GetSingle();
                lc.RobotSpawns[k] = new Matrix(q[0], q[1], q[2], q[3], q[4], q[5], q[6], q[7], q[8], q[9], q[10], q[11], q[12], q[13], q[14], q[15]);
            }
            lc.LevelWorld.Build(); lc.PlayerWorld.Build();
            return lc;
        }

        /// <summary>Upright 24-sided prism (circumscribed so it is not smaller than the true cylinder).</summary>
        public static void AddCylinder(CollisionWorld w, Matrix m, float r, float h, int n = 24)
        {
            float rr = r / (float)Math.Cos(Math.PI / n), hh = h * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * MathHelper.TwoPi / n, a1 = (i + 1) * MathHelper.TwoPi / n;
                var p0 = new Vector3((float)Math.Cos(a0) * rr, 0, (float)Math.Sin(a0) * rr);
                var p1 = new Vector3((float)Math.Cos(a1) * rr, 0, (float)Math.Sin(a1) * rr);
                Vector3 b0 = Vector3.Transform(p0 - Vector3.UnitY * hh, m), b1 = Vector3.Transform(p1 - Vector3.UnitY * hh, m);
                Vector3 t0 = Vector3.Transform(p0 + Vector3.UnitY * hh, m), t1 = Vector3.Transform(p1 + Vector3.UnitY * hh, m);
                Vector3 cb = Vector3.Transform(-Vector3.UnitY * hh, m), ct = Vector3.Transform(Vector3.UnitY * hh, m);
                w.AddTriangle(b0, b1, t1); w.AddTriangle(b0, t1, t0);   // side (two-sided queries)
                w.AddTriangle(ct, t0, t1); w.AddTriangle(cb, b1, b0);   // caps
            }
        }
    }
}
