using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// One orthographic shadow map for the fake key light (Godot has no directional light; its shadows come from level SpotLight3Ds, see LIGHTING-NOTES "Shadows").
    /// The map follows the player (texel-snapped), holds linear light-space depth in a Single colour target (DesktopGL cannot sample a depth renderbuffer) and is
    /// sampled by the PbrLit/PbrLitSkinned "Shadow" techniques (3x3 PCF, normal-offset bias).
    /// </summary>
    public sealed class ShadowMap
    {
        public readonly int Size;
        public readonly RenderTarget2D Target;
        public readonly Effect Depth;
        public Matrix View, Proj, ViewProj;
        public float Radius, HalfDepth, Texel;
        public Vector3 LightDir;
        public float AmbStrength = SceneLighting.Tune("TPS_SHADOWAMB", 0.6f);
        public float BiasMeters = SceneLighting.Tune("TPS_SHBIAS", 0.06f), NormalOffsetTexels = SceneLighting.Tune("TPS_SHNOFF", 1f);
        public int Casters;
        public BoundingFrustum Frustum;   // spot tiles: exact frustum cull
        public bool Perspective; public float Far = 1f, Soft = 1f; public Vector3 Origin;

        public ShadowMap(GraphicsDevice gd, Effect depth, int size = 2048, float radius = 32f, float halfDepth = 80f)
        {
            Depth = depth; Size = size; Radius = radius; HalfDepth = halfDepth;
            foreach (var f in new[] { SurfaceFormat.Single, SurfaceFormat.HalfVector4 })
            {
                try { var rt = new RenderTarget2D(gd, size, size, false, f, DepthFormat.Depth24); gd.SetRenderTarget(rt); gd.Clear(Color.White); gd.SetRenderTarget(null); Target = rt; Console.WriteLine($"[shadow] map {size}x{size} {f}, radius {radius} m"); break; }
                catch (Exception e) { Console.WriteLine($"[shadow] {f} failed: {e.GetType().Name} {e.Message}"); }
            }
            Enabled = Target != null;
        }
        public bool Enabled;

        /// <summary>Light direction from the key's horizontal heading and an elevation in degrees (the key itself is nearly horizontal, which would give metre-long grazing shadows).</summary>
        public static Vector3 DirFromKey(Vector3 keyDir, float elevDeg)
        {
            var h = new Vector2(keyDir.X, keyDir.Z); h = h.LengthSquared() < 1e-6f ? new Vector2(0, 1) : Vector2.Normalize(h);
            float e = MathHelper.ToRadians(elevDeg);
            return new Vector3(h.X * MathF.Cos(e), -MathF.Sin(e), h.Y * MathF.Cos(e));
        }

        /// <summary>Orthographic light frustum (half-width <paramref name="radius"/>) around <paramref name="center"/>; the view translation is snapped to whole texels so the map does not shimmer when the centre moves.</summary>
        public static void Fit(Vector3 lightDir, Vector3 center, float radius, float halfDepth, int size, out Matrix view, out Matrix proj, out float texel)
        {
            Vector3 f = Vector3.Normalize(lightDir);
            Vector3 up = MathF.Abs(f.Y) > 0.95f ? Vector3.UnitZ : Vector3.UnitY;
            Vector3 pos = center - f * halfDepth;
            view = Matrix.CreateLookAt(pos, pos + f, up);
            texel = 2f * radius / size;
            view.M41 = MathF.Round(view.M41 / texel) * texel;
            view.M42 = MathF.Round(view.M42 / texel) * texel;
            proj = Matrix.CreateOrthographicOffCenter(-radius, radius, -radius, radius, 0f, 2f * halfDepth);
        }

        /// <summary>Sphere vs the light box (x/y within the radius, depth within the near/far planes).</summary>
        public bool Contains(BoundingSphere s)
        {
            if (Frustum != null) return Frustum.Intersects(s);
            var c = Vector3.Transform(s.Center, View); float d = -c.Z, r = s.Radius;
            return MathF.Abs(c.X) <= Radius + r && MathF.Abs(c.Y) <= Radius + r && d + r >= 0f && d - r <= 2f * HalfDepth;
        }

        public void SetupFrame(Vector3 keyDir, Vector3 center)
        {
            LightDir = DirFromKey(keyDir, SceneLighting.Tune("TPS_SHELEV", 40f));
            Fit(LightDir, center, Radius, HalfDepth, Size, out View, out Proj, out Texel);
            ViewProj = View * Proj; Casters = 0; Perspective = false; Soft = 1f; Origin = Vector3.Zero; AmbOverride = null;
        }
        float? AmbOverride;

        /// <summary>Perspective spot-light map (menu robot): 90+ degree frustum from <paramref name="pos"/> along <paramref name="dir"/>.</summary>
        public void SetupSpot(Vector3 pos, Vector3 dir, float fovDeg, float near, float far, float soft)
        {
            LightDir = Vector3.Normalize(dir); Origin = pos; Perspective = true; Far = far; Soft = soft; AmbOverride = 0f; Casters = 0;
            View = Matrix.CreateLookAt(pos, pos + LightDir, MathF.Abs(LightDir.Y) > 0.95f ? Vector3.UnitZ : Vector3.UnitY);
            Proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fovDeg), 1f, near, far); ViewProj = View * Proj;
            Texel = 2f * 2.3f * MathF.Tan(MathHelper.ToRadians(fovDeg) * 0.5f) / Size;   // world size of a texel at about the robot's distance
        }

        /// <summary>Bind the target and clear it to "no occluder" (1.0); the caller then draws casters and calls <see cref="End"/>.</summary>
        public void Begin(GraphicsDevice gd, bool skinned)
        {
            gd.SetRenderTarget(Target);
            gd.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
            gd.BlendState = BlendState.Opaque; gd.DepthStencilState = DepthStencilState.Default; gd.RasterizerState = RasterizerState.CullNone;
            Depth.Parameters["LightVP"].SetValue(ViewProj); Depth.Parameters["DepthK"]?.SetValue(new Vector2(Perspective ? 1f : 0f, 1f / Far));
            Depth.CurrentTechnique = Depth.Techniques[skinned ? "DepthSkinned" : "Depth"];
        }
        /// <summary>Atlas tile: after <see cref="Begin"/> (which clears the whole target), select a viewport rectangle and the light matrices of one spot light.</summary>
        public void BeginTile(GraphicsDevice gd, int tile, int tileSize, Matrix view, Matrix proj, float far, BoundingFrustum frustum)
        {
            View = view; Proj = proj; ViewProj = view * proj; Perspective = true; Far = far; Frustum = frustum;
            gd.Viewport = new Viewport((tile % 2) * tileSize, (tile / 2) * tileSize, tileSize, tileSize);
            Depth.Parameters["LightVP"].SetValue(ViewProj); Depth.Parameters["DepthK"]?.SetValue(new Vector2(1f, 1f / far));
        }
        public void SetTechnique(bool skinned) => Depth.CurrentTechnique = Depth.Techniques[skinned ? "DepthSkinned" : "Depth"];
        public void End(GraphicsDevice gd) { gd.SetRenderTarget(null); }

        /// <summary>Receiver parameters for a PbrLit/PbrLitSkinned effect (use the "...Shadow" technique).</summary>
        public void Apply(Effect fx, int slot = 4)
        {
            fx.Parameters["ShadowTex"]?.SetValue(Target);
            fx.Parameters["ShadowVP"]?.SetValue(ViewProj);
            fx.Parameters["ShadowP"]?.SetValue(new Vector4(1f / Size, BiasMeters / (Perspective ? Far : 2f * HalfDepth), Texel * NormalOffsetTexels, 6f));
            fx.Parameters["ShadowDir"]?.SetValue(LightDir);
            fx.Parameters["ShadowAmb"]?.SetValue(AmbOverride ?? AmbStrength);
            fx.Parameters["ShadowK"]?.SetValue(new Vector4(Perspective ? 1f : 0f, 1f / Far, 0f, Soft)); fx.Parameters["ShadowOrigin"]?.SetValue(Origin);
            fx.GraphicsDevice.SamplerStates[slot] = SamplerState.PointClamp;
        }
    }
}
