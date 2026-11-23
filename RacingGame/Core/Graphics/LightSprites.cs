using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RacingGame.Graphics
{
    /// <summary>
    /// Shared additive glow billboards (headlights, tail lights, later lamps).
    /// Producers call <see cref="Add"/> while the scene is built; GameScreen calls
    /// <see cref="Flush"/> once after the 3D scene so the quads are depth tested
    /// against it (depth read only) and picked up by the glow post shader.
    /// Uses the existing Glow.tga texture; one BasicEffect and one user-primitive draw.
    /// </summary>
    public sealed class LightSprites
    {
        struct Sprite
        {
            public Vector3 Pos;
            public Color Color;
            public float Size;
            public float Growth;
        }

        const int MaxSprites = 64;
        readonly List<Sprite> sprites = new List<Sprite>(MaxSprites);
        readonly VertexPositionColorTexture[] vertices =
            new VertexPositionColorTexture[MaxSprites * 6];
        BasicEffect effect;
        Texture glow;

        static readonly DepthStencilState depthRead = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = false,
        };

        /// <summary>
        /// Queue a glow. <paramref name="size"/> is the world size at 10 units
        /// distance; it grows with distance so far lights stay visible.
        /// <paramref name="color"/> is rgb times intensity (alpha ignored).
        /// </summary>
        public void Add(Vector3 worldPos, Vector3 color, float size)
        {
            Add(worldPos, color, size, 1.0f / 15.0f);
        }

        /// <summary>
        /// Same, with the growth of the glow per unit of distance: the default 1/15 keeps far car
        /// lights visible as bright blobs; fixed lamps use a much smaller value so the glow stays
        /// close to the size of the fixture it sits on.
        /// </summary>
        public void Add(Vector3 worldPos, Vector3 color, float size, float distanceGrowth)
        {
            if (sprites.Count >= MaxSprites)
                return;
            float m = MathHelper.Clamp(
                System.Math.Max(color.X, System.Math.Max(color.Y, color.Z)), 0.0f, 1.0f);
            if (m <= 0.004f)
                return;
            sprites.Add(new Sprite
            {
                Pos = worldPos,
                Color = new Color(
                    MathHelper.Clamp(color.X, 0, 1),
                    MathHelper.Clamp(color.Y, 0, 1),
                    MathHelper.Clamp(color.Z, 0, 1), 1.0f),
                Size = size,
                Growth = distanceGrowth,
            });
        }

        /// <summary>Draw and clear the queue. Call after the 3D scene, before post shaders.</summary>
        public void Flush()
        {
            if (sprites.Count == 0)
                return;
            GraphicsDevice device = BaseGame.Device;
            if (effect == null)
            {
                effect = new BasicEffect(device);
                effect.TextureEnabled = true;
                effect.VertexColorEnabled = true;
                effect.LightingEnabled = false;
                effect.FogEnabled = false;
                glow = new Texture("Glow");
            }

            Matrix inv = BaseGame.InverseViewMatrix;
            Vector3 right = inv.Right, up = inv.Up, cam = inv.Translation;
            int v = 0;
            for (int i = 0; i < sprites.Count; i++)
            {
                Sprite s = sprites[i];
                Vector3 toCam = cam - s.Pos;
                float dist = toCam.Length();
                if (dist < 0.5f)
                    continue;
                toCam /= dist;
                // Pull towards the camera so the glow is not cut by the car body it sits on
                Vector3 c = s.Pos + toCam * 0.35f;
                float half = 0.5f * s.Size * (0.35f + dist * s.Growth);
                Vector3 r = right * half, u = up * half;
                VertexPositionColorTexture a = new VertexPositionColorTexture(c - r + u, s.Color, new Vector2(0, 0));
                VertexPositionColorTexture b = new VertexPositionColorTexture(c + r + u, s.Color, new Vector2(1, 0));
                VertexPositionColorTexture d = new VertexPositionColorTexture(c - r - u, s.Color, new Vector2(0, 1));
                VertexPositionColorTexture e = new VertexPositionColorTexture(c + r - u, s.Color, new Vector2(1, 1));
                vertices[v++] = a; vertices[v++] = b; vertices[v++] = d;
                vertices[v++] = b; vertices[v++] = e; vertices[v++] = d;
            }
            sprites.Clear();
            if (v == 0)
                return;

            BlendState oldBlend = device.BlendState;
            DepthStencilState oldDepth = device.DepthStencilState;
            RasterizerState oldRaster = device.RasterizerState;
            device.BlendState = BlendState.Additive;
            device.DepthStencilState = depthRead;
            device.RasterizerState = RasterizerState.CullNone;

            effect.World = Matrix.Identity;
            effect.View = BaseGame.ViewMatrix;
            effect.Projection = BaseGame.ProjectionMatrix;
            effect.Texture = glow.XnaTexture;
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, v / 3);

            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRaster;
        }
    }
}
