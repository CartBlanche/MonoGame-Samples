#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Explosion.cs" company="GAMADU.COM">
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
//   The explosion.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Spatials
{
    #region Using statements

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;

    using StarWarrior.Components;

    #endregion

    /// <summary>The explosion.</summary>
    internal static class Explosion
    {
        /// <summary>The circle.</summary>
        private static Texture2D circle;

        /// <summary>The render.</summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="contentManager">The content manager.</param>
        /// <param name="transformComponent">The TransformComponent.</param>
        /// <param name="color">The color.</param>
        /// <param name="radius">The radius.</param>
        public static void Render(SpriteBatch spriteBatch, ContentManager contentManager, TransformComponent transformComponent, Color color, int radius)
        {
            if (circle == null)
            {
                circle = contentManager.Load<Texture2D>("explosion");
            }

            spriteBatch.Draw(circle, new Vector2(transformComponent.X - radius, transformComponent.Y - radius), null, Color.White, 0, Vector2.Zero, 0.3f, SpriteEffects.None, 0);
        }
    }
}