using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Bullet, muzzle-flash and hit visuals drawn as additive unlit HDR billboards (Glow.fx) inside the HDR scene pass so bloom picks them up.
    /// Stand-ins for the Godot scenes (bullet.tscn MainBody/Trail/Blast*, player.tscn MuzzleFlash): same colours (bullet_material albedo (0.149,0.749,1.502),
    /// MuzzleFlash (0.125,0.627,1.0)), same lifetimes where they matter (muzzle 0.1 s, blast flash 0.15-0.25 s, sparks 0.3 s, light balls ~1 s), simplified
    /// shapes (glow quads instead of meshes/GPU particles). Effects are timestamped in simulation time so a `--sim ... --shot` frame shows their true state.
    /// </summary>
    public sealed class CombatEffects
    {
        enum Kind { Muzzle, Hit, Explosion, Puff, Sparks, Impact }
        struct Fx { public Kind Kind; public Vector3 Pos, Dir; public float T0; public int Seed; public float FloorY; }
        /// <summary>Ground height lookup (x, z, maxY) -> floor Y, used for the LightParticle bounce; null = no floor.</summary>
        public Func<float, float, float, float?> GroundY;
        readonly List<Fx> _fx = new();
        readonly Effect _glow, _fxs; readonly Texture2D _tex, _myst, _barrelTex, _noise;
        readonly List<VertexPositionColorTexture> _smoke = new(), _tube = new(), _ember = new();
        readonly List<(VertexPositionColorTexture[] V, float Life)> _barrel = new();
        public float BeamGain = SceneLighting.Tune("TPS_BEAMGAIN", 1.2f), SmokeGain = SceneLighting.Tune("TPS_SMOKEGAIN", 1f), BulletLightGain = SceneLighting.Tune("TPS_BULLETLIGHT", 6f);
        public static CombatEffects Load(GraphicsDevice gd, Microsoft.Xna.Framework.Content.ContentManager c) =>
            new CombatEffects(gd, c.Load<Effect>("Effects/Glow"), c.Load<Effect>("Effects/Fx"), c.Load<Texture2D>("Textures/Fx/blue_myst"), c.Load<Texture2D>("Textures/Fx/BarrelSmokeTexture"));
        readonly List<VertexPositionColorTexture> _blue = new(), _white = new(), _cyan = new(), _red = new(), _fire = new();
        static readonly BlendState Add = new BlendState { ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One, AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.One };
        public Vector3 BlueTint = new Vector3(0.149f, 0.749f, 1.502f) * SceneLighting.Tune("TPS_FXGAIN", 3f);       // bullet_material.tres albedo_color
        public Vector3 WhiteTint = new Vector3(1f, 1f, 1f) * SceneLighting.Tune("TPS_FXGAIN", 3f) * 1.4f;
        public Vector3 CyanTint = new Vector3(0f, 0.486f, 1f) * SceneLighting.Tune("TPS_FXGAIN", 3f) * 1.3f;         // BlastLight/LightParticle emission (0,0.486,1)
        public Vector3 RedTint = new Vector3(1.0f, 0.04f, 0.02f) * SceneLighting.Tune("TPS_FXGAIN", 3f) * 2f;          // LaserMaterial color1 (1,0,0)
        public Vector3 FireTint = new Vector3(1.0f, 0.42f, 0.08f) * SceneLighting.Tune("TPS_FXGAIN", 3f) * 1.6f;
        int _seedCounter;

        public CombatEffects(GraphicsDevice gd, Effect glow, Effect fxs, Texture2D myst, Texture2D barrel)
        {
            _glow = glow; _fxs = fxs; _myst = myst; _barrelTex = barrel; _noise = MakeNoise(gd);
            const int N = 64; var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float r = Math.Min(1f, new Vector2((x + 0.5f) / N * 2 - 1, (y + 0.5f) / N * 2 - 1).Length());
                float a = MathF.Pow(1f - r, 2.2f);
                px[y * N + x] = new Color(255, 255, 255, (int)(255 * a));
            }
            _tex = new Texture2D(gd, N, N, false, SurfaceFormat.Color); _tex.SetData(px);
        }

        /// <summary>Tileable 64x64 value noise (stand-in for the NoiseTexture2D + FastNoiseLite resources LaserBodyNoise / smoke noise; seamless, normalised to 0..1).</summary>
        static Texture2D MakeNoise(GraphicsDevice gd)
        {
            const int N = 64; var v = new float[N * N]; float mn = 9, mx = -9;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float sum = 0, amp = 1;
                for (int o = 0; o < 3; o++) { int cells = 4 << o; float fx = x / (float)N * cells, fy = y / (float)N * cells; int ix = (int)fx, iy = (int)fy; float tx = fx - ix, ty = fy - iy; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                    float a = Hash(o * 9973 + (iy % cells) * 131 + ix % cells), b = Hash(o * 9973 + (iy % cells) * 131 + (ix + 1) % cells), c = Hash(o * 9973 + ((iy + 1) % cells) * 131 + ix % cells), d = Hash(o * 9973 + ((iy + 1) % cells) * 131 + (ix + 1) % cells);
                    sum += amp * MathHelper.Lerp(MathHelper.Lerp(a, b, tx), MathHelper.Lerp(c, d, tx), ty); amp *= 0.5f; }
                v[y * N + x] = sum; mn = Math.Min(mn, sum); mx = Math.Max(mx, sum);
            }
            var px = new Color[N * N]; for (int i = 0; i < px.Length; i++) { byte g = (byte)(255 * (v[i] - mn) / (mx - mn)); px[i] = new Color(g, g, g, (byte)255); }
            var t = new Texture2D(gd, N, N, false, SurfaceFormat.Color); t.SetData(px); return t;
        }

        public void Muzzle(Vector3 pos, Vector3 dir, float now) => _fx.Add(new Fx { Kind = Kind.Muzzle, Pos = pos, Dir = dir, T0 = now });
        public void Explosion(Vector3 pos, float now) => _fx.Add(new Fx { Kind = Kind.Explosion, Pos = pos, Dir = Vector3.Up, T0 = now, Seed = ++_seedCounter * 104729 });

        /// <summary>Red robot laser: RayMesh tube + LaserShader noise cut-off, LaserEmber and barrel Smoke, driven by the shoot-clip clock (see <see cref="FxSpec.BeamParams"/>); one per shooting robot, drawn (and cleared) by the next <see cref="Draw"/>.</summary>
        public void Beam(Vector3 from, Vector3 to, float clock) { if (_beams.Count < 8) _beams.Add((from, to, clock)); }
        readonly List<(Vector3 From, Vector3 To, float Clock)> _beams = new();

        /// <summary>Transient lights for this frame: bullets (OmniLight3D, bullet.tscn) and the fading light of a bullet that just hit (explode animation). Call before the scene draw.</summary>
        public void CollectLights(SceneLighting lighting, float now, ReadOnlySpan<Projectile> bullets, float alpha)
        {
            _lightBuf.Clear();
            for (int i = 0; i < bullets.Length; i++)
                if (bullets[i].Alive) _lightBuf.Add((Vector3.Lerp(bullets[i].Previous, bullets[i].Position, alpha), FxSpec.BulletLightColor * (FxSpec.BulletLightEnergy * BulletLightGain), FxSpec.BulletLightRange));
            foreach (var f in _fx)
            {
                if (f.Kind != Kind.Hit) continue; float age = now - f.T0; if (age < 0f || age > 0.85f) continue;
                FxSpec.BulletLight(age, out float e, out float r); if (e > 0f && r > 0.01f) _lightBuf.Add((f.Pos, FxSpec.BulletLightColor * (e * BulletLightGain), r));
            }
            lighting.SetDynamicPoints(_lightBuf);
        }

        // ---- forklift exhaust + spot light (flying_forklift.tscn), stateless from the level clock ----------------------------------------------------
        public float ForkLightGain = SceneLighting.Tune("TPS_FORKLIGHT", FxSpec.ForkLightEnergy * 6f) / FxSpec.ForkLightEnergy, ForkGain = SceneLighting.Tune("TPS_FORKGAIN", 6f);
        readonly List<(Vector3, Vector3, Vector3, float, float, float)> _forkLights = new();
        readonly List<VertexPositionColorTexture> _fork = new();
        /// <summary>The forklift SpotLight3D(s) into the nearest-lights set. Light = colour x energy 12 x the same gain the bullet OmniLight uses (BulletLightGain 6: Godot energy -> LCol), range 50, outer = spot_angle 50 deg, inner 0 (spot_attenuation 1 falls off linearly from the axis).</summary>
        public void SetForkliftLights(SceneLighting lighting, LevelScene scene, float levelT)
        {
            _forkLights.Clear();
            for (int i = 0; i < scene.ForkliftRoots.Count; i++)
            {
                Matrix w = scene.ForkliftWorld(i, levelT);
                _forkLights.Add((Vector3.Transform(FxSpec.ForkLightPos, w), Vector3.TransformNormal(FxSpec.ForkLightDir, w), FxSpec.ForkLightColor * (FxSpec.ForkLightEnergy * ForkLightGain), 0f, MathHelper.ToRadians(FxSpec.ForkLightAngleDeg), FxSpec.ForkLightRange));
            }
            lighting.SetDynamicSpots(_forkLights);
        }
        /// <summary>Exhaust of the 2 forklifts (4 emitters x 20 particles each); skipped for a forklift farther than 120 m or behind the camera.</summary>
        public void DrawForklifts(GraphicsDevice gd, LevelScene scene, Matrix view, Matrix proj, Vector3 camPos, float levelT, float hdrScale)
        {
            if (scene == null || scene.ForkliftRoots.Count == 0) return;
            Vector3 right = new Vector3(view.M11, view.M21, view.M31), up = new Vector3(view.M12, view.M22, view.M32), fwd = new Vector3(-view.M13, -view.M23, -view.M33);
            _fork.Clear();
            for (int i = 0; i < scene.ForkliftRoots.Count; i++)
            {
                Vector3 c = scene.ForkliftWorld(i, levelT).Translation - camPos;
                if (c.LengthSquared() > 120f * 120f || Vector3.Dot(c, fwd) < -10f) continue;
                int fi = i; Func<float, Matrix> wAt = t => scene.ForkliftWorld(fi, t);
                for (int e = 0; e < FxSpec.ForkEmitters.Length; e++)
                    for (int k = 0; k < FxSpec.ForkCount; k++)
                    {
                        if (!FxSpec.ForkParticle(e, k, levelT, wAt, out var p, out float t01, out _)) continue;
                        Vector4 col = FxSpec.ForkColor(t01); float a = col.W * FxSpec.ForkAlpha;
                        float r = 0.5f * FxSpec.ForkScale(t01);
                        if (a <= 0.002f || r <= 0f) continue;
                        var cc = new Color(col.X, col.Y, col.Z, a);
                        var v0 = new VertexPositionColorTexture(p + (-right + up) * r, cc, new Vector2(0, 0)); var v1 = new VertexPositionColorTexture(p + (right + up) * r, cc, new Vector2(1, 0));
                        var v2 = new VertexPositionColorTexture(p + (-right - up) * r, cc, new Vector2(0, 1)); var v3 = new VertexPositionColorTexture(p + (right - up) * r, cc, new Vector2(1, 1));
                        _fork.Add(v0); _fork.Add(v1); _fork.Add(v2); _fork.Add(v1); _fork.Add(v3); _fork.Add(v2);
                    }
            }
            if (_fork.Count < 3) return;   // never draw an empty / <3-vertex array
            gd.BlendState = Add; gd.DepthStencilState = DepthStencilState.DepthRead; gd.RasterizerState = RasterizerState.CullNone; gd.SamplerStates[0] = SamplerState.LinearClamp;
            _glow.Parameters["ViewProj"].SetValue(view * proj); _glow.Parameters["Tex"].SetValue(_tex);
            Flush(gd, _fork, Vector3.One * ForkGain * hdrScale);
        }
        readonly List<(Vector3 Pos, Vector3 Color, float Range)> _lightBuf = new();

        static void ColQuad(List<VertexPositionColorTexture> l, Vector3 c, Vector3 r, Vector3 u, float radius, Color col)
        {
            if (col.A < 1 || radius <= 0f) return;
            var a = new VertexPositionColorTexture(c + (-r + u) * radius, col, new Vector2(0, 0)); var b = new VertexPositionColorTexture(c + (r + u) * radius, col, new Vector2(1, 0));
            var d = new VertexPositionColorTexture(c + (-r - u) * radius, col, new Vector2(0, 1)); var e = new VertexPositionColorTexture(c + (r - u) * radius, col, new Vector2(1, 1));
            l.Add(a); l.Add(b); l.Add(d); l.Add(b); l.Add(e); l.Add(d);
        }
        static Vector3 RandSphere(int seed) { float z = Hash(seed) * 2 - 1, a = Hash(seed + 1) * MathHelper.TwoPi, r = MathF.Sqrt(1 - z * z); return new Vector3(r * MathF.Cos(a), z, r * MathF.Sin(a)); }

        /// <summary>RayMesh stand-in: a 12-sided tube from the cannon to the hit, radius = LaserMeshRadius * traversal_scale, UV.x = distance / 8 m.</summary>
        void BuildTube(Vector3 from, Vector3 to, float radius)
        {
            Vector3 d = to - from; float len = d.Length(); if (len < 1e-3f) return; d /= len;
            Vector3 s0 = from + d * Math.Min(FxSpec.LaserStart, len); float u0 = Math.Min(FxSpec.LaserStart, len) / FxSpec.LaserMeshLength, u1 = len / FxSpec.LaserMeshLength;
            Vector3 t1 = Vector3.Normalize(Vector3.Cross(d, MathF.Abs(d.Y) > 0.9f ? Vector3.Right : Vector3.Up)), t2 = Vector3.Cross(d, t1); const int N = 12;
            for (int k = 0; k < N; k++)
            {
                float a0 = k / (float)N * MathHelper.TwoPi, a1 = (k + 1) / (float)N * MathHelper.TwoPi;
                Vector3 o0 = (t1 * MathF.Cos(a0) + t2 * MathF.Sin(a0)) * radius, o1 = (t1 * MathF.Cos(a1) + t2 * MathF.Sin(a1)) * radius;
                var p00 = new VertexPositionColorTexture(s0 + o0, Color.White, new Vector2(u0, k / (float)N)); var p01 = new VertexPositionColorTexture(s0 + o1, Color.White, new Vector2(u0, (k + 1) / (float)N));
                var p10 = new VertexPositionColorTexture(to + o0, Color.White, new Vector2(u1, k / (float)N)); var p11 = new VertexPositionColorTexture(to + o1, Color.White, new Vector2(u1, (k + 1) / (float)N));
                _tube.Add(p00); _tube.Add(p10); _tube.Add(p01); _tube.Add(p10); _tube.Add(p11); _tube.Add(p01);
            }
        }

        /// <summary>part_disappear.tscn: cyan MiniBlasts (16 balls, 0.3 s) + a cyan puff rising from 0.2 s for 1.5 s (smoke quad stand-in).</summary>
        public void DebrisPuff(Vector3 pos, float now) => _fx.Add(new Fx { Kind = Kind.Puff, Pos = pos, Dir = Vector3.Up, T0 = now, Seed = ++_seedCounter * 15485863 });
        /// <summary>red_robot.tscn DetachSpark1/2: 50 cyan sparks, 0.4 s, speed 1-10, upward hemisphere.</summary>
        public void DetachSparks(Vector3 pos, float now) => _fx.Add(new Fx { Kind = Kind.Sparks, Pos = pos, Dir = Vector3.Up, T0 = now, Seed = ++_seedCounter * 32452843 });
        /// <summary>Cyan glow around a fading debris part (stand-in for the part.gd emission_cutout electricity shell); drawn (and cleared) by the next Draw.</summary>
        public void Halo(Vector3 pos, float radius, float alpha) { if (_halos.Count < 64) _halos.Add((pos, radius, alpha)); }
        readonly List<(Vector3 P, float R, float A)> _halos = new List<(Vector3, float, float)>();

        /// <summary>red_robot/laser/impact_effect/impact_effect.tscn instanced at the beam's hit point (red_robot.gd:128-130).</summary>
        public void LaserImpact(Vector3 pos, float now) => _fx.Add(new Fx { Kind = Kind.Impact, Pos = pos, Dir = Vector3.Up, T0 = now, Seed = ++_seedCounter * 49979687 });

        public void Hit(Vector3 pos, Vector3 normal, float now) => _fx.Add(new Fx { Kind = Kind.Hit, Pos = pos, Dir = Vector3.Normalize(normal), T0 = now, Seed = ++_seedCounter * 7919, FloorY = GroundY?.Invoke(pos.X, pos.Z, pos.Y + 0.3f) ?? float.NegativeInfinity });

        static float Hash(int n) { uint h = (uint)n * 747796405u + 2891336453u; h = ((h >> (int)((h >> 28) + 4)) ^ h) * 277803737u; h = (h >> 22) ^ h; return (h & 0xFFFFFF) / (float)0xFFFFFF; }

        static void Quad(List<VertexPositionColorTexture> l, Vector3 c, Vector3 r, Vector3 u, float radius, float alpha)
        {
            if (alpha <= 0.002f || radius <= 0f) return;
            var col = new Color(255, 255, 255, (int)(MathHelper.Clamp(alpha, 0f, 1f) * 255f));
            var a = new VertexPositionColorTexture(c + (-r + u) * radius, col, new Vector2(0, 0));
            var b = new VertexPositionColorTexture(c + (r + u) * radius, col, new Vector2(1, 0));
            var d = new VertexPositionColorTexture(c + (-r - u) * radius, col, new Vector2(0, 1));
            var e = new VertexPositionColorTexture(c + (r - u) * radius, col, new Vector2(1, 1));
            l.Add(a); l.Add(b); l.Add(d); l.Add(b); l.Add(e); l.Add(d);
        }

        /// <summary>Draws live bullets and effects. <paramref name="now"/> = simulation time (incl. the fractional tick), <paramref name="alpha"/> = tick interpolation.</summary>
        public void Draw(GraphicsDevice gd, Matrix view, Matrix proj, float now, ReadOnlySpan<Projectile> bullets, float alpha, float hdrScale)
        {
            Vector3 right = new Vector3(view.M11, view.M21, view.M31), up = new Vector3(view.M12, view.M22, view.M32);
            _blue.Clear(); _white.Clear(); _cyan.Clear(); _red.Clear(); _fire.Clear();
            _smoke.Clear(); _tube.Clear(); _ember.Clear(); _barrel.Clear();
            float beamE = 0f, beamCut = 1f, beamOff = 0f;
            foreach (var (bFrom, bTo, clock) in _beams)
            {
                FxSpec.BeamParams(clock, out float trav, out float en, out float cut, out float off);
                Vector3 bd = bTo - bFrom; float bl = bd.Length(); if (bl < 1e-3f) continue; Vector3 bdn = bd / bl;
                if (trav > 0.001f && cut < 0.999f) { BuildTube(bFrom, bTo, FxSpec.LaserMeshRadius * trav); beamE = en; beamCut = cut; beamOff = off; Quad(_red, bTo, right, up, 0.5f * Math.Min(1f, trav) * (1f - cut), 0.9f * (1f - cut)); }   // + impact glow
                // BuildupParticles (red_robot.tscn:10675-10699): 64 thin spheres, 0.4 s, emitting 0.2..1.0 s, sphere SURFACE r 1 at RayFrom +1.476, radial accel -10 (implode), colour alpha 0 -> 1 from 73.7 % of life
                if (clock > FxSpec.BuildupStart) for (int k = 0; k < FxSpec.BuildupCount; k++)
                {
                    float rel = clock - FxSpec.BuildupStart - FxSpec.BuildupLife * k / FxSpec.BuildupCount; if (rel < 0f) continue;
                    int cyc = (int)(rel / FxSpec.BuildupLife); float t = rel - cyc * FxSpec.BuildupLife;
                    if (FxSpec.BuildupStart + cyc * FxSpec.BuildupLife + FxSpec.BuildupLife * k / FxSpec.BuildupCount >= FxSpec.BuildupEnd) continue;   // emitting stops at 1.0 s
                    float lt = t / FxSpec.BuildupLife; if (lt < 0.73741f) continue; float al = (lt - 0.73741f) / (1f - 0.73741f);
                    Vector3 pc = bFrom + bdn * 1.476f + RandSphere(k * 977 + cyc * 131) * (1f - 5f * t * t);
                    Quad(_ember, pc, right, up, 0.02f * 3.5f, al);
                }
                // LaserCenter (red_robot.tscn:10725-10735): one r 0.1 sphere at RayFrom +1.5 (bone y), born at 0.2 s (emitting window 0.2..0.4), lives 1.5 s, scale Curve_8fal6; material is a binary resource (colour = guess: white-hot core in red)
                float lca = FxSpec.LaserCenterAge(clock);
                if (lca >= 0f) { float sc = FxSpec.LaserCenterScale(lca / FxSpec.LaserCenterLife); if (sc > 0.001f) { Vector3 lp = bFrom + bdn * FxSpec.LaserCenterY; Quad(_white, lp, right, up, FxSpec.LaserCenterRadius * sc * 2.2f, 1f); Quad(_red, lp, right, up, FxSpec.LaserCenterRadius * sc * 6f, 0.9f); } }
                // LaserEmber (200, life 2 +-85 %, explosiveness .95, gravity +0.8 y, ramp (1,.68,.68) -> black @ .45)
                float ea = clock - FxSpec.EmberStart;
                if (ea > 0f) for (int k = 0; k < FxSpec.EmberCount; k++)
                {
                    float life = FxSpec.EmberLife * (1f - FxSpec.EmberRandom * Hash(k * 17 + 3)), a = ea - FxSpec.SpawnDelay(k, FxSpec.EmberCount, FxSpec.EmberLife, 0.95f); if (a < 0f || a > life) continue;
                    float t = a / life, br = t < 0.45f ? 1f - t / 0.45f : 0f; if (br <= 0.01f) continue;
                    Vector3 p = bFrom + bdn * (1.25f + Hash(k * 31 + 7) * Math.Max(0f, bl - 1.25f)) + new Vector3(0, 0.4f * a * a, 0);
                    Quad(_ember, p, right, up, 0.02f * MathHelper.Lerp(0.38f, 0.98f, t) * 3.5f, br);
                }
                // barrel Smoke (3 plane particles from 1.55 s, life 1.5, explosiveness .5; smoke.gdshader dissolve)
                float sa = clock - FxSpec.SmokeStart;
                if (sa > 0f) for (int k = 0; k < FxSpec.SmokeCount; k++)
                {
                    float a = sa - FxSpec.SpawnDelay(k, FxSpec.SmokeCount, FxSpec.SmokeLife, FxSpec.SmokeExplosiveness); if (a < 0f || a > FxSpec.SmokeLife) continue; float t = a / FxSpec.SmokeLife;
                    // colour ramp offsets 0 / .178 / .797 / 1 (Gradient_1e1e6)
                    Vector4 c = t < 0.17765f ? Vector4.Lerp(new Vector4(.9647f, .8353f, .8353f, 0), new Vector4(.8196f, .7098f, .7098f, 1), t / 0.17765f) : t < 0.796562f ? Vector4.Lerp(new Vector4(.8196f, .7098f, .7098f, 1), new Vector4(.1373f, .1137f, .1137f, 1), (t - 0.17765f) / (0.796562f - 0.17765f)) : Vector4.Lerp(new Vector4(.1373f, .1137f, .1137f, 1), new Vector4(.1765f, .149f, .149f, 0), (t - 0.796562f) / (1f - 0.796562f));
                    float sc = MathHelper.Lerp(0.3f, 1.3f, Hash(k * 13 + 5)) * MathHelper.Lerp(0.290909f, 0.927273f, t);
                    var v = new List<VertexPositionColorTexture>(6); ColQuad(v, bFrom + bdn * 1.3f + new Vector3(0, 0.1f * a * a, 0), right, up, sc * 0.5f, new Color(c.X, c.Y, c.Z, c.W));
                    _barrel.Add((v.ToArray(), t));
                }
            }
            _beams.Clear(); float beamEn = beamE, beamCu = beamCut, beamOf = beamOff;
            foreach (var h in _halos) Quad(_cyan, h.P, right, up, h.R, h.A);
            _halos.Clear();
            for (int i = 0; i < bullets.Length; i++)
            {
                if (!bullets[i].Alive) continue;
                Vector3 p = Vector3.Lerp(bullets[i].Previous, bullets[i].Position, alpha), d = bullets[i].Dir;
                float pulse = 1f + 0.12f * MathF.Sin(now * 45f + i);
                Quad(_blue, p, right, up, 0.30f * pulse, 0.95f);                    // MainBody glow (blue, additive)
                Quad(_white, p, right, up, 0.085f, 1f);                             // MeshInstance3D core (unshaded white sphere r 0.05)
                for (int k = 1; k <= 6; k++) { float f = 1f - k / 7f; Quad(_blue, p - d * (0.14f * k), right, up, 0.2f * f, 0.55f * f); } // Trail
            }
            for (int i = _fx.Count - 1; i >= 0; i--)
            {
                var f = _fx[i]; float age = now - f.T0;
                if (age < 0f) continue;
                if (f.Kind == Kind.Explosion)
                {
                    if (age > 1.5f) { _fx.RemoveAt(i); continue; }
                    // robot death: flash, expanding fireball, ember sprays (stand-in for explosion.png billboards + sparks)
                    if (age < 0.12f) Quad(_white, f.Pos, right, up, 1.6f * (1f - age / 0.12f), 1f - age / 0.12f);
                    if (age < 0.8f) Quad(_fire, f.Pos, right, up, 0.8f + age * 4.5f, MathF.Pow(1f - age / 0.8f, 1.3f));
                    for (int k = 0; k < 16; k++)
                    {
                        float a = Hash(f.Seed + k) * MathHelper.TwoPi, e = (Hash(f.Seed + k + 33) - 0.2f) * 1.4f, sp = 1.5f + Hash(f.Seed + k + 77) * 4f;
                        Vector3 d = new Vector3(MathF.Cos(a) * MathF.Cos(e), MathF.Sin(e), MathF.Sin(a) * MathF.Cos(e));
                        Quad(_fire, f.Pos + d * sp * age + new Vector3(0, -4.9f * age * age, 0), right, up, 0.11f * (1f - age / 1.5f), 1f - age / 1.5f);
                    }
                    continue;
                }
                if (f.Kind == Kind.Sparks)
                {
                    if (age > 0.4f) { _fx.RemoveAt(i); continue; }
                    for (int k = 0; k < 50; k++)
                    {
                        float a = Hash(f.Seed + k) * MathHelper.TwoPi, e = Hash(f.Seed + k + 33) * 1.5708f, sp = 1f + Hash(f.Seed + k + 77) * 9f, life = 0.4f * (1f - 0.42f * Hash(f.Seed + k + 5));
                        if (age > life) continue;
                        Vector3 d = new Vector3(MathF.Cos(a) * MathF.Sin(e), MathF.Cos(e), MathF.Sin(a) * MathF.Sin(e));
                        Quad(_cyan, f.Pos + d * sp * age, right, up, 0.07f * (1f - age / life), 1f - age / life);
                    }
                    continue;
                }
                if (f.Kind == Kind.Puff)
                {
                    if (age > 1.7f) { _fx.RemoveAt(i); continue; }
                    if (age < 0.3f) for (int k = 0; k < 16; k++)   // MiniBlasts: emission sphere r 0.5, scale curve 0 -> 1 (0.46) -> 0
                    {
                        float s = age / 0.3f, sc = s < 0.46f ? s / 0.46f : (1f - s) / 0.54f; Vector3 o = new Vector3(Hash(f.Seed + k) - 0.5f, Hash(f.Seed + k + 9) - 0.5f, Hash(f.Seed + k + 19) - 0.5f) * 1f;
                        Quad(_cyan, f.Pos + o, right, up, 0.2f * sc, 1f);
                    }
                    float pa = age - 0.2f;
                    if (pa > 0f) { float s = pa / 1.5f; Quad(_cyan, f.Pos + new Vector3(0, 2f * pa * pa * 0.5f, 0), right, up, 0.3f + 0.9f * Math.Min(1f, s / 0.38f), 0.55f * MathF.Sin(MathHelper.Pi * MathHelper.Clamp(s, 0f, 1f))); }
                    continue;
                }
                if (f.Kind == Kind.Impact)
                {
                    if (age > 1.4f) { _fx.RemoveAt(i); continue; }
                    // impact_effect.tscn (all emitters have NO initial velocity, so particles only move by gravity / emission shape)
                    if (age < FxSpec.ImpactRaysLife) Quad(_red, f.Pos, right, up, 0.9f * MathHelper.Lerp(FxSpec.ImpactRaysScale0, 1f, age / FxSpec.ImpactRaysLife), 0.8f * (1f - age / FxSpec.ImpactRaysLife));   // LightRays: 40 red ray quads, 0.2 s, stacked at the point
                    if (age < FxSpec.ImpactCenterLife) Quad(_red, f.Pos, right, up, 0.5f * FxSpec.ImpactCenterScale(age / FxSpec.ImpactCenterLife), 1f);   // BlastCenter: red emissive sphere r 0.5, 0.2 s, scale 0 -> 1
                    float ba = age - FxSpec.ImpactBlastStart;
                    if (ba > 0f && ba < FxSpec.ImpactBlastLife) { float t = ba / FxSpec.ImpactBlastLife; Quad(t < 0.5f ? _white : _red, f.Pos, right, up, 0.3f * MathHelper.Lerp(1.2f, 5f, t), 1f - t); }   // Blast: 1 billboard 0.3 s from 0.2 s, scale 1.2 -> 5, white -> red (mesh size ASSUMED 0.3 m radius)
                    float ea = age - FxSpec.ImpactEmberStart;   // ExplosionEmbers: 50 tiny spheres, 1 s, from 0.2 s, gravity (0,-2,0), colour white -> red -> grey
                    if (ea > 0f) for (int k = 0; k < FxSpec.ImpactEmberCount; k++)
                    {
                        float a = ea - FxSpec.SpawnDelay(k, FxSpec.ImpactEmberCount, 1f, 0.9f); if (a < 0f || a > 1f) continue;
                        float sc = a < 0.81f ? 1f : (1f - a) / 0.19f, red = a < 0.3137f ? 1f - a / 0.3137f : 0f;
                        Quad(red > 0.5f ? _ember : _fire, f.Pos + new Vector3(0, -a * a, 0), right, up, 0.03f * 3.5f * sc, 1f - 0.5f * a);
                    }
                    float sa = age - FxSpec.ImpactSmokeStart;   // SmokeParticle: 20 quads (2x2), 1 s, emission sphere r 1, gravity +2, scale 0 -> 1, colour red(a0) -> red -> grey -> a0
                    if (sa > 0f) for (int k = 0; k < FxSpec.ImpactSmokeCount; k++)
                    {
                        float a = sa - FxSpec.SpawnDelay(k, FxSpec.ImpactSmokeCount, 1f, 0.92f); if (a < 0f || a > 1f) continue;
                        int sd = f.Seed + 700 + k * 11; Vector3 o = RandSphere(sd) * MathF.Cbrt(Hash(sd + 3));
                        Vector4 c = a < 0.19571f ? Vector4.Lerp(new Vector4(1, 0, 0, 0), new Vector4(1, 0, 0, 1), a / 0.19571f) : a < 0.576407f ? Vector4.Lerp(new Vector4(1, 0, 0, 1), new Vector4(.09375f, .09375f, .09375f, 1), (a - 0.19571f) / 0.380697f) : Vector4.Lerp(new Vector4(.09375f, .09375f, .09375f, 1), new Vector4(.1055f, .1055f, .1055f, 0), (a - 0.576407f) / 0.423593f);
                        ColQuad(_smoke, f.Pos + o + new Vector3(0, a * a, 0), right, up, FxSpec.ImpactSmokeScale(a), new Color(c.X, c.Y, c.Z, c.W * 0.6f));
                    }
                    continue;
                }
                if (f.Kind == Kind.Muzzle)
                {
                    if (age > 0.3f) { _fx.RemoveAt(i); continue; }
                    if (age < 0.1f) { float s = age / 0.1f, sc = s < 0.79f ? s / 0.79f * 0.99f : 0.99f * (1f - s) / 0.21f;   // player.tscn Curve (0,0)->(0.79,0.99)->(1,0)
                        Quad(_blue, f.Pos, right, up, 0.36f * sc, 0.9f); Quad(_white, f.Pos, right, up, 0.12f * sc, 0.9f); }
                    // ShootParticle (player.tscn:551-562): 10 bullet_material spheres r 0.003, 0.3 s, explosiveness .79, NO initial velocity -> they only sag under gravity (0,-3,0) from the muzzle
                    for (int k = 0; k < FxSpec.ShootParticleCount; k++)
                    {
                        float a = age - FxSpec.SpawnDelay(k, FxSpec.ShootParticleCount, FxSpec.ShootParticleLife, 0.79f); if (a < 0f || a > FxSpec.ShootParticleLife) continue;
                        Quad(_blue, f.Pos + new Vector3(0, -1.5f * a * a, 0), right, up, 0.006f * 3.5f * 2f, 1f);
                    }
                    continue;
                }
                if (age > 1.2f) { _fx.RemoveAt(i); continue; }
                // Blast flash (BlastParticle 0.6 s / LightBlast 0.15 s): expanding glow + white core
                if (age < 0.3f) { float t = age / 0.3f; Quad(_blue, f.Pos, right, up, 0.5f + age * 4.5f, MathF.Pow(1f - t, 1.2f)); }
                if (age < 0.15f) Quad(_white, f.Pos, right, up, 0.6f * (1f - age / 0.15f), 1f - age / 0.15f);
                Vector3 n = f.Dir; Vector3 t1 = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) > 0.9f ? Vector3.Right : Vector3.Up)), t2 = Vector3.Cross(n, t1);
                // Smoke (bullet.tscn Smoke: 250 blue_myst quads, 0.6 s, sphere r 0.1, vel 0..2 + radial 1, scale 0.01..0.2 x linear curve, alpha 0.56*0.5 x (1-t))
                if (age < FxSpec.HitSmokeLife)
                {
                    float t = age / FxSpec.HitSmokeLife; var col = new Color(0.094f, 0.47f, 0.75f, 0.28f * (1f - t));
                    for (int k = 0; k < FxSpec.HitSmokeCount; k++)
                    {
                        int sd = f.Seed + k * 7; Vector3 o = RandSphere(sd) * (FxSpec.HitSmokeSphere * MathF.Cbrt(Hash(sd + 3))), v = RandSphere(sd + 5) * (Hash(sd + 9) * FxSpec.HitSmokeVelMax) + (o.LengthSquared() > 1e-8f ? Vector3.Normalize(o) : Vector3.Up) * FxSpec.HitSmokeRadial;
                        ColQuad(_smoke, f.Pos + o + v * age, right, up, MathHelper.Lerp(FxSpec.HitSmokeScaleMin, FxSpec.HitSmokeScaleMax, Hash(sd + 11)) * FxSpec.HitSmokeScale(t), col);
                    }
                }
                // BlastSparks (10 spheres r 0.03, 0.3 s, emission 7.34; the process material has NO initial velocity, so they stay at the hit point): one bright dot, ramp white -> cyan
                if (age < 0.3f) { float t = age / 0.3f, sc = t < 0.848f ? MathHelper.Lerp(1f, 0.782f, t / 0.848f) : MathHelper.Lerp(0.782f, 0.009f, (t - 0.848f) / 0.152f); Quad(t < 0.237f ? _white : _cyan, f.Pos, right, up, 0.03f * 3.5f * sc, 1f); }
                // LightParticle (40 emissive balls r 0.03, 1.5 s, explosiveness .84, sphere 0.3, vel 0..3, gravity -9.81, scale .5..1 x curve; floor bounce: FxSpec.BallPos against the ground height under the hit)
                for (int k = 0; k < FxSpec.HitBallCount; k++)
                {
                    float a = age - FxSpec.SpawnDelay(k, FxSpec.HitBallCount, FxSpec.HitBallLife, FxSpec.HitBallExplosiveness); if (a < 0f || a > FxSpec.HitBallLife) continue;
                    int sd = f.Seed + 300 + k * 17; Vector3 p0 = RandSphere(sd) * (FxSpec.HitBallSphere * MathF.Cbrt(Hash(sd + 3))), v = RandSphere(sd + 5) * (Hash(sd + 9) * FxSpec.HitBallVelMax);
                    Vector3 p = FxSpec.BallPos(f.Pos + p0, v, FxSpec.HitBallGravity, a, f.FloorY + 0.03f, out _);   // floor bounce (bullet.tscn:416-428)
                    Quad(_cyan, p, right, up, 0.03f * MathHelper.Lerp(0.5f, 1f, Hash(sd + 13)) * FxSpec.HitBallScale(a / FxSpec.HitBallLife) * 3.5f, 1f);
                }
            }
            gd.BlendState = Add; gd.DepthStencilState = DepthStencilState.DepthRead; gd.RasterizerState = RasterizerState.CullNone;
            gd.SamplerStates[0] = SamplerState.LinearClamp;
            _glow.Parameters["ViewProj"].SetValue(view * proj); _glow.Parameters["Tex"].SetValue(_tex);
            Flush(gd, _ember, new Vector3(1f, 0.55f, 0.55f) * SceneLighting.Tune("TPS_FXGAIN", 3f) * 1.5f * hdrScale);
            Flush(gd, _red, RedTint * hdrScale); Flush(gd, _fire, FireTint * hdrScale); Flush(gd, _blue, BlueTint * hdrScale); Flush(gd, _cyan, CyanTint * hdrScale); Flush(gd, _white, WhiteTint * hdrScale);
            if (_tube.Count > 0)   // laser body: LaserShader.gdshader, emission red * energy (additive, depth-tested)
            {
                _fxs.CurrentTechnique = _fxs.Techniques["Beam"]; _fxs.Parameters["ViewProj"].SetValue(view * proj); _fxs.Parameters["Noise"].SetValue(_noise);
                _fxs.Parameters["UvScale"].SetValue(FxSpec.LaserUvScale); _fxs.Parameters["OffsetU"].SetValue(beamOf); _fxs.Parameters["Cutoff"].SetValue(beamCu); _fxs.Parameters["Energy"].SetValue(beamEn);
                _fxs.Parameters["Tint"].SetValue(new Vector3(1f, 0f, 0f) * BeamGain * hdrScale); gd.SamplerStates[0] = SamplerState.LinearWrap; gd.SamplerStates[1] = SamplerState.LinearWrap;
                _fxs.CurrentTechnique.Passes[0].Apply(); gd.DrawUserPrimitives(PrimitiveType.TriangleList, _tube.ToArray(), 0, _tube.Count / 3);
            }
            gd.BlendState = BlendState.NonPremultiplied; gd.SamplerStates[0] = SamplerState.LinearClamp;
            if (_smoke.Count > 0)   // bullet puff smoke
            {
                _fxs.CurrentTechnique = _fxs.Techniques["Smoke"]; _fxs.Parameters["ViewProj"].SetValue(view * proj); _fxs.Parameters["Tex"].SetValue(_myst); _fxs.Parameters["Tint"].SetValue(Vector3.One * SmokeGain * hdrScale);
                _fxs.CurrentTechnique.Passes[0].Apply(); gd.DrawUserPrimitives(PrimitiveType.TriangleList, _smoke.ToArray(), 0, _smoke.Count / 3);
            }
            foreach (var (v, life) in _barrel)   // red robot barrel smoke (guard: an empty quad made DrawUserPrimitives throw ArgumentOutOfRange, a crash in play) (smoke.gdshader), one draw per particle (Life uniform)
            {
                if (v == null || v.Length < 3) continue;
                _fxs.CurrentTechnique = _fxs.Techniques["Barrel"]; _fxs.Parameters["ViewProj"].SetValue(view * proj); _fxs.Parameters["Tex"].SetValue(_barrelTex); _fxs.Parameters["Noise"].SetValue(_noise);
                _fxs.Parameters["Life"].SetValue(life); _fxs.Parameters["Tint"].SetValue(Vector3.One * SmokeGain * hdrScale); gd.SamplerStates[1] = SamplerState.LinearWrap;
                _fxs.CurrentTechnique.Passes[0].Apply(); gd.DrawUserPrimitives(PrimitiveType.TriangleList, v, 0, v.Length / 3);
            }
        }

        static Vector3 HemiDir(int seed, Vector3 n, Vector3 t1, Vector3 t2, float spread)
        {
            float a = Hash(seed) * MathHelper.TwoPi, e = Hash(seed + 1) * spread; // angle from the normal up to ~spread rad
            return n * MathF.Cos(e) + (t1 * MathF.Cos(a) + t2 * MathF.Sin(a)) * MathF.Sin(e);
        }

        void Flush(GraphicsDevice gd, List<VertexPositionColorTexture> l, Vector3 tint)
        {
            if (l.Count == 0) return;
            _glow.Parameters["Tint"].SetValue(tint);
            _glow.CurrentTechnique.Passes[0].Apply();
            gd.DrawUserPrimitives(PrimitiveType.TriangleList, l.ToArray(), 0, l.Count / 3);
        }
    }
}