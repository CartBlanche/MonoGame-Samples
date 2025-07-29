#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrianglesStrip.cs" company="GAMADU.COM">
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
//   The triangles strip.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Primitives
{
    #region Using statements

    using System;
    using System.Collections.Generic;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    #endregion

    /// <summary>The triangles strip.</summary>
    public class TrianglesStrip
    {
        /// <summary>The batch.</summary>
        private readonly PrimitiveBatch batch;

        /// <summary>The device.</summary>
        private readonly GraphicsDevice device;

        /// <summary>The point.</summary>
        private readonly List<Vector2> point;

        /// <summary>The state.</summary>
        private readonly RasterizerState state;

        /// <summary>Initializes a new instance of the <see cref="TrianglesStrip" /> class.</summary>
        /// <param name="device">The device.</param>
        /// <param name="primitiveBatch">The primitive batch.</param>
        public TrianglesStrip(GraphicsDevice device, PrimitiveBatch primitiveBatch)
        {
            this.IsFilled = true;
            this.Color = Color.White;
            this.point = new List<Vector2>();
            this.device = device;
            this.batch = primitiveBatch;
            this.state = new RasterizerState
                             {
                                 CullMode = CullMode.CullCounterClockwiseFace,
                                 FillMode = FillMode.WireFrame
                             };
        }

        /// <summary>Gets or sets the color.</summary>
        /// <value>The color.</value>
        private Color Color { get; set; }

        /// <summary>Gets or sets a value indicating whether this instance is filled.</summary>
        /// <value><see langword="true" /> if this instance is filled; otherwise, <see langword="false" />.</value>
        private bool IsFilled { get; set; }

        /// <summary>The add point.</summary>
        /// <param name="x1">The x 1.</param>
        /// <param name="y1">The y 1.</param>
        public void AddPoint(float x1, float y1)
        {
            this.point.Add(new Vector2(x1, y1));
        }

        /// <summary>The draw.</summary>
        /// <param name="transform">The transform.</param>
        /// <exception cref="System.InvalidOperationException">Need at least 3 points in order to draw the triangles.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void Draw(Vector2 transform)
        {
            if (this.IsFilled == false)
            {
                this.device.RasterizerState = this.state;
            }

            if (this.point.Count < 3)
            {
                throw new InvalidOperationException("Need at least 3 points in order to draw the triangles.");
            }

            this.batch.Begin(PrimitiveType.TriangleStrip);
            foreach (Vector2 item in this.point)
            {
                this.batch.AddVertex(item + transform, this.Color);
            }

            this.batch.End();

            if (this.IsFilled == false)
            {
                this.device.RasterizerState = RasterizerState.CullCounterClockwise;
            }
        }
    }
}