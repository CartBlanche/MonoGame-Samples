using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>Ground height query against the level colliders (boxes + cylinders of level.scene.json): highest collider top below a point.</summary>
    public sealed class GroundProbe
    {
        struct Shape { public Matrix Inv, World; public Vector3 Half; public bool Cyl; public float R; }
        readonly List<Shape> _shapes = new();

        public GroundProbe(string contentRoot)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(contentRoot, "Data", "level.scene.json")));
            foreach (var c in doc.RootElement.GetProperty("colliders").EnumerateArray())
            {
                string t = c.GetProperty("type").GetString();
                if (t != "box" && t != "cylinder") continue;
                var w = c.GetProperty("world"); var f = new float[16]; for (int i = 0; i < 16; i++) f[i] = w[i].GetSingle();
                var m = new Matrix(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]);
                if (Math.Abs(m.Determinant()) < 1e-8f) continue;
                var s = new Shape { World = m, Inv = Matrix.Invert(m) };
                if (t == "box") { var z = c.GetProperty("size"); s.Half = new Vector3(z[0].GetSingle(), z[1].GetSingle(), z[2].GetSingle()) * 0.5f; }
                else { s.Cyl = true; s.R = c.GetProperty("radius").GetSingle(); s.Half = new Vector3(s.R, c.GetProperty("height").GetSingle() * 0.5f, s.R); }
                _shapes.Add(s);
            }
        }

        /// <summary>Highest collider top at (x,z) that is at most <paramref name="maxY"/>; null if none.</summary>
        public float? GroundY(float x, float z, float maxY)
        {
            float? best = null;
            foreach (var s in _shapes)
            {
                // Test the vertical line through (x, maxY) in the shape's local space (shapes are upright, so local x/z are horizontal enough).
                var l = Vector3.Transform(new Vector3(x, maxY, z), s.Inv);
                bool inside = s.Cyl ? (l.X * l.X + l.Z * l.Z <= s.R * s.R) : (Math.Abs(l.X) <= s.Half.X && Math.Abs(l.Z) <= s.Half.Z);
                if (!inside) continue;
                float top = Vector3.Transform(new Vector3(l.X, s.Half.Y, l.Z), s.World).Y;
                if (top <= maxY && (best == null || top > best)) best = top;
            }
            return best;
        }
    }
}
