//-----------------------------------------------------------------------------
// Sky.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GeneratedGeometry
{
    /// <summary>
    /// Runtime class for loading and rendering a textured skydome
    /// that was created during the build process by the SkyProcessor.
    /// </summary>
    public class Sky
    {

        /// <summary>
        /// Gets or sets the skydome model.
        /// </summary>
        public Model Model;
        /// <summary>
        /// Gets or sets the texture applied to the skydome.
        /// </summary>
        public Texture2D Texture;



        /// <summary>
        /// Helper for drawing the skydome mesh with the specified view and projection matrices.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public void Draw(Matrix view, Matrix projection)
        {
            /// <summary>
            /// Draws the skydome mesh with the specified view and projection matrices.
            /// </summary>
            /// <param name="view">The view matrix.</param>
            /// <param name="projection">The projection matrix.</param>
            GraphicsDevice GraphicsDevice = Texture.GraphicsDevice;

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            view.Translation = Vector3.Zero;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.View = view;
                    effect.Projection = projection;
                    effect.Texture = Texture;
                    effect.TextureEnabled = true;
                }

                mesh.Draw();
            }
        }
    }
}
