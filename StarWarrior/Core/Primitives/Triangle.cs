#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Triangle.cs" company="GAMADU.COM">
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
//   The triangle.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Primitives
{
    #region Using statements

    using System.Collections.Generic;

    using Microsoft.Xna.Framework;

    #endregion

    /// <summary>The triangle.</summary>
    public class Triangle
    {
        /// <summary>The batch.</summary>
        private readonly PrimitiveBatch batch;

        /// <summary>The points.</summary>
        private readonly List<Vector2> points;

        /// <summary>Initializes a new instance of the <see cref="Triangle" /> class.</summary>
        /// <param name="primitiveBatch">The primitive batch.</param>
        public Triangle(PrimitiveBatch primitiveBatch)
        {
            this.Color = Color.White;
            this.points = new List<Vector2>();
            this.batch = primitiveBatch;
        }

        /// <summary>Gets or sets the color.</summary>
        /// <value>The color.</value>
        public Color Color { get; set; }

        /// <summary>The add triangle.</summary>
        /// <param name="x1">The x 1.</param>
        /// <param name="y1">The y 1.</param>
        /// <param name="x2">The x 2.</param>
        /// <param name="y2">The y 2.</param>
        /// <param name="x3">The x 3.</param>
        /// <param name="y3">The y 3.</param>
        public void AddTriangle(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            this.points.Add(new Vector2(x1, y1));
            this.points.Add(new Vector2(x2, y2));
            this.points.Add(new Vector2(x3, y3));
        }

        /// <summary>Draws the specified transform.</summary>
        /// <param name="transform">The transform.</param>
        public void Draw(Vector2 transform)
        {
            foreach (Vector2 point in this.points)
            {
                this.batch.AddVertex(point + transform, this.Color);
            }
        }
    }
}