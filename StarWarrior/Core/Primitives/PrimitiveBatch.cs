#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrimitiveBatch.cs" company="GAMADU.COM">
//     Copyright © 2013 GAMADU.COM. All rights reserved.
//
//     Redistribution and use in source and binary forms, with or without modification, are
//     permitted provided that the following conditions are met:
//
//        1. Redistributions of source code must retain the above copyright notice, this list of
//           conditions and the following disclaimer.
//
//        2. Redistributions in binary form must reproduce the above copyright notice, this list
//           of conditions and the following disclaimer in the documentation and/or other materials
//           provided with the distribution.
//
//     THIS SOFTWARE IS PROVIDED BY GAMADU.COM 'AS IS' AND ANY EXPRESS OR IMPLIED
//     WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//     FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL GAMADU.COM OR
//     CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//     CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//     SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//     ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//     NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//     ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//     The views and conclusions contained in the software and documentation are those of the
//     authors and should not be interpreted as representing official policies, either expressed
//     or implied, of GAMADU.COM.
// </copyright>
// <summary>
//   The primitive batch.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Primitives
{
    #region Using statements

    using System;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    #endregion

    /// <summary>The primitive batch is a class that handles efficient rendering automatically for its users, in a similar way to SpriteBatch. PrimitiveBatch can render lines, points, and triangles to the screen. In this sample, it is used to draw a space warriors retro scene.</summary>
    public class PrimitiveBatch : IDisposable
    {
        /// <summary>The default buffer size controls how large the vertices buffer is. Larger buffers will require flushing less often, which can increase performance. However, having buffer that is unnecessarily large will waste memory.</summary>
        private const int DefaultBufferSize = 500;

        /// <summary>The basic effect, which contains the shade effect that we will use to draw our primitives.</summary>
        private readonly BasicEffect basicEffect;

        /// <summary>The device that we will issue draw calls to.</summary>
        private readonly GraphicsDevice device;

        /// <summary>The vertices. A block of vertices that calling AddVertex will fill. Flush will draw using this array, and will determine how many primitives to draw from positionInBuffer..</summary>
        private readonly VertexPositionColor[] vertices;

        /// <summary>The has begun. hasBegun is flipped to true once Begin is called, and is used to make sure users don't call End before Begin is called.</summary>
        private bool hasBegun;

        /// <summary>The is disposed.</summary>
        private bool isDisposed;

        /// <summary>The number of vertices per primitive.</summary>
        private int numberOfVerticesPerPrimitive;

        /// <summary>The position in buffer.</summary>
        private int positionInBuffer;

        /// <summary>Initializes a new instance of the <see cref="PrimitiveBatch" /> class.</summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <exception cref="System.ArgumentNullException">The graphicsDevice is not initialized.</exception>
        public PrimitiveBatch(GraphicsDevice graphicsDevice)
        {
            this.vertices = new VertexPositionColor[DefaultBufferSize];
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice");
            }

            this.device = graphicsDevice;

            // Set up a new basic effect.
            this.basicEffect = new BasicEffect(graphicsDevice)
                                   {
                                       // Enable vertex colors.
                                       VertexColorEnabled = true,

                                       // Projection uses CreateOrthographicOffCenter to create 2d projection matrix with 0,0 in the upper left.
                                       Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 0, 1)
                                   };
        }

        /// <summary>Gets the type of the primitive. This value is set by Begin, and is the type of primitives that we are drawing.</summary>
        /// <value>The type of the primitive.</value>
        public PrimitiveType PrimitiveType { get; private set; }

        /// <summary>The add vertex is called to add another vertex to be rendered. To draw a point, AddVertex must be called once. For lines, twice, and for triangles 3 times. This method can only be called once begin has been called. If there is not enough room in the vertices buffer, Flush is called automatically.</summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="System.InvalidOperationException">Begin must be called before AddVertex can be called.</exception>
        public void AddVertex(Vector2 vertex, Color color)
        {
            if (!this.hasBegun)
            {
                throw new InvalidOperationException("Begin must be called before AddVertex can be called.");
            }

            // Are we starting a new primitive? if so, and there will not be enough room for a whole primitive, flush.
            bool newPrimitive = (this.positionInBuffer % this.numberOfVerticesPerPrimitive) == 0;

            if (newPrimitive && (this.positionInBuffer + this.numberOfVerticesPerPrimitive) >= this.vertices.Length)
            {
                this.Flush();
            }

            // Once we know there's enough room, set the vertex in the buffer, and increase position.
            this.vertices[this.positionInBuffer].Position = new Vector3(vertex, 0);
            this.vertices[this.positionInBuffer].Color = color;

            ++this.positionInBuffer;
        }

        /// <summary>The begin.</summary>
        /// <param name="primitiveType">The primitive type.</param>
        /// <exception cref="System.InvalidOperationException">End must be called before Begin can be called again.</exception>
        /// <exception cref="System.NotSupportedException">The specified primitiveType is not supported by PrimitiveBatch.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public void Begin(PrimitiveType primitiveType)
        {
            if (this.hasBegun)
            {
                throw new InvalidOperationException("End must be called before Begin can be called again.");
            }

            // These three types reuse vertices, so we can't flush properly without more complex logic.
            // Since that's a bit too complicated for this sample, we'll simply disallow them.
            switch (primitiveType)
            {
                case PrimitiveType.LineStrip:
                    throw new NotSupportedException("The primitiveType LineStrip is not supported by PrimitiveBatch.");
                case PrimitiveType.TriangleStrip:
                    throw new NotSupportedException("The primitiveType TriangleStrip is not supported by PrimitiveBatch.");
            }

            this.PrimitiveType = primitiveType;

            // How many vertices will each of these primitives require?
            this.numberOfVerticesPerPrimitive = GetNumberOfVerticesPerPrimitive(primitiveType);

            // Tell our basic effect to begin.
            this.basicEffect.CurrentTechnique.Passes[0].Apply();

            // Flip the error checking boolean. It's now ok to call AddVertex, Flush, and End.
            this.hasBegun = true;
        }

        /// <summary>The dispose.</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>The end is called once all the primitives have been drawn using AddVertex. It will call Flush to actually submit the draw call to the graphics card, and then tell the basic effect to end.</summary>
        /// <exception cref="System.InvalidOperationException">Begin must be called before End can be called.</exception>
        public void End()
        {
            if (!this.hasBegun)
            {
                throw new InvalidOperationException("Begin must be called before End can be called.");
            }

            // Draw whatever the user wanted us to draw.
            this.Flush();

            this.hasBegun = false;
        }

        /// <summary>The dispose.</summary>
        /// <param name="disposing">The disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this.isDisposed)
            {
                if (this.basicEffect != null)
                {
                    this.basicEffect.Dispose();
                }

                this.isDisposed = true;
            }
        }

        /// <summary>Gets the number of vertices per primitive.</summary>
        /// <param name="primitive">The primitive.</param>
        /// <returns>The number of vertices per primitive.</returns>
        /// <exception cref="System.InvalidOperationException">Primitive is not valid</exception>
        private static int GetNumberOfVerticesPerPrimitive(PrimitiveType primitive)
        {
            int result;
            switch (primitive)
            {
                case PrimitiveType.LineList:
                    result = 2;
                    break;
                case PrimitiveType.TriangleList:
                    result = 3;
                    break;
                default:
                    throw new InvalidOperationException("Primitive is not valid.");
            }

            return result;
        }

        /// <summary>The flush is called to issue the draw call to the graphics card. Once the draw call is made, positionInBuffer is reset, so that AddVertex can start over at the beginning. End will call this to draw the primitives that the user requested, and AddVertex will call this if there is not enough room in the buffer.</summary>
        /// <exception cref="System.InvalidOperationException">Begin must be called before Flush can be called.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        private void Flush()
        {
            if (!this.hasBegun)
            {
                throw new InvalidOperationException("Begin must be called before Flush can be called.");
            }

            // no work to do
            if (this.positionInBuffer == 0)
            {
                return;
            }

            // how many primitives will we draw?
            int primitiveCount = this.positionInBuffer / this.numberOfVerticesPerPrimitive;

            // submit the draw call to the graphics card
            this.device.DrawUserPrimitives(this.PrimitiveType, this.vertices, 0, primitiveCount);

            // now that we've drawn, it's ok to reset positionInBuffer back to zero,
            // and write over any vertices that may have been set previously.
            this.positionInBuffer = 0;
        }
    }
}