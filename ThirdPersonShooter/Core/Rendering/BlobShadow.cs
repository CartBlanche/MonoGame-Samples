using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Soft dark disc on the ground under a character (Godot Decal blob_shadow stand-in). Draw after opaque, before alpha.
    /// Pass 2: the custom BlobShadow.fx quad never produced pixels on DesktopGL (cause not found in 3 attempts), so this uses a
    /// BasicEffect + procedural radial-gradient texture (black, alpha 1 at centre -> 0 at edge). The fx argument is kept for API compatibility.
    /// </summary>
    public sealed class BlobShadow
    {
        static readonly RasterizerState _rs = new RasterizerState { CullMode = CullMode.None, DepthBias = -0.0002f, SlopeScaleDepthBias = -2f };
        // result = dst * (1 - srcAlpha): pure darkening, independent of the source colour
        static readonly BlendState _multiply = new BlendState { ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.InverseSourceAlpha, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One };
        readonly BasicEffect _be; readonly Texture2D _tex;
        public float Radius = SceneLighting.Tune("TPS_BLOBR", 1.4f), Opacity = 0.9f, DY = SceneLighting.Tune("TPS_BLOBDY", 0.12f);

        public BlobShadow(GraphicsDevice gd, Effect unused = null)
        {
            const int N = 64; var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float r = Math.Min(1f, new Vector2((x + 0.5f) / N * 2 - 1, (y + 0.5f) / N * 2 - 1).Length());
                px[y * N + x] = new Color(System.Environment.GetEnvironmentVariable("TPS_BLOBRED") != null ? 255 : 0, 0, 0, (int)(255 * MathF.Pow(1 - r, 1.2f)));
            }
            _tex = new Texture2D(gd, N, N, false, SurfaceFormat.Color); _tex.SetData(px);
            _be = new BasicEffect(gd) { TextureEnabled = true, Texture = _tex, LightingEnabled = false, VertexColorEnabled = false, FogEnabled = false };
        }

        public void Draw(GraphicsDevice gd, Matrix view, Matrix proj, Vector3 groundPos)
        {
            float y = groundPos.Y + DY, r = Radius;
            var v = new[]
            {
                new VertexPositionTexture(new Vector3(groundPos.X - r, y, groundPos.Z - r), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(groundPos.X + r, y, groundPos.Z - r), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(groundPos.X - r, y, groundPos.Z + r), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(groundPos.X + r, y, groundPos.Z + r), new Vector2(1, 1)),
            };
            gd.BlendState = _multiply; gd.DepthStencilState = DepthStencilState.DepthRead; gd.RasterizerState = _rs;
            gd.SamplerStates[0] = SamplerState.LinearClamp;
            _be.View = view; _be.Projection = proj; _be.World = Matrix.Identity; _be.Alpha = Opacity;
            _be.CurrentTechnique.Passes[0].Apply();
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, v, 0, 2);
        }
    }
}
