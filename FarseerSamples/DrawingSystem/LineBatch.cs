using System;
using FarseerPhysics.Collision.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    public class LineBatch : IDisposable
    {
        private const int DefaultBufferSize = 500;

        // a basic effect, which contains the shaders that we will use to draw our
        // primitives.
        private BasicEffect _basicEffect;

        // the device that we will issue draw calls to.
        private GraphicsDevice _device;

        // hasBegun is flipped to true once Begin is called, and is used to make
        // sure users don't call End before Begin is called.
        private bool _hasBegun;

        private bool _isDisposed;
        private VertexPositionColor[] _lineVertices;
        private int _lineVertsCount;

        public LineBatch(GraphicsDevice graphicsDevice)
            : this(graphicsDevice, DefaultBufferSize)
        {
        }

        public LineBatch(GraphicsDevice graphicsDevice, int bufferSize)
        {
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice");
            }
            _device = graphicsDevice;

            _lineVertices = new VertexPositionColor[bufferSize - bufferSize % 2];

            // set up a new basic effect, and enable vertex colors.
            _basicEffect = new BasicEffect(graphicsDevice);
            _basicEffect.VertexColorEnabled = true;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                if (_basicEffect != null)
                    _basicEffect.Dispose();

                _isDisposed = true;
            }
        }

        public void Begin(Matrix projection, Matrix view)
        {
            if (_hasBegun)
            {
                throw new InvalidOperationException("End must be called before Begin can be called again.");
            }

            _device.SamplerStates[0] = SamplerState.AnisotropicClamp;
            //tell our basic effect to begin.
            _basicEffect.Projection = projection;
            _basicEffect.View = view;
            _basicEffect.CurrentTechnique.Passes[0].Apply();

            // flip the error checking boolean. It's now ok to call DrawLineShape, Flush,
            // and End.
            _hasBegun = true;
        }

        public void DrawLineShape(Shape shape)
        {
            DrawLineShape(shape, Color.Black);
        }

        public void DrawLineShape(Shape shape, Color color)
        {
            if (!_hasBegun)
            {
                throw new InvalidOperationException("Begin must be called before DrawLineShape can be called.");
            }
            if (shape.ShapeType != ShapeType.Edge &&
                shape.ShapeType != ShapeType.Loop)
            {
                throw new NotSupportedException("The specified shapeType is not supported by LineBatch.");
            }
            if (shape.ShapeType == ShapeType.Edge)
            {
                if (_lineVertsCount >= _lineVertices.Length)
                {
                    Flush();
                }
                EdgeShape edge = (EdgeShape)shape;
                _lineVertices[_lineVertsCount].Position = new Vector3(edge.Vertex1, 0f);
                _lineVertices[_lineVertsCount + 1].Position = new Vector3(edge.Vertex2, 0f);
                _lineVertices[_lineVertsCount].Color = _lineVertices[_lineVertsCount + 1].Color = color;
                _lineVertsCount += 2;
            }
            else if (shape.ShapeType == ShapeType.Loop)
            {
                LoopShape loop = (LoopShape)shape;
                for (int i = 0; i < loop.Vertices.Count; ++i)
                {
                    if (_lineVertsCount >= _lineVertices.Length)
                    {
                        Flush();
                    }
                    _lineVertices[_lineVertsCount].Position = new Vector3(loop.Vertices[i], 0f);
                    _lineVertices[_lineVertsCount + 1].Position = new Vector3(loop.Vertices.NextVertex(i), 0f);
                    _lineVertices[_lineVertsCount].Color = _lineVertices[_lineVertsCount + 1].Color = color;
                    _lineVertsCount += 2;
                }
            }
        }

        public void DrawLine(Vector2 v1, Vector2 v2)
        {
            DrawLine(v1, v2, Color.Black);
        }

        public void DrawLine(Vector2 v1, Vector2 v2, Color color)
        {
            if (!_hasBegun)
            {
                throw new InvalidOperationException("Begin must be called before DrawLineShape can be called.");
            }
            if (_lineVertsCount >= _lineVertices.Length)
            {
                Flush();
            }
            _lineVertices[_lineVertsCount].Position = new Vector3(v1, 0f);
            _lineVertices[_lineVertsCount + 1].Position = new Vector3(v2, 0f);
            _lineVertices[_lineVertsCount].Color = _lineVertices[_lineVertsCount + 1].Color = color;
            _lineVertsCount += 2;
        }

        // End is called once all the primitives have been drawn using AddVertex.
        // it will call Flush to actually submit the draw call to the graphics card, and
        // then tell the basic effect to end.
        public void End()
        {
            if (!_hasBegun)
            {
                throw new InvalidOperationException("Begin must be called before End can be called.");
            }

            // Draw whatever the user wanted us to draw
            Flush();

            _hasBegun = false;
        }

        private void Flush()
        {
            if (!_hasBegun)
            {
                throw new InvalidOperationException("Begin must be called before Flush can be called.");
            }
            if (_lineVertsCount >= 2)
            {
                int primitiveCount = _lineVertsCount / 2;
                // submit the draw call to the graphics card
                _device.DrawUserPrimitives(PrimitiveType.LineList, _lineVertices, 0, primitiveCount);
                _lineVertsCount -= primitiveCount * 2;
            }
        }
    }
}