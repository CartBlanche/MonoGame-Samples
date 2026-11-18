using System.Linq;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace ThirdPersonShooter
{
    /// <summary>
    /// 3D background of the menu screens (tps-demo-master/menu/menu.tscn): the skinned player posed at the origin, a dark floor lit by a spot light
    /// (SpotLight3D at (0.557, 2.312, 0.557) + the orange "bounce" spot below the floor), bluish ambient (0.165, 0.184, 0.271), black sky/fog,
    /// camera at (0.070, 1.224, 2.097) with the scene's basis, FOV 75 (Godot default, vertical). Simplified: the floor lighting (including a baked
    /// soft shadow) is rendered once into an HDR lightmap texture, no shadow maps, no volumetric fog, no panorama sky. Drawn through the normal HDR/tonemap/bloom path.
    /// </summary>
    public sealed class MenuBackdrop
    {
        // menu.tscn Camera3D transform basis rows -> look direction = -(third column), up = second column
        static readonly Vector3 CamPos = new(0.0702285f, 1.22404f, 2.09675f);
        static readonly Vector3 CamFwd = new(-0.488828f, -0.0740721f, -0.86923f);
        static readonly Vector3 CamUp = new(-0.0363083f, 0.997253f, -0.064563f);
        // menu.tscn SpotLight3D basis: looks along -(third column)
        static readonly Vector3 SpotPos = new(0.557445f, 2.31204f, 0.556668f), SpotDir = new(-0.254675f, -0.855452f, -0.450936f);
        static readonly Vector3 BouncePos = new(-0.206581f, -0.720626f, 0.088479f), BounceDir = new(0.254675f, 0.855451f, 0.450936f);
        static readonly Vector3 AmbientColor = new(0.164706f, 0.184314f, 0.270588f);  // environment ambient_light_color

        public readonly SceneLighting Lights = new SceneLighting();
        public float SpotEnergy = SceneLighting.Tune("TPS_MSPOT", 3.5f), Exposure = SceneLighting.Tune("TPS_MEXP", 1.3f), FloorGain = SceneLighting.Tune("TPS_MFLOOR", 2.6f);
        readonly AnimationPlayer _anim; readonly SkinnedClip _rest, _eyes; readonly int[] _eyeBones; float _t; readonly Texture2D _lightmap; readonly BasicEffect _floor;
        const float Half = 9f; // lightmap covers [-Half, Half] on x and z
        public Matrix World = Matrix.Identity; // glb model faces +Z = toward the camera

        readonly ShadowMap _sm; readonly Effect _floorFx; Texture2D _lightmapNoShadow;
        public static readonly bool ShadowsEnvOff = Environment.GetEnvironmentVariable("TPS_SHADOWS") == "0";
        public float FloorShadowDark = SceneLighting.Tune("TPS_MSHDARK", 0.9f), SpotShadowSoft = SceneLighting.Tune("TPS_MSHSOFT", 4f);

        public MenuBackdrop(GraphicsDevice gd, PlayerRenderer player, Texture2D floorTex, Effect depthFx = null, Effect floorFx = null)
        {
            if (depthFx != null && floorFx != null) { _sm = new ShadowMap(gd, depthFx, 1024, 1f, 1f); _floorFx = floorFx; }
            Lights.ClearLights();
            Lights.Ambient = AmbientColor * 0.35f; Lights.AmbGround = AmbientColor * 0.12f;
            Lights.KeyColor = Vector3.Zero; Lights.FogDensity = 0f; Lights.FogStart = 1000f;
            Lights.EnvUp = AmbientColor * 0.4f; Lights.EnvHorizon = AmbientColor * 0.15f; Lights.EnvDown = AmbientColor * 0.05f;
            Lights.AddSpot(SpotPos, SpotDir, Vector3.One * SpotEnergy, MathHelper.ToRadians(14f), MathHelper.ToRadians(45f), shadowed: true); // SpotLight3D default angle 45, white
            Lights.AddSpot(BouncePos, BounceDir, new Vector3(1f, 0.654902f, 0.0509804f) * (0.3f * SpotEnergy), MathHelper.ToRadians(14f), MathHelper.ToRadians(45f)); // SpotLightBounce colour, energy 0.3

            // The level emission tuning (SceneLighting.EmisTune) assumes the game exposure (3.4) and desaturates; the menu exposure is lower, so the eyes/chest panel looked dim and pale.
            // Rescale so the emissive reaches the same brightness after tonemapping, and keep its blue (Godot menu: eyes and chest light bright blue).
            float k = SceneLighting.Tune("TPS_MEMIS", 3.4f / Exposure);
            Lights.EmisTune = new Vector3(Lights.EmisTune.X * k, SceneLighting.Tune("TPS_MEMISSAT", 1f), Lights.EmisTune.Z * k);
            _anim = new AnimationPlayer(player.Model);
            // menu.tscn AnimationTree: Blend2 "Idlecombatrest" (loop) + "eyes" filtered to the eyelid/eye bones, blend_amount 1.0
            _rest = player.Model.Clips["Idlecombatrest"]; _eyes = player.Model.Clips["eyes"];
            _eyeBones = new[] { "eyelid.d.L", "eyelid.d.R", "eyelid.u.L", "eyelid.u.R", "eyes" }.Select(player.Model.BoneIndex).ToArray();
            Pose(0f, 0f);
            _floor = new BasicEffect(gd) { TextureEnabled = true, LightingEnabled = false, VertexColorEnabled = false, FogEnabled = false };
            var tint = FloorTint(floorTex); _lightmap = BuildLightmap(gd, tint); if (_sm != null) _lightmapNoShadow = BuildLightmap(gd, tint, false);
        }

        /// <summary>menu.tscn floor material: albedo_color 0.4196 x floor.png. The 512 px texture is stretched over the 4096 m quad (one texel = 8 m), so near the origin it reads as a flat colour: the centre texels.</summary>
        static Vector3 FloorTint(Texture2D t)
        {
            Vector3 avg = new Vector3(0.5f);
            try
            {
                var px = new Color[t.Width * t.Height]; t.GetData(px); double r = 0, g = 0, b = 0; int n = 0;
                for (int y = t.Height / 2 - 8; y <= t.Height / 2 + 8; y++) for (int x = t.Width / 2 - 8; x <= t.Width / 2 + 8; x++) { var c = px[y * t.Width + x]; r += c.R; g += c.G; b += c.B; n++; }
                avg = new Vector3((float)(r / n / 255.0), (float)(g / n / 255.0), (float)(b / n / 255.0));
            }
            catch { }
            avg = new Vector3(MathF.Pow(avg.X, 2.2f), MathF.Pow(avg.Y, 2.2f), MathF.Pow(avg.Z, 2.2f)); // sRGB -> linear
            return avg * 0.419608f; // albedo_color
        }

        Texture2D BuildLightmap(GraphicsDevice gd, Vector3 albedo, bool fakeShadow = true)
        {
            const int N = 256; var px = new HalfVector4[N * N];
            float cosO = MathF.Cos(MathHelper.ToRadians(45f)), cosI = MathF.Cos(MathHelper.ToRadians(14f));
            var spotDir = Vector3.Normalize(SpotDir);
            // soft contact shadow, placed to the screen-left / toward the camera like godot-01-main-menu.png
            Vector2 sc = new Vector2(-0.8f, 0.35f), sAxis = Vector2.Normalize(new Vector2(-0.87f, 0.49f)), sPerp = new Vector2(-sAxis.Y, sAxis.X);
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = -Half + (i + 0.5f) / N * 2f * Half, z = -Half + (j + 0.5f) / N * 2f * Half;
                    var X = new Vector3(x, 0f, z); var toL = SpotPos - X; float d = Math.Max(toL.Length(), 0.5f); var L = toL / d;
                    float cone = MathHelper.Clamp((Vector3.Dot(-L, spotDir) - cosO) / (cosI - cosO), 0f, 1f); cone = cone * cone * (3f - 2f * cone);
                    float spot = SpotEnergy * cone * L.Y / (d * d) * MathF.Pow(Math.Max(0f, 1f - MathF.Pow(d / 12f, 4f)), 2f);
                    var q = new Vector2(x - sc.X, z - sc.Y); float a = Vector2.Dot(q, sAxis) / 1.7f, b = Vector2.Dot(q, sPerp) / 0.95f;
                    float shadow = MathHelper.Clamp(1.15f - MathF.Sqrt(a * a + b * b), 0f, 1f); shadow = fakeShadow ? shadow * shadow * (3f - 2f * shadow) : 0f;
                    float cdx = x - CamPos.X, cdz = z - CamPos.Z; float fade = MathHelper.Clamp(1f - (MathF.Sqrt(cdx * cdx + cdz * cdz) - 2.2f) / 4.2f, 0f, 1f); fade *= fade * (3f - 2f * fade); // black "fog" with distance
                    Vector3 c = albedo * (AmbientColor * 0.5f * (1f - 0.55f * shadow) + new Vector3(0.8f, 0.85f, 1f) * spot * (1f - 0.92f * shadow) / MathHelper.Pi) * fade * FloorGain;
                    px[j * N + i] = new HalfVector4(c.X, c.Y, c.Z, 1f);
                }
            var mid = px[(N / 2 + 8) * N + N / 2 + 8].ToVector4(); Console.WriteLine($"[menu] floor albedo={albedo} lightmap near-feet={mid}");
            var t = new Texture2D(gd, N, N, false, SurfaceFormat.HalfVector4); t.SetData(px); return t;
        }

        /// <summary>The menu robot loops Idlecombatrest with the eyes clip on the eyelid bones (menu.tscn); --shot runs never call Update, so screenshots stay at t=0.</summary>
        public void Update(float dt) { float p = _t; _t += dt; Pose(_t, p); }
        void Pose(float t, float prev)
        {
            _anim.BeginFrame();
            _anim.Add(_rest, t, prev, 1f);
            _anim.AddOverlay(new[] { _eyes }, new[] { t }, new[] { 1f }, 1f, _eyeBones);
            _anim.Finish();
        }

        /// <summary>Renders the 3D backdrop through the HDR pass into <paramref name="rt"/> (null = back buffer).</summary>
        public void Draw(GraphicsDevice gd, PostProcess post, PlayerRenderer player, int w, int h, RenderTarget2D rt)
        {
            Matrix view = Matrix.CreateLookAt(CamPos, CamPos + CamFwd, CamUp);
            Matrix proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(PlayerConstants.Fov), w / (float)h, 0.05f, 100f);
            float exp = post.Exposure, sat = post.Saturation, bp = post.BlackPoint, sc = post.SCurve; post.SCurve = SceneLighting.Tune("TPS_MSCURVE", 0.6f); post.Exposure = Exposure; post.Saturation = 1.15f; post.BlackPoint = 0f;
            // real spot shadow (Settings > SHADOWS): perspective depth map from the menu SpotLight3D (menu.tscn:126-128, shadow_enabled), robot as the only caster
            bool shOn = _sm != null && _sm.Enabled && GameSettings.Current.Shadows && !ShadowsEnvOff;
            player.Shadow = null;
            if (shOn)
            {
                var sw = player.World; var sa = player.Anim; player.Anim = _anim; player.World = World;
                _sm.SetupSpot(SpotPos, SpotDir, 100f, 0.3f, 14f, SpotShadowSoft); _sm.Begin(gd, true); player.DrawShadow(gd, _sm); _sm.End(gd);
                player.Anim = sa; player.World = sw; player.Shadow = _sm;
            }
            post.Begin(w, h);
            Lights.HdrScale = post.HdrScale;
            gd.Clear(Color.Black);
            gd.DepthStencilState = DepthStencilState.Default; gd.RasterizerState = RasterizerState.CullNone; gd.BlendState = BlendState.Opaque; gd.SamplerStates[0] = SamplerState.LinearClamp;
            _floor.View = view; _floor.Projection = proj; _floor.World = Matrix.Identity; _floor.Texture = _lightmap; _floor.DiffuseColor = new Vector3(post.HdrScale * SceneLighting.Tune("TPS_MFG", 1f));
            var v = new[]
            {
                new VertexPositionTexture(new Vector3(-Half, 0, -Half), new Vector2(0, 0)), new VertexPositionTexture(new Vector3(Half, 0, -Half), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-Half, 0, Half), new Vector2(0, 1)), new VertexPositionTexture(new Vector3(Half, 0, Half), new Vector2(1, 1)),
            };
            if (shOn)
            {
                _floorFx.Parameters["World"].SetValue(Matrix.Identity); _floorFx.Parameters["ViewProj"].SetValue(view * proj);
                _floorFx.Parameters["Lightmap"].SetValue(_lightmapNoShadow); _floorFx.Parameters["HdrScale"]?.SetValue(post.HdrScale * SceneLighting.Tune("TPS_MFG", 1f)); _floorFx.Parameters["ShadowDark"].SetValue(FloorShadowDark);
                _floorFx.Parameters["ShadowSlot"]?.SetValue(0f);
                gd.SamplerStates[0] = SamplerState.LinearClamp; _sm.Apply(_floorFx, 1);
                _floorFx.CurrentTechnique.Passes[0].Apply();
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, v, 0, 2);
            }
            else { _floor.CurrentTechnique.Passes[0].Apply(); gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, v, 0, 2); }
            var savedAnim = player.Anim; var savedWorld = player.World;
            player.Anim = _anim; player.World = World;
            player.Draw(gd, Lights, view, proj, CamPos);
            player.Anim = savedAnim; player.World = savedWorld; player.Shadow = null;
            post.Resolve(rt);
            post.Exposure = exp; post.Saturation = sat; post.BlackPoint = bp; post.SCurve = sc;
        }
    }
}
