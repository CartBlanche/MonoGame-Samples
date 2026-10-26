using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Shooter.Rendering.Particles;
using Shooter.Rendering.PostProcess;

namespace Shooter.Rendering
{
    /// <summary>Per-frame statistics, useful for the perf notes / debug overlay.</summary>
    public struct SceneRenderStats
    {
        public int DrawCalls;
        public int Triangles;
        public int ShadowDrawCalls;
        public int ItemsVisible;
        public int ItemsTotal;
        public int ParticlesDrawn;
    }

    /// <summary>
    /// MonoGame DesktopGL implementation of the URP forward pipeline used by the Unity FPS Microgame:
    /// shadow map -> depth/normals prepass -> SSAO -> HDR scene (skybox, opaque, transparent) -> weapon overlay camera -> post chain.
    /// </summary>
    public sealed partial class SceneRenderer : ISceneRenderer, IDisposable
    {
        private readonly GraphicsDevice _gd;
        private readonly ContentManager _content;
        private readonly FullscreenQuad _quad;
        private readonly PostProcessor _post;

        private readonly Effect _litFx, _skyFx, _particleFx, _projectorFx, _spriteFx;

        private readonly Texture2D _white, _black, _flatNormal;

        private RenderTarget2D _sceneColor, _depthNormals, _shadowMap;
        private int _targetW, _targetH;
        private readonly SurfaceFormat _hdrFormat;

        private SceneRenderStats _stats;

        /// <summary>
        /// Empirical multiplier on the panoramic sky. The documented Skybox/Panoramic maths (tint * unity_ColorSpaceDouble 4.5948 * exposure)
        /// renders the sky 1.89x too bright compared to the Unity baseline screenshots (levels, shadows and tonemap match to within a few
        /// 8-bit values, so the sky constant is what differs); 0.53 reproduces the baseline sky colour to +-1-2 levels. Set to 1 for the formula.
        /// </summary>
        public float SkyCalibration { get; set; } = 0.53f;

        /// <summary>Resolution of the single shadow cascade (URP asset: 1024).</summary>
        public int ShadowMapSize { get; }

        /// <summary>Render-target format used for the HDR scene buffer.</summary>
        public SurfaceFormat HdrFormat => _hdrFormat;

        /// <summary>Intermediate targets for debugging / screenshots (valid after Render).</summary>
        public RenderTarget2D DebugShadowMap => _shadowMap;
        public RenderTarget2D DebugDepthNormals => _depthNormals;
        public RenderTarget2D DebugSsao => _post.SsaoTexture;

        public Texture2D Overlay { get { return _post.Overlay; } set { _post.Overlay = value; } }
        public RenderTarget2D DebugSceneColor => _sceneColor;

        /// <summary>Statistics of the last <see cref="Render"/> call.</summary>
        public SceneRenderStats Stats => _stats;

        /// <summary>Particle systems drawn in the transparent pass (M3). Update it from the game's Update.</summary>
        public ParticleManager Particles { get; }

