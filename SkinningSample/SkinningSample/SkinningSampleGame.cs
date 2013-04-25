using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkinningSample.ContentTypes;
using System;

namespace SkinningSample
{
    public class SkinningSampleGame
    {
        #region Fields
        Game game;

        KeyboardState currentKeyboardState = new KeyboardState();
#if !WINDOWS_PHONE
        GamePadState currentGamePadState = new GamePadState();
#endif

        Model currentModel;
        AnimationPlayer animationPlayer;

        float cameraArc = 0;
        float cameraRotation = 0;
        float cameraDistance = 100;

        #endregion

        public SkinningSampleGame(Game game, GraphicsDeviceManager graphics)
        {
            this.game = game;

#if WINDOWS_PHONE
            // Frame rate is 30 fps by default for Windows Phone.
            game.TargetElapsedTime = TimeSpan.FromTicks(333333);

            graphics.IsFullScreen = true;            
#endif
        }

        /// <summary>
        /// Load your graphics content.
        /// </summary>
        public void LoadContent()
        {
            // Load the model.
            currentModel = game.Content.Load<Model>("dude");

            // Look up our custom skinning information.
            SkinningData skinningData = currentModel.Tag as SkinningData;

            if (skinningData == null)
                throw new InvalidOperationException
                    ("This model does not contain a SkinningData tag.");

            // Create an animation player, and start decoding an animation clip.
            animationPlayer = new AnimationPlayer(skinningData);

            AnimationClip clip = skinningData.AnimationClips["Take 001"];

            animationPlayer.StartClip(clip);
        }

        #region Update and Draw


        /// <summary>
        /// Allows the game to run logic.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            HandleInput();

            UpdateCamera(gameTime);

            animationPlayer.Update(gameTime.ElapsedGameTime, true, Matrix.Identity);

        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            GraphicsDevice device = game.GraphicsDevice;

            device.Clear(Color.CornflowerBlue);

            Matrix[] bones = animationPlayer.GetSkinTransforms();

            // Compute camera matrices.
            Matrix view = Matrix.CreateTranslation(0, -40, 0) *
                          Matrix.CreateRotationY(MathHelper.ToRadians(cameraRotation)) *
                          Matrix.CreateRotationX(MathHelper.ToRadians(cameraArc)) *
                          Matrix.CreateLookAt(new Vector3(0, 0, -cameraDistance),
                                              new Vector3(0, 0, 0), Vector3.Up);

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                                                                    device.Viewport.AspectRatio,
                                                                    1,
                                                                    10000);

            // Render the skinned mesh.
            foreach (ModelMesh mesh in currentModel.Meshes)
            {
                foreach (SkinnedEffect effect in mesh.Effects)
                {
                    effect.SetBoneTransforms(bones);

                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();

                    effect.SpecularColor = new Vector3(0.25f);
                    effect.SpecularPower = 16;
                }

                mesh.Draw();
            }

        }

        #endregion

        #region Handle Input


        /// <summary>
        /// Handles input for quitting the game.
        /// </summary>
        private void HandleInput()
        {
            currentKeyboardState = Keyboard.GetState();
#if !WINDOWS_PHONE
            currentGamePadState = GamePad.GetState(PlayerIndex.One);
#endif
            // Check for exit.
            if (currentKeyboardState.IsKeyDown(Keys.Escape)
#if !WINDOWS_PHONE
                || currentGamePadState.Buttons.Back == ButtonState.Pressed
#endif
                )
            {
                game.Exit();
            }
        }


        /// <summary>
        /// Handles camera input.
        /// </summary>
        private void UpdateCamera(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // Check for input to rotate the camera up and down around the model.
            if (currentKeyboardState.IsKeyDown(Keys.Up) ||
                currentKeyboardState.IsKeyDown(Keys.W))
            {
                cameraArc += time * 0.1f;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Down) ||
                currentKeyboardState.IsKeyDown(Keys.S))
            {
                cameraArc -= time * 0.1f;
            }
#if !WINDOWS_PHONE
            cameraArc += currentGamePadState.ThumbSticks.Right.Y * time * 0.25f;
#endif

            // Limit the arc movement.
            if (cameraArc > 90.0f)
                cameraArc = 90.0f;
            else if (cameraArc < -90.0f)
                cameraArc = -90.0f;

            // Check for input to rotate the camera around the model.
            if (currentKeyboardState.IsKeyDown(Keys.Right) ||
                currentKeyboardState.IsKeyDown(Keys.D))
            {
                cameraRotation += time * 0.1f;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Left) ||
                currentKeyboardState.IsKeyDown(Keys.A))
            {
                cameraRotation -= time * 0.1f;
            }

#if !WINDOWS_PHONE
            cameraRotation += currentGamePadState.ThumbSticks.Right.X * time * 0.25f;
#endif

            // Check for input to zoom camera in and out.
            if (currentKeyboardState.IsKeyDown(Keys.Z))
                cameraDistance += time * 0.25f;

            if (currentKeyboardState.IsKeyDown(Keys.X))
                cameraDistance -= time * 0.25f;

#if !WINDOWS_PHONE
            cameraDistance += currentGamePadState.Triggers.Left * time * 0.5f;
            cameraDistance -= currentGamePadState.Triggers.Right * time * 0.5f;
#endif

            // Limit the camera distance.
            if (cameraDistance > 500.0f)
                cameraDistance = 500.0f;
            else if (cameraDistance < 10.0f)
                cameraDistance = 10.0f;

            if (
#if !WINDOWS_PHONE
            currentGamePadState.Buttons.RightStick == ButtonState.Pressed ||
#endif
            currentKeyboardState.IsKeyDown(Keys.R))
            {
                cameraArc = 0;
                cameraRotation = 0;
                cameraDistance = 100;
            }
        }


        #endregion


    }
}
