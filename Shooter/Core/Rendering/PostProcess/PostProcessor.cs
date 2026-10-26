using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter.Rendering.PostProcess
{
    /// <summary>
    /// The URP post-processing chain used by the Unity project, ported to MonoGame (see the .fx headers for the exact source of each pass):
    /// SSAO (depth-normals source, bilateral blur) -> camera motion blur -> Gaussian bloom pyramid -> UberPost (bloom, vignette, ACES)
    /// -> FinalPost (FXAA 3.11, film grain, linear to sRGB) into the caller's render target.
    /// </summary>
    internal sealed class PostProcessor : IDisposable
    {
        private readonly GraphicsDevice _gd;
        private readonly FullscreenQuad _quad;
        private readonly SurfaceFormat _hdrFormat;

        private readonly Effect _bloom, _uber, _final, _motion, _ssao;
        private readonly Texture2D _grain;

        private int _w, _h;
        private RenderTarget2D _motionTarget, _ldrTarget;
        private RenderTarget2D _ssaoA, _ssaoB, _ssaoFinal;
        private RenderTarget2D[] _bloomDown = new RenderTarget2D[0];
        private RenderTarget2D[] _bloomUp = new RenderTarget2D[0];

        private Matrix _prevViewProjection;
        private bool _hasPrev;
        private int _frame;

        /// <summary>Screen-space AO result (r = 1 - occlusion), valid after <see cref="ComputeSsao"/>.</summary>
        public RenderTarget2D SsaoTexture => _ssaoFinal;

        /// <summary>Optional UI layer (premultiplied, linear light) composited in the final pass; null = none.</summary>
        public Texture2D Overlay;
        private Texture2D _noOverlay;   // bound when there is no UI layer so the sampler never points at nothing

        public PostProcessor(GraphicsDevice gd, ContentManager content, FullscreenQuad quad, SurfaceFormat hdrFormat)
        {
            _gd = gd;
            _quad = quad;
            _hdrFormat = hdrFormat;
            _bloom = content.Load<Effect>("Effects/Bloom");
            _uber = content.Load<Effect>("Effects/Uber");
            _final = content.Load<Effect>("Effects/FinalPost");
            _motion = content.Load<Effect>("Effects/MotionBlur");
            _ssao = content.Load<Effect>("Effects/Ssao");
            _grain = content.Load<Texture2D>("Effects/FilmGrain_Thin1");
            _noOverlay = new Texture2D(_gd, 1, 1);
            _noOverlay.SetData(new[] { Color.Transparent });
        }

        public void Resize(int w, int h)
        {
            if (w == _w && h == _h)
                return;
            ReleaseTargets();
            _w = w;
            _h = h;
            _motionTarget = new RenderTarget2D(_gd, w, h, false, _hdrFormat, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _ldrTarget = new RenderTarget2D(_gd, w, h, false, _hdrFormat, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _ssaoA = new RenderTarget2D(_gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _ssaoB = new RenderTarget2D(_gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _ssaoFinal = new RenderTarget2D(_gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            int bw = Math.Max(1, w >> 1);
            int bh = Math.Max(1, h >> 1);
            int mipCount = Math.Max(1, Math.Min(6, (int)Math.Floor(Math.Log(Math.Max(bw, bh), 2.0) - 1)));
            _bloomDown = new RenderTarget2D[mipCount];
            _bloomUp = new RenderTarget2D[mipCount];
            for (int i = 0; i < mipCount; i++)
            {
                int mw = Math.Max(1, bw >> i);
                int mh = Math.Max(1, bh >> i);
                _bloomDown[i] = new RenderTarget2D(_gd, mw, mh, false, _hdrFormat, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                _bloomUp[i] = new RenderTarget2D(_gd, mw, mh, false, _hdrFormat, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }

        private void ReleaseTargets()
        {
            _motionTarget?.Dispose();
            _ldrTarget?.Dispose();
            _ssaoA?.Dispose();
            _ssaoB?.Dispose();
            _ssaoFinal?.Dispose();
            foreach (var t in _bloomDown) t?.Dispose();
            foreach (var t in _bloomUp) t?.Dispose();
        }

        public void Dispose()
        {
            ReleaseTargets();
        }

        private void Fullscreen(RenderTarget2D target, Effect fx, string technique)
        {
            _gd.SetRenderTarget(target);
            _gd.BlendState = BlendState.Opaque;
            _gd.DepthStencilState = DepthStencilState.None;
            _gd.RasterizerState = RasterizerState.CullNone;
            fx.CurrentTechnique = fx.Techniques[technique];
            _quad.Draw(fx.CurrentTechnique.Passes[0]);
        }

        // ------------------------------------------------------------------ SSAO

        public void ComputeSsao(RenderTarget2D depthNormals, Camera cam)
        {
            var fx = _ssao;
            Vector2 texel = new Vector2(1f / _w, 1f / _h);
            Vector4 ssaoParams = new Vector4(0.3f, 0.035f, 1f, 100f);
            if (Environment.GetEnvironmentVariable("SSAO_TEST") != null)
                ssaoParams = new Vector4(2.0f, 0.6f, 1f, 100f);
            fx.Parameters["SsaoParams"].SetValue(ssaoParams);
            fx.Parameters["ProjScale"].SetValue(new Vector2(cam.Projection.M11, cam.Projection.M22));
            fx.Parameters["TexelSize"].SetValue(texel);
            fx.Parameters["ScreenSize"].SetValue(new Vector2(_w, _h));

            _gd.SamplerStates[0] = SamplerState.PointClamp;
            _gd.SamplerStates[1] = SamplerState.LinearClamp;

            fx.Parameters["DepthTex"].SetValue(depthNormals);
            Fullscreen(_ssaoA, fx, "SsaoMain");

            fx.Parameters["BlurDirection"].SetValue(new Vector2(1, 0));
            fx.Parameters["AoTex"].SetValue(_ssaoA);
            Fullscreen(_ssaoB, fx, "SsaoBlur");

            fx.Parameters["BlurDirection"].SetValue(new Vector2(0, 1));
            fx.Parameters["AoTex"].SetValue(_ssaoB);
            Fullscreen(_ssaoA, fx, "SsaoBlur");

            fx.Parameters["AoTex"].SetValue(_ssaoA);
            Fullscreen(_ssaoFinal, fx, "SsaoFinal");
            fx.Parameters["DepthTex"].SetValue((Texture2D)null);
            fx.Parameters["AoTex"].SetValue((Texture2D)null);
        }

        // ------------------------------------------------------------------ chain

        /// <summary>
        /// Runs motion blur, bloom, uber and final passes. <paramref name="finalTargets"/> is what the caller had bound (empty = back buffer).
        /// </summary>
        public void Run(RenderTarget2D scene, RenderTarget2D depthNormals, Camera cam, PostProcessSettings s, bool enabled,
            RenderTargetBinding[] finalTargets, Viewport finalViewport)
        {
            _frame++;
            Matrix viewProjection = cam.View * cam.Projection;
            if (!_hasPrev)
                _prevViewProjection = viewProjection;

            RenderTarget2D source = scene;

            if (enabled && s.MotionBlurIntensity > 0f && depthNormals != null)
            {
                var fx = _motion;
                if (Environment.GetEnvironmentVariable("DUMP") != null)
                {
                    Console.WriteLine($"motion: vp==prev {viewProjection == _prevViewProjection}");
                    foreach (var prm in fx.Parameters)
                        Console.WriteLine($"Motion param: {prm.Name} {prm.ParameterClass} rows={prm.RowCount} cols={prm.ColumnCount}");
                }
                Matrix invView = Matrix.Invert(cam.View);
                float tanH = 1f / cam.Projection.M22;
                float tanW = 1f / cam.Projection.M11;
                Vector3 tl = Vector3.TransformNormal(new Vector3(-tanW, tanH, -1f), invView);
                Vector3 xe = Vector3.TransformNormal(new Vector3(2f * tanW, 0f, 0f), invView);
                Vector3 ye = Vector3.TransformNormal(new Vector3(0f, -2f * tanH, 0f), invView);
                fx.Parameters["ViewProjection"].SetValue(viewProjection);
                fx.Parameters["PreviousViewProjection"].SetValue(_prevViewProjection);
                fx.Parameters["CameraPosition"].SetValue(invView.Translation);
                fx.Parameters["RayTopLeft"].SetValue(tl);
                fx.Parameters["RayXExtent"].SetValue(xe);
                fx.Parameters["RayYExtent"].SetValue(ye);
                fx.Parameters["Intensity"].SetValue(s.MotionBlurIntensity);
                float mbClamp = 0.05f;
                string envClamp = Environment.GetEnvironmentVariable("MB_CLAMP");
                if (envClamp != null) mbClamp = float.Parse(envClamp, System.Globalization.CultureInfo.InvariantCulture);
                fx.Parameters["VelocityClamp"].SetValue(mbClamp);
                fx.Parameters["SourceSize"].SetValue(new Vector2(_w, _h));
                fx.Parameters["SourceTex"].SetValue(source);
                fx.Parameters["DepthTex"].SetValue(depthNormals);
                _gd.SamplerStates[0] = SamplerState.PointClamp;
                _gd.SamplerStates[1] = SamplerState.PointClamp;
                Fullscreen(_motionTarget, fx, "MotionBlur");
                fx.Parameters["SourceTex"].SetValue((Texture2D)null);
                fx.Parameters["DepthTex"].SetValue((Texture2D)null);
                source = _motionTarget;
            }

            Texture2D bloomTexture = null;
            bool bloomOn = enabled && s.BloomIntensity > 0f;
            if (bloomOn)
                bloomTexture = RunBloom(source, s);

            // Uber
            {
                var fx = _uber;
                fx.Parameters["BloomParams"].SetValue(new Vector4(s.BloomIntensity, 1f, 1f, 1f));
                fx.Parameters["VignetteParams1"].SetValue(new Vector4(s.VignetteColor, 1f)); // not rounded -> roundness 1
                fx.Parameters["VignetteParams2"].SetValue(new Vector4(0.5f, 0.5f, s.VignetteIntensity * 3f, s.VignetteSmoothness * 5f));
                fx.Parameters["UberFlags"].SetValue(new Vector4(bloomOn ? 1f : 0f, enabled && s.VignetteIntensity > 0f ? 1f : 0f, enabled && s.Tonemapping ? 1f : 0f, 0f));
                fx.Parameters["PostExposure"].SetValue(1f);
                fx.Parameters["SourceTex"].SetValue(source);
                fx.Parameters["BloomTex"].SetValue(bloomTexture ?? source);
                _gd.SamplerStates[0] = SamplerState.LinearClamp;
                _gd.SamplerStates[1] = SamplerState.LinearClamp;
                Fullscreen(_ldrTarget, fx, "Uber");
                fx.Parameters["SourceTex"].SetValue((Texture2D)null);
                fx.Parameters["BloomTex"].SetValue((Texture2D)null);
            }

            // Final
            {
                var fx = _final;
                bool grain = enabled && s.FilmGrainIntensity > 0f;
                bool fxaa = enabled && s.Fxaa;
                fx.Parameters["SourceSize"].SetValue(new Vector4(_w, _h, 1f / _w, 1f / _h));
                // URP: scale = pixels / grain texture size, random offset per frame (Random.InitState(frameCount))
                var rnd = new Random(_frame * 7919 + 17);
                fx.Parameters["GrainTiling"].SetValue(new Vector4(_w / (float)_grain.Width, _h / (float)_grain.Height, (float)rnd.NextDouble(), (float)rnd.NextDouble()));
                fx.Parameters["GrainParams"].SetValue(new Vector2(s.FilmGrainIntensity * 4f, s.FilmGrainResponse));
                fx.Parameters["FinalFlags"].SetValue(new Vector4(fxaa ? 1f : 0f, grain ? 1f : 0f, 1f, Overlay != null ? 1f : 0f));
                fx.Parameters["HudTex"].SetValue(Overlay ?? _noOverlay);
                _gd.SamplerStates[2] = SamplerState.LinearClamp;
                fx.Parameters["SourceTex"].SetValue(_ldrTarget);
                fx.Parameters["GrainTex"].SetValue(_grain);
                _gd.SamplerStates[0] = SamplerState.LinearClamp;
                _gd.SamplerStates[1] = SamplerState.LinearWrap;

                if (finalTargets != null && finalTargets.Length > 0)
                    _gd.SetRenderTargets(finalTargets);
                else
                    _gd.SetRenderTarget(null);
                _gd.Viewport = finalViewport;
                _gd.BlendState = BlendState.Opaque;
                _gd.DepthStencilState = DepthStencilState.None;
                _gd.RasterizerState = RasterizerState.CullNone;
                fx.CurrentTechnique = fx.Techniques["FinalPost"];
                _quad.Draw(fx.CurrentTechnique.Passes[0]);
                fx.Parameters["SourceTex"].SetValue((Texture2D)null);
                fx.Parameters["HudTex"].SetValue((Texture2D)null);
            }

            _prevViewProjection = viewProjection;
            _hasPrev = true;
        }

        private Texture2D RunBloom(Texture2D source, PostProcessSettings s)
        {
            var fx = _bloom;
            int mipCount = _bloomDown.Length;

            // URP: threshold converted to linear, hard-coded knee 0.5, scatter lerp(0.05, 0.95, user scatter)
            float threshold = GammaToLinear(s.BloomThreshold);
            float knee = threshold * 0.5f;
            float scatter = MathHelper.Lerp(0.05f, 0.95f, s.BloomScatter);
            fx.Parameters["Params"].SetValue(new Vector4(scatter, 65472f, threshold, knee));

            _gd.SamplerStates[0] = SamplerState.LinearClamp;
            _gd.SamplerStates[1] = SamplerState.LinearClamp;

            fx.Parameters["SourceTex"].SetValue(source);
            fx.Parameters["SourceTexelSize"].SetValue(new Vector4(1f / source.Width, 1f / source.Height, source.Width, source.Height));
            Fullscreen(_bloomDown[0], fx, "BloomPrefilter");

            for (int i = 1; i < mipCount; i++)
            {
                var src = _bloomDown[i - 1];
                fx.Parameters["SourceTex"].SetValue(src);
                fx.Parameters["SourceTexelSize"].SetValue(new Vector4(1f / src.Width, 1f / src.Height, src.Width, src.Height));
                Fullscreen(_bloomUp[i], fx, "BloomBlurH");

                var mid = _bloomUp[i];
                fx.Parameters["SourceTex"].SetValue(mid);
                fx.Parameters["SourceTexelSize"].SetValue(new Vector4(1f / mid.Width, 1f / mid.Height, mid.Width, mid.Height));
                Fullscreen(_bloomDown[i], fx, "BloomBlurV");
            }

            for (int i = mipCount - 2; i >= 0; i--)
            {
                var low = (i == mipCount - 2) ? _bloomDown[i + 1] : _bloomUp[i + 1];
                fx.Parameters["SourceTex"].SetValue(_bloomDown[i]);
                fx.Parameters["LowMipTex"].SetValue(low);
                Fullscreen(_bloomUp[i], fx, "BloomUpsample");
            }

            fx.Parameters["SourceTex"].SetValue((Texture2D)null);
            fx.Parameters["LowMipTex"].SetValue((Texture2D)null);
            return mipCount == 1 ? _bloomDown[0] : _bloomUp[0];
        }

        internal static float GammaToLinear(float c)
        {
            return c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
        }
    }
}
