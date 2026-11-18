using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Perspective spot-light shadow maps for the level's SpotLight3Ds (Godot: 13 spots with shadow_enabled, level/geometry/scenes/lights.tscn:21..151).
    /// The MaxSelected spots nearest to the focus (the player) get a Tile x Tile map each, packed into one 2x2 R32F atlas (Tiles = 4 so one map can fade out while
    /// another fades in). Selection has hysteresis, every map's strength fades with the light's distance and eases in/out over BlendSeconds, so moving between lamps does not pop.
    /// The lamps are static, so the maps never shimmer with the camera; only moving casters change them.
    /// </summary>
    public sealed class SpotShadows
    {
        public const int Tile = 512, Tiles = 4, MaxSelected = 3;
        public const float FovMarginDeg = 4f, Near = 0.3f, Far = 30f, FadeNear = 14f, FadeFar = 26f, Hysteresis = 1.5f, BlendSeconds = 0.35f, MaxDist = 28f;

        internal sealed class Entry
        {
            public SceneLighting.L Light; public bool Selected; public float Blend, Weight, Dist;
            public Matrix View, Proj, VP; public BoundingFrustum Frustum; public float TexelPerM; public int Tile;
        }
        internal readonly List<Entry> Entries = new();
        internal readonly List<Entry> Active = new();
        public readonly ShadowMap Atlas;
        public bool Enabled;                         // false: no maps this frame (Settings > SHADOWS off), shader weights stay 0
        public int ActiveCount => Active.Count;
        readonly Vector4[] _vr = new Vector4[Tiles * 4]; readonly Vector4[] _pos = new Vector4[Tiles], _p = new Vector4[Tiles];

        internal SpotShadows(GraphicsDevice gd, Effect depth, IEnumerable<SceneLighting.L> spots)
        {
            Atlas = new ShadowMap(gd, depth, Tile * 2);
            foreach (var l in spots) Entries.Add(new Entry { Light = l });
            Console.WriteLine($"[spotshadow] {Entries.Count} shadow-capable spots, {MaxSelected} maps of {Tile}x{Tile} in a {Tile * 2}x{Tile * 2} atlas");
        }

        /// <summary>Spot light frustum: look-at along the light direction, vertical/horizontal FOV = 2 x (outer cone + margin), near/far from the light range.</summary>
        public static void Fit(Vector3 pos, Vector3 dir, float outerRad, out Matrix view, out Matrix proj, out float texelPerM)
        {
            dir = Vector3.Normalize(dir);
            view = Matrix.CreateLookAt(pos, pos + dir, MathF.Abs(dir.Y) > 0.95f ? Vector3.UnitZ : Vector3.UnitY);
            float fov = 2f * (outerRad + MathHelper.ToRadians(FovMarginDeg));
            proj = Matrix.CreatePerspectiveFieldOfView(fov, 1f, Near, Far);
            texelPerM = 2f * MathF.Tan(fov * 0.5f) / Tile;
        }

        /// <summary>Nearest-N selection with hysteresis: incumbents get a distance bonus; lights beyond maxDist are never selected. Updates <paramref name="selected"/> in place.</summary>
        public static void SelectNearest(IReadOnlyList<float> dist, bool[] selected, int n, float bonus, float maxDist)
        {
            var idx = new List<int>(); for (int i = 0; i < dist.Count; i++) if (dist[i] <= maxDist) idx.Add(i);
            idx.Sort((a, b) => (dist[a] - (selected[a] ? bonus : 0f)).CompareTo(dist[b] - (selected[b] ? bonus : 0f)));
            Array.Clear(selected, 0, selected.Length);
            for (int k = 0; k < idx.Count && k < n; k++) selected[idx[k]] = true;
        }

        /// <summary>Distance fade of a light's map strength (1 close, 0 at FadeFar).</summary>
        public static float DistWeight(float d) { float t = Math.Clamp((d - FadeNear) / (FadeFar - FadeNear), 0f, 1f); return 1f - t * t * (3f - 2f * t); }

        /// <summary>Per-frame update. <paramref name="dt"/> &lt;= 0 jumps to the target state (screenshots, tests).</summary>
        public void Update(Vector3 focus, float dt)
        {
            var dist = new float[Entries.Count]; var sel = new bool[Entries.Count];
            for (int i = 0; i < Entries.Count; i++) { dist[i] = Vector3.Distance(Entries[i].Light.Pos, focus); sel[i] = Entries[i].Selected; }
            SelectNearest(dist, sel, MaxSelected, Hysteresis, MaxDist);
            Active.Clear();
            for (int i = 0; i < Entries.Count; i++)
            {
                var e = Entries[i]; e.Selected = sel[i]; e.Dist = dist[i];
                float target = e.Selected ? 1f : 0f;
                e.Blend = dt <= 0f ? target : (e.Blend < target ? Math.Min(target, e.Blend + dt / BlendSeconds) : Math.Max(target, e.Blend - dt / BlendSeconds));
                e.Weight = e.Blend * DistWeight(dist[i]);
                if (e.Weight > 0.003f) Active.Add(e);
            }
            Active.Sort((a, b) => b.Weight.CompareTo(a.Weight));
            if (Active.Count > Tiles) Active.RemoveRange(Tiles, Active.Count - Tiles);
            for (int t = 0; t < Active.Count; t++)
            {
                var e = Active[t]; e.Tile = t;
                Fit(e.Light.Pos, e.Light.Dir, e.Light.Outer, out e.View, out e.Proj, out e.TexelPerM);
                e.VP = e.View * e.Proj; e.Frustum = new BoundingFrustum(e.VP);
            }
        }

        /// <summary>Receiver parameters (called from SceneLighting.Apply with the light list of that draw: the shadow only applies to a light that is in the 8 shader slots).</summary>
        internal void Bind(Effect fx, List<SceneLighting.L> all)
        {
            Array.Clear(_p, 0, Tiles);
            if (Enabled)
                foreach (var e in Active)
                {
                    int slot = -1; for (int q = 0; q < 8 && q < all.Count; q++) if (ReferenceEquals(all[q], e.Light)) { slot = q; break; }
                    { var m = e.VP; int b = e.Tile * 4; _vr[b] = new Vector4(m.M11, m.M12, m.M13, m.M14); _vr[b + 1] = new Vector4(m.M21, m.M22, m.M23, m.M24); _vr[b + 2] = new Vector4(m.M31, m.M32, m.M33, m.M34); _vr[b + 3] = new Vector4(m.M41, m.M42, m.M43, m.M44); } _pos[e.Tile] = new Vector4(e.Light.Pos, slot);
                    _p[e.Tile] = new Vector4(e.TexelPerM, 1f / Far, slot >= 0 ? e.Weight : 0f, e.Tile);
                }
            fx.Parameters["SpR"]?.SetValue(_vr); fx.Parameters["SpPos"]?.SetValue(_pos); fx.Parameters["SpP"]?.SetValue(_p);
            fx.Parameters["SpotInv"]?.SetValue(1f / Tile);
            fx.Parameters["SpotTex"]?.SetValue(Atlas.Target);
            fx.GraphicsDevice.SamplerStates[4] = SamplerState.PointClamp; fx.GraphicsDevice.SamplerStates[5] = SamplerState.PointClamp;   // slot 4 in the spot-only program, 5 when the key sampler is also used
        }

        /// <summary>Zero the receiver weights (effects shared with another light set, e.g. the menu).</summary>
        internal static void Clear(Effect fx) { fx.Parameters["SpP"]?.SetValue(new Vector4[Tiles]); }
    }
}