        public SceneRenderer(GraphicsDevice graphicsDevice, ContentManager content, int shadowMapSize = 1024)
        {
            _gd = graphicsDevice;
            _content = content;
            ShadowMapSize = shadowMapSize;

            _hdrFormat = ProbeHdrFormat(graphicsDevice);

            _litFx = content.Load<Effect>("Effects/Lit");
            _skyFx = content.Load<Effect>("Effects/Skybox");
            _particleFx = content.Load<Effect>("Effects/Particles");
            _projectorFx = content.Load<Effect>("Effects/Projector");
            _spriteFx = content.Load<Effect>("Effects/Sprite");

            _quad = new FullscreenQuad(graphicsDevice);
            _post = new PostProcessor(graphicsDevice, content, _quad, _hdrFormat);
            Particles = new ParticleManager(graphicsDevice, _particleFx);

            _white = MakeTexture(graphicsDevice, Color.White);
            _black = MakeTexture(graphicsDevice, new Color(0, 0, 0, 255));
            _flatNormal = MakeTexture(graphicsDevice, new Color(128, 128, 255, 255));

            _shadowMap = new RenderTarget2D(graphicsDevice, shadowMapSize, shadowMapSize, false, SurfaceFormat.Single, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            InitLitConstants();
        }

        private static Texture2D MakeTexture(GraphicsDevice gd, Color c)
        {
            var t = new Texture2D(gd, 1, 1, false, SurfaceFormat.Color);
            t.SetData(new[] { c });
            return t;
        }

        /// <summary>
        /// Tries HalfVector4 (RGBA16F), then Rgba1010102, then Color; each candidate is verified by clearing to a value above 1 and reading it back.
        /// </summary>
        private static SurfaceFormat ProbeHdrFormat(GraphicsDevice gd)
        {
            SurfaceFormat[] candidates = { SurfaceFormat.HalfVector4, SurfaceFormat.Rgba1010102, SurfaceFormat.Color };
            foreach (var f in candidates)
            {
                try
                {
                    using (var rt = new RenderTarget2D(gd, 4, 4, false, f, DepthFormat.None))
                    {
                        gd.SetRenderTarget(rt);
                        gd.Clear(ClearOptions.Target, new Vector4(4f, 0.5f, 0.25f, 1f), 1f, 0);
                        gd.SetRenderTarget(null);
                        if (f == SurfaceFormat.HalfVector4)
                        {
                            var data = new HalfVector4[16];
                            rt.GetData(data);
                            Vector4 v = data[0].ToVector4();
                            if (v.X > 3.5f)
                                return f;
                        }
                        else
                        {
                            return f; // 10-bit / 8-bit formats cannot store >1; accepted as fallback
                        }
                    }
                }
                catch (Exception)
                {
                    gd.SetRenderTarget(null);
                }
            }
            return SurfaceFormat.Color;
        }

        private void EnsureTargets(int w, int h)
        {
            if (_sceneColor != null && w == _targetW && h == _targetH)
                return;
            _sceneColor?.Dispose();
            _depthNormals?.Dispose();
            _targetW = w;
            _targetH = h;
            _sceneColor = new RenderTarget2D(_gd, w, h, false, _hdrFormat, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            _depthNormals = new RenderTarget2D(_gd, w, h, false, SurfaceFormat.Vector4, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
            _post.Resize(w, h);
        }

        public void Dispose()
        {
            _sceneColor?.Dispose();
            _depthNormals?.Dispose();
            _shadowMap?.Dispose();
            _post.Dispose();
            _white.Dispose();
            _black.Dispose();
            _flatNormal.Dispose();
        }

        // ================================================================== frame

        private struct FrameLight
        {
            public Vector3 SunDirection;
            public Vector3 SunColor;
            public Vector3 AmbientColor;
            public bool Shadows;
            public Matrix ShadowViewProjection;
            public Vector4 ShadowParams;
            public Vector2 ShadowFade;
            public Vector3 ShadowBias;
        }

        private RenderItem[] _items = new RenderItem[0];
        private BoundingBox[] _worldBounds = new BoundingBox[0];
        private bool[] _hasBounds = new bool[0];

        internal struct Entry
        {
            public int Item;
            public int Part;
            public int Queue;
            public float Depth;
            public ParticleSystemInstance Particle;
        }

        private readonly List<Entry> _opaque = new List<Entry>(512);
        private readonly List<Entry> _transparent = new List<Entry>(128);
        private readonly List<Entry> _weaponOpaque = new List<Entry>(32);
        private readonly List<Entry> _weaponTransparent = new List<Entry>(32);
        private readonly List<int> _casters = new List<int>(512);

        private static readonly Comparison<Entry> OpaqueOrder = (a, b) =>
        {
            int c = a.Queue.CompareTo(b.Queue);
            return c != 0 ? c : a.Depth.CompareTo(b.Depth); // front to back
        };

        private static readonly Comparison<Entry> TransparentOrder = (a, b) =>
        {
            int c = a.Queue.CompareTo(b.Queue);
            return c != 0 ? c : b.Depth.CompareTo(a.Depth); // back to front
        };

        public void Render(GameTime gameTime, IReadOnlyList<RenderItem> items, SceneLighting lighting, Camera camera, Camera weaponCamera, PostProcessSettings post)
        {
            if (post == null)
                post = new PostProcessSettings();
            if (lighting == null)
                lighting = new SceneLighting();

            _stats = default;

            // remember what the caller had bound; the final pass writes there
            RenderTargetBinding[] savedTargets = _gd.GetRenderTargets();
            Viewport savedViewport = _gd.Viewport;
            int w, h;
            if (savedTargets.Length > 0 && savedTargets[0].RenderTarget is RenderTarget2D rt)
            {
                w = rt.Width;
                h = rt.Height;
            }
            else
            {
                w = _gd.PresentationParameters.BackBufferWidth;
                h = _gd.PresentationParameters.BackBufferHeight;
            }
            EnsureTargets(w, h);

            // ---- collect items
            int n = items.Count;
            if (_items.Length < n)
            {
                _items = new RenderItem[n];
                _worldBounds = new BoundingBox[n];
                _hasBounds = new bool[n];
            }
            for (int i = 0; i < n; i++)
            {
                _items[i] = items[i];
                BoundsFor(i);
            }
            _stats.ItemsTotal = n;

            Matrix viewProjection = camera.View * camera.Projection;
            var frustum = new BoundingFrustum(viewProjection);
            Matrix invView = Matrix.Invert(camera.View);
            Vector3 camForward = invView.Forward;
            Vector3 camPos = invView.Translation;
            if (camera.Position != Vector3.Zero)
                camPos = camera.Position;

            _opaque.Clear();
            _transparent.Clear();
            _weaponOpaque.Clear();
            _weaponTransparent.Clear();

            Matrix weaponViewProjection = weaponCamera != null ? weaponCamera.View * weaponCamera.Projection : viewProjection;
            Vector3 weaponForward = weaponCamera != null ? Matrix.Invert(weaponCamera.View).Forward : camForward;
            Vector3 weaponPos = weaponCamera != null ? Matrix.Invert(weaponCamera.View).Translation : camPos;

            for (int i = 0; i < n; i++)
            {
                ref RenderItem item = ref _items[i];
                if (item.Mesh == null)
                    continue;

                bool weapon = item.Layer == RenderLayer.Weapon;
                if (!weapon && _hasBounds[i] && frustum.Contains(_worldBounds[i]) == ContainmentType.Disjoint)
                    continue;

                Vector3 center = _hasBounds[i]
                    ? (_worldBounds[i].Min + _worldBounds[i].Max) * 0.5f
                    : item.World.Translation;
                float depth = Vector3.Dot(center - (weapon ? weaponPos : camPos), weapon ? weaponForward : camForward);

                _stats.ItemsVisible++;
                for (int p = 0; p < item.Mesh.Parts.Count; p++)
                {
                    Material m = item.Mesh.Parts[p].Material;
                    if (m == null || m.Kind == MaterialKind.Skybox)
                        continue;
                    var e = new Entry { Item = i, Part = p, Depth = depth, Queue = QueueOf(m) };
                    bool transparent = m.Surface == SurfaceType.Transparent;
                    if (weapon)
                        (transparent ? _weaponTransparent : _weaponOpaque).Add(e);
                    else
                        (transparent ? _transparent : _opaque).Add(e);
                }
            }

            Particles.CollectVisible(camera, camPos, camForward, _transparent, this, weaponCamera, weaponPos, weaponForward, _weaponTransparent);

            _opaque.Sort(OpaqueOrder);
            _transparent.Sort(TransparentOrder);
            _weaponOpaque.Sort(OpaqueOrder);
            _weaponTransparent.Sort(TransparentOrder);

            // ---- lighting constants
            FrameLight light = BuildLight(lighting, camera, camPos);

            // ---- passes
            var swp = System.Diagnostics.Stopwatch.StartNew();
            bool prof = Environment.GetEnvironmentVariable("PROFILE") != null;
            if (light.Shadows)
                RenderShadowMap(lighting, camera, ref light, n);
            if (prof) { Console.WriteLine($"  shadow {swp.Elapsed.TotalMilliseconds:F1} ms"); swp.Restart(); }

            bool wantSsao = post.Enabled && post.Ssao;
            bool wantDepthNormals = wantSsao || (post.Enabled && post.MotionBlurIntensity > 0f);
            if (wantDepthNormals)
            {
                RenderDepthNormals(camera, weaponCamera, camPos, weaponPos);
                if (wantSsao)
                    _post.ComputeSsao(_depthNormals, camera);
            }

            if (prof) { Console.WriteLine($"  depth+ssao {swp.Elapsed.TotalMilliseconds:F1} ms"); swp.Restart(); }
            RenderScene(lighting, camera, weaponCamera, camPos, weaponPos, ref light, wantSsao);
            if (prof) { Console.WriteLine($"  scene {swp.Elapsed.TotalMilliseconds:F1} ms"); swp.Restart(); }

            _post.Run(_sceneColor, wantDepthNormals ? _depthNormals : null, camera, post, post.Enabled, savedTargets, savedViewport);

            if (prof) { Console.WriteLine($"  post {swp.Elapsed.TotalMilliseconds:F1} ms"); swp.Restart(); }
            // leave the device in a sane state for SpriteBatch-based UI drawn afterwards
            _gd.BlendState = BlendState.Opaque;
            _gd.DepthStencilState = DepthStencilState.Default;
            _gd.RasterizerState = RasterizerState.CullCounterClockwise;
            _gd.SamplerStates[0] = SamplerState.LinearClamp;
            for (int i = 1; i < 8; i++)
                _gd.Textures[i] = null;
        }

        private void BoundsFor(int i)
        {
            ref RenderItem item = ref _items[i];
            BoundingBox b = item.Mesh != null ? item.Mesh.Bounds : default;
            if (item.Mesh == null || b.Min == b.Max)
            {
                _hasBounds[i] = false;
                return;
            }
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            Matrix wm = item.World;
            for (int c = 0; c < 8; c++)
            {
                var corner = new Vector3((c & 1) == 0 ? b.Min.X : b.Max.X, (c & 2) == 0 ? b.Min.Y : b.Max.Y, (c & 4) == 0 ? b.Min.Z : b.Max.Z);
                Vector3 t = Vector3.Transform(corner, wm);
                min = Vector3.Min(min, t);
                max = Vector3.Max(max, t);
            }
            _worldBounds[i] = new BoundingBox(min, max);
            _hasBounds[i] = true;
            if (item.BoneMatrices != null)
            {
                // skinned meshes can leave their bind-pose bounds: inflate a little (conservative)
                Vector3 ext = (max - min) * 0.5f;
                _worldBounds[i] = new BoundingBox(min - ext, max + ext);
            }
        }

        private static int QueueOf(Material m)
        {
            if (m.RenderQueue >= 0)
                return m.RenderQueue;
            if (m.Surface == SurfaceType.Transparent)
                return 3000;
            return m.AlphaClip ? 2450 : 2000;
        }

        private FrameLight BuildLight(SceneLighting l, Camera camera, Vector3 camPos)
        {
            FrameLight f = new FrameLight();
            f.SunDirection = Vector3.Normalize(l.SunDirection);
            Vector3 sun = l.ColorsAreGamma ? GammaToLinear(l.SunColor) : l.SunColor;
            Vector3 amb = l.ColorsAreGamma ? GammaToLinear(l.AmbientColor) : l.AmbientColor;
            f.SunColor = sun * l.SunIntensity;
            f.AmbientColor = amb * l.AmbientIntensity;
            f.Shadows = l.SunShadows;
            f.ShadowParams = new Vector4(l.SunShadows ? 1f : 0f, 1f / ShadowMapSize, ShadowMapSize, 0f);

            // URP: GetScaleAndBiasForLinearDistanceFade(shadowDistance^2, cascadeBorder 0.2)
            float maxSq = Math.Min(l.ShadowDistance, camera.FarPlane);
            maxSq *= maxSq;
            float border = (1f - 0.2f);
            border *= border;
            float fadeNear = border * maxSq;
            f.ShadowFade = new Vector2(1f / (maxSq - fadeNear), -fadeNear / (maxSq - fadeNear));
            return f;
        }

        internal static Vector3 GammaToLinear(Vector3 c)
        {
            return new Vector3(PostProcessor.GammaToLinear(c.X), PostProcessor.GammaToLinear(c.Y), PostProcessor.GammaToLinear(c.Z));
        }

        internal static Vector4 GammaToLinear(Vector4 c)
        {
            return new Vector4(PostProcessor.GammaToLinear(c.X), PostProcessor.GammaToLinear(c.Y), PostProcessor.GammaToLinear(c.Z), c.W);
        }
    }
}
