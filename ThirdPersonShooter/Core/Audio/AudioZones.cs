using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>Godot audio bus a sound is routed to (default_bus_layout.tres): Master (no reverb), Outside, Reactor.</summary>
    public enum AudioBus { Master, Outside, Reactor }

    /// <summary>
    /// level.tscn Area3D volumes with audio_bus_override (SoundOutside = CollisionPolygon3D prism, SoundReactorRoom = capsule), read from level.scene.json `volumes`.
    /// Godot (AudioStreamPlayer3D::_get_actual_bus, from memory): the EMITTER's position is tested against the areas; 2D players (AudioStreamPlayer: Music, jump, land, shoot, step) ignore areas.
    /// Both areas can overlap near x=36..39 (reactor sphere r 39 vs the polygon's west edge); Godot's order is unspecified at equal priority: the Reactor wins here.
    /// </summary>
    public static class AudioZones
    {
        sealed class Zone { public AudioBus Bus; public Matrix Inv; public bool Capsule; public float R, H, Depth; public float[] Poly; }
        static readonly List<Zone> Zones = new();
        public static int Count => Zones.Count;

        public static void Load(string contentRoot) => LoadJson(File.ReadAllText(Path.Combine(contentRoot, "Data", "level.scene.json")));

        public static void LoadJson(string json)
        {
            Zones.Clear();
            using var doc = JsonDocument.Parse(json);
            foreach (var v in doc.RootElement.GetProperty("volumes").EnumerateArray())
            {
                if (!Enum.TryParse(v.GetProperty("bus").GetString(), out AudioBus bus)) continue;
                foreach (var s in v.GetProperty("shapes").EnumerateArray())
                {
                    string t = s.GetProperty("type").GetString(); var w = s.GetProperty("world"); var f = new float[16]; for (int i = 0; i < 16; i++) f[i] = w[i].GetSingle();
                    var m = new Matrix(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]);
                    var z = new Zone { Bus = bus, Inv = Matrix.Invert(m) };
                    if (t == "polygonPrism") { var p = s.GetProperty("polygon"); z.Poly = new float[p.GetArrayLength()]; for (int i = 0; i < z.Poly.Length; i++) z.Poly[i] = p[i].GetSingle(); z.Depth = s.GetProperty("depth").GetSingle(); }
                    else if (t == "capsule") { z.Capsule = true; z.R = s.GetProperty("radius").GetSingle(); z.H = s.GetProperty("height").GetSingle(); }
                    else continue;
                    Zones.Add(z);
                }
            }
            Zones.Sort((a, b) => b.Capsule.CompareTo(a.Capsule));   // capsules (Reactor) first = win overlaps
        }

        /// <summary>Bus of the first zone containing the world point, else Master.</summary>
        public static AudioBus BusAt(Vector3 p)
        {
            foreach (var z in Zones)
            {
                var l = Vector3.Transform(p, z.Inv);
                if (z.Capsule)
                {
                    float half = MathF.Max(0f, z.H * 0.5f - z.R), y = Math.Clamp(l.Y, -half, half);   // capsule axis = local Y; height includes both caps
                    if (l.X * l.X + (l.Y - y) * (l.Y - y) + l.Z * l.Z <= z.R * z.R) return z.Bus;
                }
                else if (MathF.Abs(l.Z) <= z.Depth * 0.5f && InPolygon(z.Poly, l.X, l.Y)) return z.Bus;      // prism extrudes along local Z
            }
            return AudioBus.Master;
        }

        static bool InPolygon(float[] poly, float x, float y)
        {
            bool c = false; int n = poly.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = poly[2 * i], yi = poly[2 * i + 1], xj = poly[2 * j], yj = poly[2 * j + 1];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c;
            }
            return c;
        }
    }
}
