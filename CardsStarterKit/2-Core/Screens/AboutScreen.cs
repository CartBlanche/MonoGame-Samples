//-----------------------------------------------------------------------------
// AboutScreen.cs
//
// About screen displaying game information and credits
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;
using CardsFramework;
using System.IO;
using System.Reflection;

namespace CardsFramework.Core
{
    public abstract class AboutScreenBase : GameScreen
    {
        private Texture2D buttonRegularTexture;
        private Texture2D buttonPressedTexture;
        private Rectangle safeArea;

        private Rectangle backButtonBounds;
        private bool isBackButtonPressed = false;
        private bool isMouseDown = false;

        private Vector2 contentStartPosition;
        private float lineSpacing;
        private float contentScale;

        private readonly string title;

        protected abstract string[] ContentLines { get; }
        protected virtual string TitleText => string.IsNullOrWhiteSpace(title) ? "About" : title;

        protected AboutScreenBase(string title)
        {
            this.title = title;
            EnabledGestures = GestureType.Tap;
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;

            buttonRegularTexture = content.Load<Texture2D>(Path.Combine("Images", "ButtonRegular"));
            buttonPressedTexture = content.Load<Texture2D>(Path.Combine("Images", "ButtonPressed"));

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

            // Calculate content layout and fit scale after safe-area is known.
            BuildLayout();

            base.LoadContent();
        }

        private void BuildLayout()
        {
            float heightScale = safeArea.Height / 720f;
            lineSpacing = 30f * heightScale;

            // Start below the title with side padding.
            contentStartPosition = new Vector2(
                safeArea.Left + 80 * heightScale,
                safeArea.Top + 120 * heightScale
            );

            contentScale = ComputeContentScale(heightScale);
        }

        private float ComputeContentScale(float heightScale)
        {
            if (ContentLines == null || ContentLines.Length == 0)
            {
                return 1f;
            }

            float maxLineWidth = 0f;
            foreach (string line in ContentLines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                float lineWidth = ScreenManager.RegularFont.MeasureString(line).X;
                if (lineWidth > maxLineWidth)
                {
                    maxLineWidth = lineWidth;
                }
            }

            float availableWidth = Math.Max(1f, safeArea.Width - (160f * heightScale));
            float widthScale = maxLineWidth > 0f ? availableWidth / maxLineWidth : 1f;

            float availableHeight = Math.Max(1f, safeArea.Bottom - (contentStartPosition.Y + 20f * heightScale));
            float contentHeight = Math.Max(1f, ContentLines.Length * lineSpacing);
            float heightFitScale = availableHeight / contentHeight;

            float fit = Math.Min(widthScale, heightFitScale);
            return MathHelper.Clamp(Math.Min(1f, fit), 0.60f, 1f);
        }

        protected string GetVersionLine()
        {
            var version = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version;
            return version == null ? "Version" : $"Version {version.ToString(3)}";
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

            // Draw title
            Vector2 titleSize = ScreenManager.Font.MeasureString(TitleText);
            Vector2 titlePos = new Vector2(safeArea.Center.X - titleSize.X / 2, safeArea.Top + 10);
            spriteBatch.DrawString(ScreenManager.Font, TitleText, titlePos, Color.White * TransitionAlpha);

            // Draw content lines
            Vector2 linePos = contentStartPosition;
            foreach (string line in ContentLines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    spriteBatch.DrawString(
                        ScreenManager.RegularFont,
                        line,
                        linePos,
                        Color.White * TransitionAlpha,
                        0f,
                        Vector2.Zero,
                        contentScale,
                        SpriteEffects.None,
                        0f);
                }

                linePos.Y += lineSpacing * contentScale;
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

    public class AboutScreen : AboutScreenBase
    {
        public AboutScreen() : this(null) { }

        public AboutScreen(string title) : base(title)
        {
        }

        protected override string[] ContentLines => new[]
        {
            "Cards Framework",
            "",
            GetVersionLine(),
            "",
            "Built with MonoGame",
            "www.monogame.net"
        };
    }
}
