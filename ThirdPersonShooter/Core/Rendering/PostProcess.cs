using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>HDR scene target + bloom chain + AgX tonemap pass. Tries HalfVector4, Rgba1010102, then Color (with HdrScale pre-scale).</summary>
    public sealed class PostProcess
    {
        readonly GraphicsDevice _gd;
        readonly Effect _tone, _bloom;
        readonly VertexPositionTexture[] _quad =
        {
            new(new Vector3(-1, 1, 0), new Vector2(0, 0)), new(new Vector3(1, 1, 0), new Vector2(1, 0)),
            new(new Vector3(-1, -1, 0), new Vector2(0, 1)), new(new Vector3(1, -1, 0), new Vector2(1, 1)),
        };
        RenderTarget2D _scene; RenderTarget2D[] _b = new RenderTarget2D[3], _t = new RenderTarget2D[3];
        public SurfaceFormat Format { get; private set; }
        public float HdrScale { get; private set; } = 1f;
        public float Exposure = 4.5f, Saturation = 1.4f;       // environment.tres tonemap_exposure, adjustment_saturation
        public float BloomThreshold = 2.5f, BloomIntensity = 0.35f;
        public bool BloomOn = true;
        public float SCurve = 0.85f, Gamma = 1.2f, BlackPoint = 0.015f;

        public PostProcess(GraphicsDevice gd, Effect tone, Effect bloom) { _gd = gd; _tone = tone; _bloom = bloom; Exposure = SceneLighting.Tune("TPS_EXP", 1.9f); Gamma = SceneLighting.Tune("TPS_GAMMA", Gamma); Saturation = SceneLighting.Tune("TPS_SAT", 1.8f); SCurve = SceneLighting.Tune("TPS_SCURVE", SCurve); BlackPoint = SceneLighting.Tune("TPS_BLACK", BlackPoint); BloomThreshold = SceneLighting.Tune("TPS_BT", BloomThreshold); BloomIntensity = SceneLighting.Tune("TPS_BI", BloomIntensity); }

        public RenderTarget2D Begin(int w, int h)
        {
            if (_scene == null || _scene.Width != w || _scene.Height != h) Create(w, h);
            _gd.SetRenderTarget(_scene); _gd.Viewport = new Viewport(0, 0, w, h);   // spot shadow tiles leave a sub-rectangle viewport behind
            return _scene;
        }

        void Create(int w, int h)
        {
            var fmts = new[] { SurfaceFormat.HalfVector4, SurfaceFormat.Rgba1010102, SurfaceFormat.Color };
            foreach (var f in fmts)
            {
                try
                {
                    var rt = new RenderTarget2D(_gd, w, h, false, f, DepthFormat.Depth24);
                    _gd.SetRenderTarget(rt); _gd.Clear(Color.Black); _gd.SetRenderTarget(null);
                    _scene = rt; Format = f; break;
                }
                catch (Exception e) { Console.WriteLine($"[hdr] {f} failed: {e.GetType().Name} {e.Message}"); }
            }
            HdrScale = Format == SurfaceFormat.Color ? 1f / 16f : (Format == SurfaceFormat.Rgba1010102 ? 1f / 8f : 1f);
            Console.WriteLine($"[hdr] scene target format = {Format}, HdrScale={HdrScale}");
            var bf = Format == SurfaceFormat.Color ? SurfaceFormat.Color : (Format == SurfaceFormat.Rgba1010102 ? SurfaceFormat.Rgba1010102 : SurfaceFormat.HalfVector4);
            for (int i = 0; i < 3; i++)
            {
                int d = 2 << i; // 1/2, 1/4, 1/8
                _b[i] = new RenderTarget2D(_gd, Math.Max(1, w / d), Math.Max(1, h / d), false, bf, DepthFormat.None);
                _t[i] = new RenderTarget2D(_gd, Math.Max(1, w / d), Math.Max(1, h / d), false, bf, DepthFormat.None);
            }
        }

        void Quad(Effect fx, string tech)
        {
            fx.CurrentTechnique = fx.Techniques[tech];
            fx.CurrentTechnique.Passes[0].Apply();
            _gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, _quad, 0, 2);
        }

        /// <summary>Bloom + tonemap the scene into <paramref name="target"/> (null = backbuffer).</summary>
        public void Resolve(RenderTarget2D target)
        {
            _gd.BlendState = BlendState.Opaque; _gd.DepthStencilState = DepthStencilState.None; _gd.RasterizerState = RasterizerState.CullNone;
            _gd.SamplerStates[0] = SamplerState.LinearClamp; _gd.SamplerStates[1] = SamplerState.LinearClamp; _gd.SamplerStates[2] = SamplerState.LinearClamp; _gd.SamplerStates[3] = SamplerState.LinearClamp;
            if (BloomOn)
            {
                RenderTarget2D src = _scene;
                for (int i = 0; i < 3; i++)
                {
                    _gd.SetRenderTarget(_b[i]); _gd.Clear(Color.Black);
                    _bloom.Parameters["SrcTex"].SetValue(src);
                    _bloom.Parameters["TexelSize"].SetValue(new Vector2(1f / src.Width, 1f / src.Height));
                    _bloom.Parameters["Threshold"].SetValue(BloomThreshold); _bloom.Parameters["Exposure"].SetValue(Exposure); _bloom.Parameters["HdrScale"].SetValue(HdrScale);
                    Quad(_bloom, i == 0 ? "Prefilter" : "Down");
                    for (int pass = 0; pass < 2; pass++)
                    {
                        var from = pass == 0 ? _b[i] : _t[i]; var to = pass == 0 ? _t[i] : _b[i];
                        _gd.SetRenderTarget(to);
                        _bloom.Parameters["SrcTex"].SetValue(from);
                        _bloom.Parameters["TexelSize"].SetValue(new Vector2(1f / from.Width, 1f / from.Height));
                        _bloom.Parameters["BlurDir"].SetValue(pass == 0 ? new Vector2(1, 0) : new Vector2(0, 1));
                        Quad(_bloom, "Blur");
                    }
                    src = _b[i];
                }
            }
            _gd.SetRenderTarget(target);
            _tone.Parameters["SceneTex"].SetValue(_scene);
            for (int i = 0; i < 3; i++) _tone.Parameters["Bloom" + (i + 1) + "Tex"].SetValue(_b[i]);
            _tone.Parameters["Exposure"].SetValue(Exposure); _tone.Parameters["Saturation"].SetValue(Saturation); _tone.Parameters["Gamma"].SetValue(Gamma); _tone.Parameters["SCurve"].SetValue(SCurve); _tone.Parameters["BlackPoint"].SetValue(BlackPoint);
            _tone.Parameters["HdrScale"].SetValue(HdrScale); _tone.Parameters["BloomIntensity"].SetValue(BloomOn ? BloomIntensity : 0f);
            _tone.Parameters["InvSize"].SetValue(new Vector2(1f / _scene.Width, 1f / _scene.Height));
            Quad(_tone, "Tonemap");
        }
    }
}
