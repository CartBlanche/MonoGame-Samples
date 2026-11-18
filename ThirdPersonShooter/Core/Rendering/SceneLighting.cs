using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Godot environment + light data for the PBR shaders. Real values: tps-demo-master/level/geometry/environment.tres
    /// (ambient_light_color l.5, fog_light_color/fog_density l.25-26, tonemap exposure l.9, saturation l.37).
    /// Lights come from Tools/GodotExtract/out/lights.json (export_lights.py, KHR_lights_punctual of lights.glb, world space).
    /// </summary>
    public sealed class SceneLighting
    {
        internal sealed class L { public Vector3 Pos, Dir, Color; public bool Spot, Shadowed; public float Inner, Outer, Range; }
        readonly List<L> _lights = new();
        /// <summary>Level spot shadow maps (null for the menu backdrop).</summary>
        public SpotShadows Spots;
        internal IEnumerable<L> StaticSpots() { foreach (var l in _lights) if (l.Spot && !l.Shadowed) yield return l; }
        public void CreateSpotShadows(GraphicsDevice gd, Effect depth) { Spots = new SpotShadows(gd, depth, StaticSpots()); }
        readonly Vector4[] _pos = new Vector4[8], _col = new Vector4[8], _dir = new Vector4[8];

        // environment.tres: ambient_light_color = (0.258824, 0.203922, 0.152941)
        public Vector3 Ambient = new Vector3(0.258824f, 0.203922f, 0.152941f);
        // environment.tres: fog_light_color = (0.4, 0.25098, 0.101961), fog_density = 0.002 (exponential)
        public Vector3 FogColor = new Vector3(0.4f, 0.25098f, 0.101961f);
        public float FogDensity = 0.002f, FogStart = 15f;
        public Vector3 AmbGround, EmisTune;
        public Vector2 AmbShape = Vector2.One;   // lighting pass 3: ambient gain for walls (x) and floors (y)
        public float CavMin = 0.45f;
        // Fake reflection environment (reflected_light_source is Disabled in Godot; reflections there come from VoxelGI/probes, not portable)
        public Vector3 EnvUp, EnvHorizon, EnvDown;
        // GI stand-in key light (Godot has none; VoxelGI supplies form)
        public Vector3 KeyDir = Vector3.Normalize(new Vector3(-0.35f, -1f, -0.25f));
        public Vector3 KeyColor = new Vector3(1.0f, 0.8f, 0.55f) * 0.22f;
        // Lighting pass 4 (level only; neutral defaults keep the menu backdrop unchanged): metals keep part of their albedo as diffuse (no GI/reflections to light them),
        // a flat neutral ambient lifts ceilings/undersides, a rougher spec floor and weaker key spec tame the grazing glare, player albedo tint (cream).
        public float EnvGain = 1f; public Vector2 KeyNear = Vector2.One; public float KeyFloor = 1f, AlbGain = 1f, MetalDiff = 0f, SpecMin = 0.03f, KeySpec = 1f;
        public Vector3 AmbWarm = Vector3.Zero, SteelTint = Vector3.One;
        public Vector3 AmbFlat = Vector3.Zero, PlayerTint = Vector3.One;
        public float LightScale = 1f, LightRange = 40f;
        public float HdrScale = 1f;

        public SceneLighting()
        {
            // Pass 2 (see LIGHTING-NOTES): hemisphere ambient, neutral-warm sky / dark ground; warmth comes from lights, distant fog and emissives.
            Ambient = new Vector3(0.062f, 0.054f, 0.042f) * Tune("TPS_AMB", 1f);
            AmbGround = new Vector3(0.022f, 0.018f, 0.014f) * Tune("TPS_AMB", 1f);
            AmbShape = new Vector2(Tune("TPS_AMBWALL", 1f), Tune("TPS_AMBFLOOR", 1f)); CavMin = 0.45f;
            KeyColor *= Tune("TPS_KEY", 0.6f);
            FogDensity = Tune("TPS_FOG", 0.015f); FogStart = Tune("TPS_FOGSTART", 15f); FogColor *= Tune("TPS_FOGI", 0.3f);
            EmisTune = new Vector3(Tune("TPS_EMIS", 0.6f), Tune("TPS_EMISSAT", 0.7f), Tune("TPS_EMISMAX", 6f));
            // reflection environment: neutral grey so metals stay grey (Godot metals read neutral), only the horizon carries the fog warmth
            float al = Vector3.Dot(Ambient, new Vector3(0.3f, 0.59f, 0.11f));
            EnvUp = new Vector3(al) * 1.6f; EnvHorizon = Vector3.Lerp(new Vector3(al * 2f), FogColor, 0.35f); EnvDown = Ambient * 0.3f;
        }

        /// <summary>Lighting pass 3 (level only; the menu backdrop keeps its own look): key light turned to a grazing direction from behind the spawn view so
        /// walls/pillars facing the player light up while floors stay dim (Godot: bright walls, dark floors), stronger key, deeper cavity term, and the emission
        /// scale compensated for the lower level exposure (3.4 -> 2.8) so emissives look as before. All overridable by TPS_KEY, TPS_KEYDIR, TPS_CAVMIN, TPS_EXP.</summary>
        public void ApplyLevelGrade()
        {
            var kc = (Environment.GetEnvironmentVariable("TPS_KEYC") ?? "1,0.6,0.3").Split(',');
            KeyColor = new Vector3(float.Parse(kc[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(kc[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(kc[2], System.Globalization.CultureInfo.InvariantCulture)) * 0.22f * Tune("TPS_KEY", 1.8f);
            var f = (Environment.GetEnvironmentVariable("TPS_KEYDIR") ?? "0.8,-0.2,0.6").Split(',');
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            KeyDir = Vector3.Normalize(new Vector3(float.Parse(f[0], ci), float.Parse(f[1], ci), float.Parse(f[2], ci)));
            CavMin = Tune("TPS_CAVMIN", 0.25f);
            float k = 3.4f / Tune("TPS_EXP", 1.9f);
            EmisTune = new Vector3(EmisTune.X * k, EmisTune.Y, EmisTune.Z * k);
            // pass 4
            FogDensity = Tune("TPS_FOG", 0.006f); FogStart = Tune("TPS_FOGSTART", 25f);
            FogColor = new Vector3(0.4f, 0.25098f, 0.101961f) * Tune("TPS_FOGI", 0.3f);
            EnvGain = Tune("TPS_ENVGAIN", 0.3f); KeyNear = new Vector2(Tune("TPS_KEYNEAR", 0.1f), Tune("TPS_KEYNEARD", 14f)); KeyFloor = Tune("TPS_KEYFLOOR", 0.25f); AlbGain = Tune("TPS_ALBGAIN", 1.7f);
            MetalDiff = Tune("TPS_METALDIFF", 1f); SpecMin = Tune("TPS_SPECMIN", 0.1f); KeySpec = Tune("TPS_KEYSPEC", 0.5f);
            AmbFlat = new Vector3(0.55f, 0.52f, 0.47f) * Tune("TPS_AMBFLAT", 0.2f);
            // pass 5: warm saturated ambient on floors; tile_steel (untextured white metal, the grate slats / underlay) tinted dark olive (Godot reflects a dark environment off it)
            AmbWarm = new Vector3(0.30f, 0.19f, 0.07f) * Tune("TPS_AMBWARM", 0.12f);
            SteelTint = new Vector3(Tune("TPS_STEELR", 0.10f), Tune("TPS_STEELG", 0.10f), Tune("TPS_STEELB", 0.04f));
            PlayerTint = new Vector3(Tune("TPS_PTR", 0.4f), Tune("TPS_PTG", 0.14f), Tune("TPS_PTB", 0.08f));
            // fake teal bounce from the glowing teal strips on the left of the spawn walkway (spawn-relative: side, ahead, height, gain)
            var t = (Environment.GetEnvironmentVariable("TPS_TEAL") ?? "-5,4,0.3,1").Split(',');
            float ts = float.Parse(t[0], ci), ta = float.Parse(t[1], ci), th = float.Parse(t[2], ci), tg = float.Parse(t[3], ci);
            if (tg > 0)
            {
                var sp = new Vector3(64.8183f, -1.0765f, 78.7639f); var fw = new Vector3(0.817573f, 0f, 0.575826f); var lf = new Vector3(0.575826f, 0f, -0.817573f);
                _lights.Add(new L { Pos = sp + lf * ts + fw * ta + new Vector3(0, th, 0), Color = new Vector3(0.1f, 0.24f, 0.28f) * 100f * tg });
            }
        }

        public static float Tune(string n, float d) { var v = Environment.GetEnvironmentVariable(n); return v != null ? float.Parse(v, System.Globalization.CultureInfo.InvariantCulture) : d; }

        public void Load(string contentRoot)
        {
            string p = Path.Combine(contentRoot, "Data", "lights.json");
            if (!File.Exists(p)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(p));
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                Vector3 V(string k) { var a = e.GetProperty(k); return new Vector3(a[0].GetSingle(), a[1].GetSingle(), a[2].GetSingle()); }
                var l = new L { Pos = V("pos"), Color = V("color") * e.GetProperty("intensity").GetSingle() / 100f * 100f, Spot = e.GetProperty("type").GetString() == "spot" };
                var d = V("dir"); l.Dir = d.LengthSquared() > 1e-6f ? Vector3.Normalize(d) : Vector3.Down;
                if (l.Spot) { l.Inner = e.GetProperty("inner").GetSingle(); l.Outer = e.GetProperty("outer").GetSingle(); }
                _lights.Add(l);
            }
            Console.WriteLine($"[lights] loaded {_lights.Count}");
        }

        /// <summary>Menu backdrop: replace the level lights with a custom set (see MenuBackdrop).</summary>
        public void ClearLights() => _lights.Clear();
        /// <summary>Per-frame transient point lights (bullets): replaces the previous set; merged into the nearest-8 selection in <see cref="Apply"/>. Range is per light.</summary>
        public void SetDynamicPoints(List<(Vector3 Pos, Vector3 Color, float Range)> pts) { _dyn.Clear(); foreach (var p in pts) _dyn.Add(new L { Pos = p.Pos, Dir = Vector3.Down, Color = p.Color, Range = p.Range }); }
        readonly List<L> _dyn = new(); readonly List<L> _all = new(); readonly List<L> _dynSpot = new();
        /// <summary>Moving spot lights (forklifts): compete with the static lights for the nearest-8 set (no priority slots); one outside <see cref="DynSpotReach"/> of the focus is skipped.</summary>
        public void SetDynamicSpots(List<(Vector3 Pos, Vector3 Dir, Vector3 Color, float Inner, float Outer, float Range)> s) { _dynSpot.Clear(); foreach (var p in s) _dynSpot.Add(new L { Pos = p.Pos, Dir = Vector3.Normalize(p.Dir), Color = p.Color, Spot = true, Inner = p.Inner, Outer = p.Outer, Range = p.Range }); }
        public float DynSpotReach = 70f;
        public void AddSpot(Vector3 pos, Vector3 dir, Vector3 color, float innerRad, float outerRad, bool shadowed = false) => _lights.Add(new L { Pos = pos, Dir = Vector3.Normalize(dir), Color = color, Spot = true, Inner = innerRad, Outer = outerRad, Shadowed = shadowed });

        public void Apply(Effect fx, Vector3 focus)
        {
            Comparison<L> byDist = (a, b) => Vector3.DistanceSquared(a.Pos, focus).CompareTo(Vector3.DistanceSquared(b.Pos, focus));
            _lights.Sort(byDist); _dyn.Sort(byDist);
            _all.Clear(); for (int k = 0; k < _dyn.Count && k < 3; k++) _all.Add(_dyn[k]);   // transient lights (bullets) take up to 3 of the 8 slots, the rest are the nearest static lights
            _all.AddRange(_lights);
            if (_dynSpot.Count > 0)
            {
                int s0 = _all.Count; foreach (var d in _dynSpot) if (Vector3.Distance(d.Pos, focus) < DynSpotReach) _all.Add(d);
                if (_all.Count > s0) { int st = Math.Min(3, _dyn.Count); _all.Sort(st, _all.Count - st, Comparer<L>.Create(byDist)); }
            }
            for (int i = 0; i < 8; i++)
            {
                if (i < _all.Count)
                {
                    var l = _all[i];
                    _pos[i] = new Vector4(l.Pos, l.Range > 0f ? l.Range : LightRange);
                    _col[i] = new Vector4(l.Color * LightScale, l.Spot ? MathF.Cos(l.Outer) : -3f);
                    _dir[i] = new Vector4(l.Dir, l.Spot ? MathF.Cos(l.Inner) : -2f);
                }
                else { _pos[i] = new Vector4(0, -1000, 0, 1); _col[i] = new Vector4(0, 0, 0, -3); _dir[i] = new Vector4(0, -1, 0, -2); }
            }
            int slot = -1; for (int q = 0; q < 8 && q < _all.Count; q++) if (_all[q].Shadowed) slot = q;
            fx.Parameters["ShadowSlot"]?.SetValue((float)slot);
            if (Spots != null) Spots.Bind(fx, _all); else SpotShadows.Clear(fx);
            Set(fx, "LPos", _pos); Set(fx, "LCol", _col); Set(fx, "LDirI", _dir);
            Set(fx, "LightDir", KeyDir); Set(fx, "LightColor", KeyColor);
            Set(fx, "AmbientColor", Ambient); Set(fx, "AmbGround", AmbGround); fx.Parameters["AmbShape"]?.SetValue(AmbShape); fx.Parameters["CavMin"]?.SetValue(CavMin); Set(fx, "EmisTune", EmisTune); fx.Parameters["FogStart"]?.SetValue(FogStart); Set(fx, "EnvUp", EnvUp); Set(fx, "EnvHorizon", EnvHorizon); Set(fx, "EnvDown", EnvDown);
            fx.Parameters["EnvGain"]?.SetValue(EnvGain); fx.Parameters["KeyNear"]?.SetValue(KeyNear); fx.Parameters["KeyFloor"]?.SetValue(KeyFloor); fx.Parameters["AlbGain"]?.SetValue(AlbGain); fx.Parameters["MetalDiff"]?.SetValue(MetalDiff); fx.Parameters["SpecMin"]?.SetValue(SpecMin); fx.Parameters["KeySpec"]?.SetValue(KeySpec); Set(fx, "AmbFlat", AmbFlat); Set(fx, "AmbWarm", AmbWarm);
            Set(fx, "FogColor", FogColor); fx.Parameters["FogDensity"]?.SetValue(FogDensity); fx.Parameters["HdrScale"]?.SetValue(HdrScale);
        }
        static void Set(Effect fx, string n, Vector3 v) => fx.Parameters[n]?.SetValue(v);
        static void Set(Effect fx, string n, Vector4[] v) => fx.Parameters[n]?.SetValue(v);
    }
}
