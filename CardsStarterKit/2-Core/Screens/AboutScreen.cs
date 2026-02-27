//-----------------------------------------------------------------------------
// AboutScreen.cs
//
// About screen displaying game information and credits
//-----------------------------------------------------------------------------

using System;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;
using CardsFramework;

namespace Blackjack
{
    class AboutScreen : GameScreen
    {
        private Texture2D background;
        private Texture2D buttonRegularTexture;
        private Texture2D buttonPressedTexture;
        private Rectangle safeArea;

        private Rectangle backButtonBounds;
        private bool isBackButtonPressed = false;
        private bool isMouseDown = false;

        private string[] contentLines;
        private Vector2 contentStartPosition;
        private float lineSpacing;

        public AboutScreen()
        {
            // About screen needs Tap gestures for mobile
            EnabledGestures = GestureType.Tap;

            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;

            background = content.Load<Texture2D>("Images/UI/table");
            buttonRegularTexture = content.Load<Texture2D>("Images/ButtonRegular");
            buttonPressedTexture = content.Load<Texture2D>("Images/ButtonPressed");

            safeArea = ScreenManager.SafeArea;

            // Calculate proportional spacing based on screen height
            float heightScale = safeArea.Height / 720f;
            lineSpacing = 30f * heightScale;

            // Calculate back button bounds in top-left corner
            int backButtonSize = (int)(50 * heightScale);
            int backButtonPadding = (int)(20 * heightScale);
            backButtonBounds = new Rectangle(
                safeArea.Left + backButtonPadding,
                safeArea.Top + backButtonPadding,
                backButtonSize,
                backButtonSize);

            // Build content lines
            BuildContent();

            base.LoadContent();
        }

        private void BuildContent()
        {
            // Build the about content
            contentLines = new string[]
            {
                "MonoGame Blackjack",
                "",
                "Version 1.0",
                "",
                "Based on Microsoft XNA Card Game Starter Kit",
                "Modernized for MonoGame",
                "",
                "Features:",
                "- Cross-platform gameplay",
                "- Multiple languages supported",
                "- Customizable settings",
                "- NPC opponents",
                "",
                "Thanks:",
                "- Pixabay for Jazz Music, CardRemoval and Winning Sound effects",
                "",
                "Built with MonoGame",
                "www.monogame.net"
            };

            // Calculate starting position for content (centered below title)
            float heightScale = safeArea.Height / 720f;
            contentStartPosition = new Vector2(
                safeArea.Left + 80 * heightScale,
                safeArea.Top + 120 * heightScale
            );
        }

        public override void HandleInput(InputState inputState)
        {
            // Cancel the about screen if the user presses the back button
            PlayerIndex player;
            if (inputState.IsNewButtonPress(Buttons.Back, ControllingPlayer, out player))
            {
                ExitScreen();
                return;
            }

            // Check for escape key
            if (inputState.IsNewKeyPress(Keys.Escape, ControllingPlayer, out player))
            {
                ExitScreen();
                return;
            }

            // Get transformed cursor position
            Vector2 cursorPos = inputState.CurrentCursorLocation;
            Point mousePos = new Point((int)cursorPos.X, (int)cursorPos.Y);

            // Reset button state
            isBackButtonPressed = false;

            if (UIUtility.IsDesktop)
            {
                // Handle mouse input - detect click (press + release)
                if (inputState.CurrentMouseState.LeftButton == ButtonState.Released)
                {
                    if (isMouseDown && backButtonBounds.Contains(mousePos))
                    {
                        isBackButtonPressed = true;
                        AudioManager.PlaySound("menu_select");
                        ExitScreen();
                    }
                    isMouseDown = false;
                }
                else if (inputState.CurrentMouseState.LeftButton == ButtonState.Pressed)
                {
                    isMouseDown = true;
                }
            }
            else if (UIUtility.IsMobile)
            {
                // Handle touch input with gestures (like SettingsScreen does)
                foreach (GestureSample gesture in inputState.Gestures)
                {
                    if (gesture.GestureType == GestureType.Tap)
                    {
                        Point tapLocation = new Point((int)gesture.Position.X, (int)gesture.Position.Y);
                        if (backButtonBounds.Contains(tapLocation))
                        {
                            isBackButtonPressed = true;
                            AudioManager.PlaySound("menu_select");
                            ExitScreen();
                        }
                    }
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

            // Draw background
            spriteBatch.Draw(background, safeArea, Color.White * TransitionAlpha);

            // Draw title
            string title = "About";
            Vector2 titleSize = ScreenManager.Font.MeasureString(title);
            Vector2 titlePos = new Vector2(safeArea.Center.X - titleSize.X / 2, safeArea.Top + 10);
            spriteBatch.DrawString(ScreenManager.Font, title, titlePos, Color.White * TransitionAlpha);

            // Draw content lines
            Vector2 linePos = contentStartPosition;
            foreach (string line in contentLines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    spriteBatch.DrawString(ScreenManager.RegularFont, line, linePos, Color.White * TransitionAlpha);
                }
                linePos.Y += lineSpacing;
            }

            // Draw back button
            Texture2D backTexture = isBackButtonPressed ? buttonPressedTexture : buttonRegularTexture;
            spriteBatch.Draw(backTexture, backButtonBounds, Color.Red * TransitionAlpha);
            string backText = "X";
            Vector2 backTextSize = ScreenManager.Font.MeasureString(backText);
            Vector2 backTextPos = new Vector2(
                backButtonBounds.X + (backButtonBounds.Width - backTextSize.X) / 2,
                backButtonBounds.Y + (backButtonBounds.Height - backTextSize.Y) / 2);
            spriteBatch.DrawString(ScreenManager.Font, backText, backTextPos, Color.White * TransitionAlpha);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
