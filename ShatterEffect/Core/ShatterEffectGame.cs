//-----------------------------------------------------------------------------
// ShatterEffectGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShatterEffect
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class ShatterEffectGame : Game
    {
        GraphicsDeviceManager graphics;

        Vector3 lightPosition = Vector3.UnitY;
        Vector4 ambientColor = Color.DarkGray.ToVector4();
        Vector4 diffuseColor = Color.White.ToVector4();
        Vector4 specularColor = Color.White.ToVector4();
        float specularPower = 50;

        float time;

        const float translationRate = 50;
        const float rotationRate = MathHelper.Pi * 3;
        const float duration = 2.0f;

        Model model;
        SpriteFont font;
        SpriteBatch spriteBatch;

        Matrix view;
        Matrix projection;
        Vector3 cameraPosition = new Vector3(-696, 429, 835);
        Vector3 targetPosition = new Vector3(0, 60, 0);

        int shatterEffectIndex = 0; // 0 for default shatter, 1 for explosion
        bool autoShatter = false;
        bool autoShatterReversing = false;
        float autoShatterSpeed = 0.5f; // Slow-motion multiplier for auto-shatter
        float autoShatterPauseDuration = 1.0f; // Pause duration in seconds before changing direction
        float autoShatterPauseTimer = 0.0f; // Timer to track pause duration
        KeyboardState previousKeyboardState;

        public ShatterEffectGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(this.graphics.GraphicsDevice);

            font = Content.Load<SpriteFont>("font");
            model = Content.Load<Model>("model/ship1");

            // Calculate View/Projection Matrices.
            view = Matrix.CreateLookAt(cameraPosition, targetPosition, Vector3.Up);
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45.0f), GraphicsDevice.Viewport.AspectRatio, 1.0f, 10000.0f);
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Handle input
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

            // Toggle between shatter effects with Tab (on key release)
            if (keyboardState.IsKeyUp(Keys.Tab) && previousKeyboardState.IsKeyDown(Keys.Tab))
            {
                shatterEffectIndex = (shatterEffectIndex + 1) % 2; // Toggle between 0 and 1
            }

            // Toggle auto-shatter with Enter (on key release)
            if (keyboardState.IsKeyUp(Keys.Enter) && previousKeyboardState.IsKeyDown(Keys.Enter))
            {
                autoShatter = !autoShatter;
                autoShatterReversing = false; // Reset reversing state
            }

            if (autoShatter)
            {
                float adjustedElapsedTime = elapsedTime * autoShatterSpeed; // Apply slow-motion multiplier

                if (autoShatterPauseTimer > 0.0f)
                {
                    autoShatterPauseTimer -= elapsedTime; // Decrease pause timer
                }
                else if (!autoShatterReversing)
                {
                    time += adjustedElapsedTime;
                    if (time >= duration)
                    {
                        time = duration;
                        autoShatterReversing = true;
                        autoShatterPauseTimer = autoShatterPauseDuration; // Start pause timer
                    }
                }
                else
                {
                    time -= adjustedElapsedTime;
                    if (time <= 0.0f)
                    {
                        time = 0.0f;
                        autoShatterReversing = false;
                        autoShatterPauseTimer = autoShatterPauseDuration; // Start pause timer
                    }
                }
            }
            else
            {
                // Manual control for shatter effects
                if (keyboardState.IsKeyDown(Keys.Up) || gamePadState.Buttons.A == ButtonState.Pressed)
                {
                    time = Math.Min(duration, time + elapsedTime);
                }

                if (keyboardState.IsKeyDown(Keys.Down) || gamePadState.Buttons.B == ButtonState.Pressed)
                {
                    time = Math.Max(0.0f, time - elapsedTime);
                }
            }

            previousKeyboardState = keyboardState; // Update previous keyboard state

            base.Update(gameTime);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.MonoGameOrange);

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    SetupEffect(transforms, mesh, part);
                }
                mesh.Draw();
            }

            // Draw the UI
            spriteBatch.Begin();
            spriteBatch.DrawString(font, "Shatter Effect Demo", new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(font, "Press Tab to toggle shatter effects", new Vector2(10, 30), Color.White);
            spriteBatch.DrawString(font, "Press Enter to toggle auto-shatter", new Vector2(10, 50), Color.White);
            spriteBatch.DrawString(font, "Use Up/Down arrows or GamePad A/B to control shatter amount", new Vector2(10, 70), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void SetupEffect(Matrix[] transforms, ModelMesh mesh, ModelMeshPart part)
        {
            Effect effect = part.Effect;

            if (shatterEffectIndex == 0) // Default shatter effect
            {
                effect.Parameters["TranslationAmount"].SetValue(translationRate * time);
                effect.Parameters["RotationAmount"].SetValue(rotationRate * time);
            }
            else if (shatterEffectIndex == 1) // Explosion effect
            {
                effect.Parameters["TranslationAmount"].SetValue(translationRate * time * 2); // Expand outward
                effect.Parameters["RotationAmount"].SetValue(0); // No rotation for explosion
            }

            effect.Parameters["time"].SetValue(time);
            effect.Parameters["WorldViewProjection"].SetValue(
                transforms[mesh.ParentBone.Index] * view * projection);
            effect.Parameters["World"].SetValue(transforms[mesh.ParentBone.Index]);
            effect.Parameters["eyePosition"].SetValue(cameraPosition);
            effect.Parameters["lightPosition"].SetValue(lightPosition);
            effect.Parameters["ambientColor"].SetValue(ambientColor);
            effect.Parameters["diffuseColor"].SetValue(diffuseColor);
            effect.Parameters["specularColor"].SetValue(specularColor);
            effect.Parameters["specularPower"].SetValue(specularPower);
        }
    }
}