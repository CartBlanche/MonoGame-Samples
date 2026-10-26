using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter.Rendering
{
    /// <summary>
    /// Full-screen quad with positions in NDC and uv (0,0) at the top-left (matches the PostVS vertex shader in Effects/PostCommon.fxh).
    /// </summary>
    internal sealed class FullscreenQuad
    {
        private readonly GraphicsDevice _device;
        private readonly VertexPositionTexture[] _vertices =
        {
            new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
        };
        private readonly short[] _indices = { 0, 1, 2, 0, 2, 3 };

        public FullscreenQuad(GraphicsDevice device)
        {
            _device = device;
        }

        public void Draw(EffectPass pass)
        {
            pass.Apply();
            _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, 4, _indices, 0, 2);
        }
    }
}
