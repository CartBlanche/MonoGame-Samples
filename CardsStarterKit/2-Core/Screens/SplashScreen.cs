//-----------------------------------------------------------------------------
// SplashScreen.cs
//
// Splash screen that displays for 3 seconds on game startup
//-----------------------------------------------------------------------------

using System;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using CardsFramework;

namespace Blackjack
{
    class SplashScreen : GameScreen
    {
        private Texture2D splashTexture;
        private Rectangle safeArea;
        private TimeSpan displayDuration = TimeSpan.FromSeconds(3);
        private TimeSpan elapsedTime = TimeSpan.Zero;
        private bool contentLoaded = false;

        public SplashScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;

            // TODO: Load actual MonoGame splash logo
            // For now, we'll use the blank texture as a placeholder
            try
            {
                splashTexture = content.Load<Texture2D>("Images/blank");
            }
            catch
            {
                // If blank texture doesn't exist, create a simple colored texture
                splashTexture = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
                splashTexture.SetData(new[] { Color.DarkSlateGray });
            }

            safeArea = ScreenManager.SafeArea;
            contentLoaded = true;

            base.LoadContent();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (!contentLoaded || coveredByOtherScreen)
                return;

            // Only count time if we're fully transitioned on
            if (TransitionPosition == 0)
            {
                elapsedTime += gameTime.ElapsedGameTime;

                // After display duration, exit and show main menu
                if (elapsedTime >= displayDuration)
                {
                    // Remove splash screen and add background + main menu
                    ExitScreen();
                    ScreenManager.AddScreen(new BackgroundScreen(), null);
                    ScreenManager.AddScreen(new MainMenuScreen(), null);
                }
            }
        }

        public override void HandleInput(InputState inputState)
        {
            // Allow skipping splash screen with any input
            if (inputState.IsNewKeyPress(Microsoft.Xna.Framework.Input.Keys.Space, ControllingPlayer, out _) ||
                inputState.IsNewKeyPress(Microsoft.Xna.Framework.Input.Keys.Enter, ControllingPlayer, out _) ||
                inputState.IsNewKeyPress(Microsoft.Xna.Framework.Input.Keys.Escape, ControllingPlayer, out _) ||
                inputState.IsNewButtonPress(Microsoft.Xna.Framework.Input.Buttons.A, ControllingPlayer, out _) ||
                inputState.IsNewButtonPress(Microsoft.Xna.Framework.Input.Buttons.Start, ControllingPlayer, out _))
            {
                // Skip to main menu immediately
                ExitScreen();
                ScreenManager.AddScreen(new BackgroundScreen(), null);
                ScreenManager.AddScreen(new MainMenuScreen(), null);
            }

            // Check for mouse/touch input to skip
            if (UIUtility.IsDesktop && inputState.CurrentMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                ExitScreen();
                ScreenManager.AddScreen(new BackgroundScreen(), null);
                ScreenManager.AddScreen(new MainMenuScreen(), null);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!contentLoaded)
                return;

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            Color backgroundColor = Color.Black;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

            // Draw background
            spriteBatch.Draw(splashTexture, safeArea, backgroundColor * TransitionAlpha);

            // Draw placeholder text for now (TODO: replace with actual logo)
            string splashText = "#BuiltWithMonoGame";
            SpriteFont font = ScreenManager.Font;
            Vector2 textSize = font.MeasureString(splashText);
            Vector2 textPosition = new Vector2(
                safeArea.Center.X - textSize.X / 2,
                safeArea.Center.Y - textSize.Y / 2
            );
            spriteBatch.DrawString(font, splashText, textPosition, Color.White * TransitionAlpha);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
