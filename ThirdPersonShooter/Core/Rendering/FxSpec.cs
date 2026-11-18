using System;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Godot effect constants and keyframe tracks (headless-testable). Sources:
    /// red_robot.tscn "shoot" Animation (tracks 6-9 RayMesh shader params, 14 Smoke emitting, 0 LaserEmber emitting) and ShaderMaterial_p5m1n (uv_scale (15,0.5), clip 8, color1 (1,0,0));
    /// bullet.tscn OmniLight3D (colour (0.125,0.627,1), energy 1.5, range 1.5, attenuation 2) and the "explode" animation tracks 0 and 12.
    /// </summary>
    public static class FxSpec
    {
        // ---- red robot laser (RayMesh) -------------------------------------------------------------
        static readonly float[] TravT = { 0, 1, 1.65f, 1.7f, 2.1f, 2.2f, 2.3f, 2.4f, 2.6f }, TravV = { 0, 0, 0, 0.3f, 0.3f, 1.5f, 1.5f, 0.3f, 0 };            // track 6 traversal_scale
        static readonly float[] EneT = { 0, 1, 1.9f, 2.1f, 2.2f, 2.5f, 2.6f }, EneV = { 1, 1, 1, 1, 5, 5, 1 };                                               // track 7 energy
        static readonly float[] CutT = { 0, 1.65f, 1.7f, 2.1f, 2.2f, 2.3f, 2.4f, 2.6f }, CutV = { 1, 1, 0.55f, 0.55f, 0, 0, 0.55f, 1 };                       // track 8 cutoff_value
        static readonly float[] OffT = { 0, 1.05f, 1.65f, 2.1f, 2.4f, 2.6f }, OffV = { 0, 0, 0, 0.3f, 0.3f, 0 };                                              // track 9 offset_uv_x
        public static readonly Vector2 LaserUvScale = new Vector2(15f, 0.5f);   // ShaderMaterial_p5m1n uv_scale
        public const float LaserMeshLength = 8f;      // material clip default; the real raymesh length is in a binary .mesh (assumed 8 m: UV.x 0..1 over the mesh)
        public const float LaserMeshRadius = 0.2f;    // ASSUMED (raymesh.mesh is compressed binary); scaled by traversal_scale 0.3 .. 1.5
        public const float LaserStart = 1.25f;        // RayMesh node origin (0,1.25,0) along the bone's +Y = beam direction
        public const float EmberStart = 2.25f, EmberLife = 2f, EmberRandom = 0.85f; public const int EmberCount = 200;   // LaserEmber: track 0 true at 2.25 s, lifetime 2, lifetime_randomness .85, amount 200
        public const float SmokeStart = 1.55f, SmokeLife = 1.5f, SmokeExplosiveness = 0.5f; public const int SmokeCount = 3;   // Smoke: track 14 true at 1.55 s, lifetime 1.5, explosiveness 0.5, amount 3

        static float Track(float[] ts, float[] vs, float t)
        {
            if (t <= ts[0]) return vs[0];
            for (int i = 1; i < ts.Length; i++) if (t <= ts[i]) return MathHelper.Lerp(vs[i - 1], vs[i], (t - ts[i - 1]) / (ts[i] - ts[i - 1]));
            return vs[^1];
        }
        public static void BeamParams(float clock, out float traversal, out float energy, out float cutoff, out float offsetU)
        { traversal = Track(TravT, TravV, clock); energy = Track(EneT, EneV, clock); cutoff = Track(CutT, CutV, clock); offsetU = Track(OffT, OffV, clock); }

        /// <summary>Particle spawn offset of particle i of n for a one-shot emitter (Godot: spawns spread over lifetime*(1-explosiveness)).</summary>
        public static float SpawnDelay(int i, int n, float lifetime, float explosiveness) => lifetime * (1f - explosiveness) * i / n;

        // ---- laser impact (enemies/red_robot/laser/impact_effect/impact_effect.tscn, animation "blast" :9-60 and node sub-resources) ----
        public const float ImpactRaysLife = 0.2f, ImpactRaysScale0 = 0.536364f;                 // LightRays: emitting 0..0.2, lifetime 0.2, scale curve 0.536 -> 1
        public const float ImpactCenterLife = 0.2f;                                              // BlastCenter: emitting 0..0.3, lifetime 0.2, scale curve 0 (slope 2.4) -> 1
        public static float ImpactCenterScale(float t) => MathHelper.Clamp(t, 0f, 1f);
        public const float ImpactBlastStart = 0.2f, ImpactBlastLife = 0.3f;                      // Blast: emitting 0.2..0.5, lifetime 0.3, scale 1.2 -> 5
        public const float ImpactEmberStart = 0.2f; public const int ImpactEmberCount = 50;      // ExplosionEmbers: 0.2..0.5, 50, default lifetime 1, explosiveness 0.9, gravity (0,-2,0)
        public const float ImpactSmokeStart = 0.3f; public const int ImpactSmokeCount = 20;      // SmokeParticle: 0.3..0.6, 20, lifetime 1, explosiveness 0.92, sphere r 1, gravity (0,2,0)
        public static float ImpactSmokeScale(float t) => 1f - MathF.Pow(1f - MathHelper.Clamp(t, 0f, 1f), 2.5f);   // Curve_omnbd 0 (slope 3.25) -> 1; quad 2x2 so radius = scale

        public const int ShootParticleCount = 10; public const float ShootParticleLife = 0.3f;     // player.tscn ShootParticle
        public const int BuildupCount = 64; public const float BuildupLife = 0.4f, BuildupStart = 0.2f, BuildupEnd = 1.0f;   // red_robot.tscn BuildupParticles + animation track 12

        // ---- LightParticle floor bounce (bullet.tscn:416-428 ParticleProcessMaterial_vah46: collision_mode 1 rigid, friction 0.03, bounce 0.6) -------------
        public const float BallBounce = 0.6f, BallFriction = 0.03f;
        /// <summary>Analytic ballistic ball with gravity <paramref name="g"/> (negative) and rigid bounces on a flat floor at <paramref name="floorY"/> (NegativeInfinity = no floor). Bounce keeps 0.6 of the normal speed, friction removes 3 % of the horizontal speed per bounce; it rests when the bounce speed drops below 0.25 m/s.</summary>
        public static Vector3 BallPos(Vector3 p0, Vector3 v, float g, float t, float floorY, out int bounces)
        {
            bounces = 0; float h = p0.Y - floorY; Vector3 p = p0;
            if (float.IsNegativeInfinity(floorY)) return p0 + v * t + new Vector3(0, 0.5f * g * t * t, 0);
            if (h < 0f) h = 0f; p.Y = floorY + h;
            for (int i = 0; i < 12; i++)
            {
                // time to reach h = 0: h + vy*dt + g/2 dt^2 = 0 (g < 0), positive root
                float disc = v.Y * v.Y - 2f * g * h; float dt = (-v.Y - MathF.Sqrt(Math.Max(0f, disc))) / g;
                if (dt < 0f) dt = 0f;
                if (t < dt) { return new Vector3(p.X + v.X * t, floorY + h + v.Y * t + 0.5f * g * t * t, p.Z + v.Z * t); }
                p = new Vector3(p.X + v.X * dt, floorY, p.Z + v.Z * dt); t -= dt; bounces++;
                float vn = -(v.Y + g * dt) * BallBounce; float k = 1f - BallFriction; v = new Vector3(v.X * k, vn, v.Z * k); h = 0f;
                if (vn < 0.25f) return new Vector3(p.X + v.X * t * 0f, floorY, p.Z);   // at rest on the floor
            }
            return new Vector3(p.X, floorY, p.Z);
        }

        // ---- bullet omni light ---------------------------------------------------------------------
        public static readonly Vector3 BulletLightColor = new Vector3(0.12549f, 0.627451f, 1f);   // OmniLight3D light_color
        public const float BulletLightEnergy = 1.5f, BulletLightRange = 1.5f;
        /// <summary>Light energy / range after the bullet explodes (animation "explode": energy 1.5 -> 3.0 @0.25 -> 0 @0.7; range 3.0 -> 0 @0.85). age < 0 = flying.</summary>
        public static void BulletLight(float explodedAge, out float energy, out float range)
        {
            if (explodedAge < 0f) { energy = BulletLightEnergy; range = BulletLightRange; return; }
            energy = explodedAge < 0.25f ? MathHelper.Lerp(1.5f, 3f, explodedAge / 0.25f) : explodedAge < 0.7f ? MathHelper.Lerp(3f, 0f, (explodedAge - 0.25f) / 0.45f) : 0f;
            range = explodedAge < 0.85f ? MathHelper.Lerp(3f, 0f, explodedAge / 0.85f) : 0f;
        }

        // ---- bullet hit smoke (bullet.tscn Smoke GPUParticles3D) -----------------------------------
        public const int HitSmokeCount = 250; public const float HitSmokeLife = 0.6f, HitSmokeScaleMin = 0.01f, HitSmokeScaleMax = 0.2f, HitSmokeSphere = 0.1f, HitSmokeVelMax = 2f, HitSmokeRadial = 1f;
        public const int HitBallCount = 40; public const float HitBallLife = 1.5f, HitBallExplosiveness = 0.84f, HitBallSphere = 0.3f, HitBallVelMax = 3f, HitBallGravity = -9.81f;
        /// <summary>LightParticle scale curve (0,0.022) -> (0.1,1) -> (1,0.022).</summary>
        public static float HitBallScale(float t) => t < 0.1f ? MathHelper.Lerp(0.021978f, 1f, t / 0.1f) : MathHelper.Lerp(1f, 0.021978f, (t - 0.1f) / 0.9f);
        /// <summary>Smoke scale curve: linear 0 -> 1.</summary>
        public static float HitSmokeScale(float t) => MathHelper.Clamp(t, 0f, 1f);

        // ---- forklift exhaust + spot light (level/forklift/flying_forklift.tscn) -----------------------
        // 4 CPUParticles3D: amount 20, lifetime 0.5, sphere r 0.1, direction (0,0,1) spread 10, gravity (0,6,0), speed 3, local_coords default (false = WORLD space),
        // QuadMesh 1x1 billboard (billboard_mode 3), additive unshaded, albedo alpha 0.176 x dot.png x vertex colour (ramp 4), scale curve Curve_bs8xw.
        public const int ForkCount = 20; public const float ForkLife = 0.5f, ForkRadius = 0.1f, ForkSpreadDeg = 10f, ForkSpeed = 3f, ForkGravity = 6f, ForkAlpha = 0.176471f;
        /// <summary>Emitter transforms in the forklift's local space (flying_forklift.tscn:50-113; rotation 180 deg about Y; row-vector Matrix, Godot Basis rows are the Matrix columns).</summary>
        public static readonly Matrix[] ForkEmitters =
        {
            Matrix.CreateRotationY(MathF.PI) * Matrix.CreateTranslation(0.471727f, -0.277269f, -2.89514f),
            Matrix.CreateRotationY(MathF.PI) * Matrix.CreateTranslation(-0.501376f, -0.277269f, -2.89514f),
            Matrix.CreateRotationY(MathF.PI) * Matrix.CreateTranslation(1.42691f, -0.414244f, -2.48934f),
            Matrix.CreateRotationY(MathF.PI) * Matrix.CreateTranslation(-1.33273f, -0.414244f, -2.48934f),
        };
        /// <summary>Curve_bs8xw: (0,0.3) (0.2,0.8) (1,1), all tangents 0 (cubic Hermite per segment = smoothstep).</summary>
        public static float ForkScale(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f); float a, b, u;
            if (t < 0.2f) { a = 0.3f; b = 0.8f; u = t / 0.2f; } else { a = 0.8f; b = 1f; u = (t - 0.2f) / 0.8f; }
            return MathHelper.Lerp(a, b, u * u * (3f - 2f * u));
        }
        /// <summary>Gradient "4" (offsets 0 / 0.1 / 0.3 / 1), linear: rgb in xyz, alpha in w.</summary>
        public static Vector4 ForkColor(float t)
        {
            Vector4 c0 = new Vector4(0.0431373f, 0.458824f, 1f, 1f), c1 = new Vector4(0.42f, 0.874333f, 1f, 0.858824f), c2 = new Vector4(0.945098f, 0.627451f, 1f, 0.411765f), c3 = new Vector4(0.12549f, 0.25098f, 1f, 0f);
            t = MathHelper.Clamp(t, 0f, 1f);
            return t < 0.1f ? Vector4.Lerp(c0, c1, t / 0.1f) : t < 0.3f ? Vector4.Lerp(c1, c2, (t - 0.1f) / 0.2f) : Vector4.Lerp(c2, c3, (t - 0.3f) / 0.7f);
        }
        /// <summary>SpotLight3D (flying_forklift.tscn:~114): colour (0.439,0.753,1), energy 12, range 50, spot_angle 50 (half-angle), local transform pointing down and forward.</summary>
        public static readonly Vector3 ForkLightColor = new Vector3(0.439216f, 0.752941f, 1f);
        public const float ForkLightEnergy = 12f, ForkLightRange = 50f, ForkLightAngleDeg = 50f;
        public static readonly Vector3 ForkLightPos = new Vector3(0f, 0.181815f, 4.10661f);
        /// <summary>Light axis in forklift space: the node's local -Z through Basis rows (1,0,0) (0,-0.819152,0.573576) (0,-0.573576,-0.819152) = (0,-0.573576,0.819152).</summary>
        public static readonly Vector3 ForkLightDir = new Vector3(0f, -0.573576f, 0.819152f);

        /// <summary>Stateless world-space exhaust particle k (0..19) of emitter e at time T: born at T - ((T - k*life/20) mod life) at the emitter's world matrix of THAT time
        /// (<paramref name="forkliftWorldAt"/>), then ballistic (world-space particles do not follow the forklift). Returns false when not yet born.</summary>
        public static bool ForkParticle(int e, int k, float T, Func<float, Matrix> forkliftWorldAt, out Vector3 pos, out float life01, out float spawnT)
        {
            pos = default; life01 = 0f; spawnT = 0f;
            float ph = T - k * ForkLife / ForkCount; if (ph < 0f) return false;
            int cyc = (int)MathF.Floor(ph / ForkLife); float a = ph - cyc * ForkLife; spawnT = T - a; life01 = a / ForkLife;
            int sd = e * 100003 + k * 7919 + cyc * 104729;
            Matrix m = ForkEmitters[e] * forkliftWorldAt(spawnT);
            float az = H(sd) * MathHelper.TwoPi, el = H(sd + 1) * MathHelper.ToRadians(ForkSpreadDeg);
            Vector3 dl = new Vector3(MathF.Sin(el) * MathF.Cos(az), MathF.Sin(el) * MathF.Sin(az), MathF.Cos(el));
            float z = H(sd + 2) * 2f - 1f, ang = H(sd + 3) * MathHelper.TwoPi, rr = MathF.Sqrt(1f - z * z);
            Vector3 off = new Vector3(rr * MathF.Cos(ang), z, rr * MathF.Sin(ang)) * (ForkRadius * MathF.Cbrt(H(sd + 4)));
            Vector3 v = Vector3.TransformNormal(dl, m) * ForkSpeed;
            pos = Vector3.Transform(off, m) + v * a + new Vector3(0f, 0.5f * ForkGravity * a * a, 0f);
            return true;
        }
        static float H(int n) { uint h = (uint)n * 747796405u + 2891336453u; h = ((h >> (int)((h >> 28) + 4)) ^ h) * 277803737u; h = (h >> 22) ^ h; return (h & 0xFFFFFF) / (float)0xFFFFFF; }

        // ---- LaserCenter (red_robot.tscn:10725-10735): 1 sphere (SphereMesh_2uov0 r 0.1), lifetime 1.5, one_shot, gravity 0, scale curve Curve_8fal6; emitting true 0.2 s / false 0.4 s (track 13)
        public const float LaserCenterLife = 1.5f, LaserCenterOn = 0.2f, LaserCenterOff = 0.4f, LaserCenterRadius = 0.1f, LaserCenterY = 1.5f;
        /// <summary>Curve_8fal6: (0.00263,0) (0.85,0.9909; tangents -0.0751) (1,0; left tangent -18.75); cubic Hermite = Godot's bezier with tangent handles.</summary>
        public static float LaserCenterScale(float t)
        {
            const float x0 = 0.00263158f, x1 = 0.85f, x2 = 1f, y1 = 0.990909f, m1 = -0.0750987f, m2 = -18.7533f;
            if (t <= x0 || t >= x2) return 0f;
            float h0 = t < x1 ? x1 - x0 : x2 - x1, u = (t - (t < x1 ? x0 : x1)) / h0, u2 = u * u, u3 = u2 * u;
            float pa = t < x1 ? 0f : y1, pb = t < x1 ? y1 : 0f, ma = t < x1 ? 0f : m1, mb = t < x1 ? m1 : m2;
            return (2 * u3 - 3 * u2 + 1) * pa + (u3 - 2 * u2 + u) * h0 * ma + (-2 * u3 + 3 * u2) * pb + (u3 - u2) * h0 * mb;
        }
        /// <summary>Age of the single LaserCenter particle at shoot-clip time <paramref name="clock"/> (born when emitting turns on at 0.2 s; -1 = not alive).</summary>
        public static float LaserCenterAge(float clock) { float a = clock - LaserCenterOn; return a >= 0f && a <= LaserCenterLife ? a : -1f; }
    }
}
