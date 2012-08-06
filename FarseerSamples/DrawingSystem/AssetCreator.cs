using System;
using System.Collections.Generic;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Common.Decomposition;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    public enum MaterialType
    {
        Blank,
        Dots,
        Squares,
        Waves,
        Pavement
    }

    public class AssetCreator
    {
        private const int CircleSegments = 32;

        private GraphicsDevice _device;
        private BasicEffect _effect;
        private Dictionary<MaterialType, Texture2D> _materials = new Dictionary<MaterialType, Texture2D>();

        public AssetCreator(GraphicsDevice device)
        {
            _device = device;
            _effect = new BasicEffect(_device);
        }

        public static Vector2 CalculateOrigin(Body b)
        {
            Vector2 lBound = new Vector2(float.MaxValue);
            AABB bounds;
            Transform trans;
            b.GetTransform(out trans);

            for (int i = 0; i < b.FixtureList.Count; ++i)
            {
                for (int j = 0; j < b.FixtureList[i].Shape.ChildCount; ++j)
                {
                    b.FixtureList[i].Shape.ComputeAABB(out bounds, ref trans, j);
                    Vector2.Min(ref lBound, ref bounds.LowerBound, out lBound);
                }
            }
            // calculate body offset from its center and add a 1 pixel border
            // because we generate the textures a little bigger than the actual body's fixtures
            return ConvertUnits.ToDisplayUnits(b.Position - lBound) + new Vector2(1f);
        }

        public void LoadContent(ContentManager contentManager)
        {
            _materials[MaterialType.Blank] = contentManager.Load<Texture2D>("Materials/blank");
            _materials[MaterialType.Dots] = contentManager.Load<Texture2D>("Materials/dots");
            _materials[MaterialType.Squares] = contentManager.Load<Texture2D>("Materials/squares");
            _materials[MaterialType.Waves] = contentManager.Load<Texture2D>("Materials/waves");
            _materials[MaterialType.Pavement] = contentManager.Load<Texture2D>("Materials/pavement");
        }

        public Texture2D TextureFromShape(Shape shape, MaterialType type, Color color, float materialScale)
        {
            switch (shape.ShapeType)
            {
                case ShapeType.Circle:
                    return CircleTexture(shape.Radius, type, color, materialScale);
                case ShapeType.Polygon:
                    return TextureFromVertices(((PolygonShape) shape).Vertices, type, color, materialScale);
                default:
                    throw new NotSupportedException("The specified shape type is not supported.");
            }
        }

        public Texture2D TextureFromVertices(Vertices vertices, MaterialType type, Color color, float materialScale)
        {
            // copy vertices
            Vertices verts = new Vertices(vertices);

            // scale to display units (i.e. pixels) for rendering to texture
            Vector2 scale = ConvertUnits.ToDisplayUnits(Vector2.One);
            verts.Scale(ref scale);

            // translate the boundingbox center to the texture center
            // because we use an orthographic projection for rendering later
            AABB vertsBounds = verts.GetCollisionBox();
            verts.Translate(-vertsBounds.Center);

            List<Vertices> decomposedVerts;
            if (!verts.IsConvex())
            {
                decomposedVerts = EarclipDecomposer.ConvexPartition(verts);
            }
            else
            {
                decomposedVerts = new List<Vertices>();
                decomposedVerts.Add(verts);
            }
            List<VertexPositionColorTexture[]> verticesFill =
                new List<VertexPositionColorTexture[]>(decomposedVerts.Count);

            materialScale /= _materials[type].Width;

            for (int i = 0; i < decomposedVerts.Count; ++i)
            {
                verticesFill.Add(new VertexPositionColorTexture[3 * (decomposedVerts[i].Count - 2)]);
                for (int j = 0; j < decomposedVerts[i].Count - 2; ++j)
                {
                    // fill vertices
                    verticesFill[i][3 * j].Position = new Vector3(decomposedVerts[i][0], 0f);
                    verticesFill[i][3 * j + 1].Position = new Vector3(decomposedVerts[i].NextVertex(j), 0f);
                    verticesFill[i][3 * j + 2].Position = new Vector3(decomposedVerts[i].NextVertex(j + 1), 0f);
                    verticesFill[i][3 * j].TextureCoordinate = decomposedVerts[i][0] * materialScale;
                    verticesFill[i][3 * j + 1].TextureCoordinate = decomposedVerts[i].NextVertex(j) * materialScale;
                    verticesFill[i][3 * j + 2].TextureCoordinate = decomposedVerts[i].NextVertex(j + 1) * materialScale;
                    verticesFill[i][3 * j].Color =
                        verticesFill[i][3 * j + 1].Color = verticesFill[i][3 * j + 2].Color = color;
                }
            }

            // calculate outline
            VertexPositionColor[] verticesOutline = new VertexPositionColor[2 * verts.Count];
            for (int i = 0; i < verts.Count; ++i)
            {
                verticesOutline[2 * i].Position = new Vector3(verts[i], 0f);
                verticesOutline[2 * i + 1].Position = new Vector3(verts.NextVertex(i), 0f);
                verticesOutline[2 * i].Color = verticesOutline[2 * i + 1].Color = Color.Black;
            }

            Vector2 vertsSize = new Vector2(vertsBounds.UpperBound.X - vertsBounds.LowerBound.X,
                                            vertsBounds.UpperBound.Y - vertsBounds.LowerBound.Y);
            return RenderTexture((int)vertsSize.X, (int)vertsSize.Y,
                                 _materials[type], verticesFill, verticesOutline);
        }

        public Texture2D CircleTexture(float radius, MaterialType type, Color color, float materialScale)
        {
            return EllipseTexture(radius, radius, type, color, materialScale);
        }

        public Texture2D EllipseTexture(float radiusX, float radiusY, MaterialType type, Color color,
                                        float materialScale)
        {
            VertexPositionColorTexture[] verticesFill = new VertexPositionColorTexture[3 * (CircleSegments - 2)];
            VertexPositionColor[] verticesOutline = new VertexPositionColor[2 * CircleSegments];
            const float segmentSize = MathHelper.TwoPi / CircleSegments;
            float theta = segmentSize;

            radiusX = ConvertUnits.ToDisplayUnits(radiusX);
            radiusY = ConvertUnits.ToDisplayUnits(radiusY);
            materialScale /= _materials[type].Width;

            Vector2 start = new Vector2(radiusX, 0f);

            for (int i = 0; i < CircleSegments - 2; ++i)
            {
                Vector2 p1 = new Vector2(radiusX * (float)Math.Cos(theta), radiusY * (float)Math.Sin(theta));
                Vector2 p2 = new Vector2(radiusX * (float)Math.Cos(theta + segmentSize),
                                         radiusY * (float)Math.Sin(theta + segmentSize));
                // fill vertices
                verticesFill[3 * i].Position = new Vector3(start, 0f);
                verticesFill[3 * i + 1].Position = new Vector3(p1, 0f);
                verticesFill[3 * i + 2].Position = new Vector3(p2, 0f);
                verticesFill[3 * i].TextureCoordinate = start * materialScale;
                verticesFill[3 * i + 1].TextureCoordinate = p1 * materialScale;
                verticesFill[3 * i + 2].TextureCoordinate = p2 * materialScale;
                verticesFill[3 * i].Color = verticesFill[3 * i + 1].Color = verticesFill[3 * i + 2].Color = color;

                // outline vertices
                if (i == 0)
                {
                    verticesOutline[0].Position = new Vector3(start, 0f);
                    verticesOutline[1].Position = new Vector3(p1, 0f);
                    verticesOutline[0].Color = verticesOutline[1].Color = Color.Black;
                }
                if (i == CircleSegments - 3)
                {
                    verticesOutline[2 * CircleSegments - 2].Position = new Vector3(p2, 0f);
                    verticesOutline[2 * CircleSegments - 1].Position = new Vector3(start, 0f);
                    verticesOutline[2 * CircleSegments - 2].Color =
                        verticesOutline[2 * CircleSegments - 1].Color = Color.Black;
                }
                verticesOutline[2 * i + 2].Position = new Vector3(p1, 0f);
                verticesOutline[2 * i + 3].Position = new Vector3(p2, 0f);
                verticesOutline[2 * i + 2].Color = verticesOutline[2 * i + 3].Color = Color.Black;

                theta += segmentSize;
            }

            return RenderTexture((int)(radiusX * 2f), (int)(radiusY * 2f),
                                 _materials[type], verticesFill, verticesOutline);
        }

        private Texture2D RenderTexture(int width, int height, Texture2D material,
                                        VertexPositionColorTexture[] verticesFill,
                                        VertexPositionColor[] verticesOutline)
        {
            List<VertexPositionColorTexture[]> fill = new List<VertexPositionColorTexture[]>(1);
            fill.Add(verticesFill);
            return RenderTexture(width, height, material, fill, verticesOutline);
        }

        private Texture2D RenderTexture(int width, int height, Texture2D material,
                                        List<VertexPositionColorTexture[]> verticesFill,
                                        VertexPositionColor[] verticesOutline)
        {
            Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0f);
            PresentationParameters pp = _device.PresentationParameters;
            RenderTarget2D texture = new RenderTarget2D(_device, width + 2, height + 2, false, SurfaceFormat.Color,
                                                        DepthFormat.None, pp.MultiSampleCount,
                                                        RenderTargetUsage.DiscardContents);
            _device.RasterizerState = RasterizerState.CullNone;
            _device.SamplerStates[0] = SamplerState.LinearWrap;

            _device.SetRenderTarget(texture);
            _device.Clear(Color.Transparent);
            _effect.Projection = Matrix.CreateOrthographic(width + 2f, -height - 2f, 0f, 1f);
            _effect.View = halfPixelOffset;
            // render shape;
            _effect.TextureEnabled = true;
            _effect.Texture = material;
            _effect.VertexColorEnabled = true;
            _effect.Techniques[0].Passes[0].Apply();
            for (int i = 0; i < verticesFill.Count; ++i)
            {
                _device.DrawUserPrimitives(PrimitiveType.TriangleList, verticesFill[i], 0, verticesFill[i].Length / 3);
            }
            // render outline;
            _effect.TextureEnabled = false;
            _effect.Techniques[0].Passes[0].Apply();
            _device.DrawUserPrimitives(PrimitiveType.LineList, verticesOutline, 0, verticesOutline.Length / 2);
            _device.SetRenderTarget(null);
            return texture;
        }
    }
}