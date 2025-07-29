#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Lines.cs" company="GAMADU.COM">
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
//   The lines.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Primitives
{
    #region Using statements

    using System.Collections.Generic;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    #endregion

    /// <summary>The lines.</summary>
    public class Lines
    {
        /// <summary>The batch.</summary>
        private readonly PrimitiveBatch batch;

        /// <summary>The lines.</summary>
        private readonly List<Vector2> lines;

        /// <summary>Initializes a new instance of the <see cref="Lines" /> class.</summary>
        /// <param name="primitiveBatch">The primitive batch.</param>
        public Lines(PrimitiveBatch primitiveBatch)
        {
            this.Color = Color.White;
            this.lines = new List<Vector2>();
            this.batch = primitiveBatch;
        }

        /// <summary>Gets or sets the color.</summary>
        /// <value>The color.</value>
        private Color Color { get; set; }

        /// <summary>The add line.</summary>
        /// <param name="x1">The x 1.</param>
        /// <param name="y1">The y 1.</param>
        /// <param name="x2">The x 2.</param>
        /// <param name="y2">The y 2.</param>
        public void AddLine(float x1, float y1, float x2, float y2)
        {
            this.lines.Add(new Vector2(x1, y1));
            this.lines.Add(new Vector2(x2, y2));
        }

        /// <summary>The draw.</summary>
        /// <param name="position">The position.</param>
        public void Draw(Vector2 position)
        {
            this.batch.Begin(PrimitiveType.LineList);
            foreach (Vector2 item in this.lines)
            {
                this.batch.AddVertex(item + position, this.Color);
            }

            this.batch.End();
        }
    }
}