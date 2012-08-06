using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    /// <summary>
    /// A popup message box screen, used to display "are you sure?"
    /// confirmation messages.
    /// </summary>
    public class MessageBoxScreen : GameScreen
    {
        private Rectangle _backgroundRectangle;
        private Texture2D _gradientTexture;
        private string _message;
        private Vector2 _textPosition;

        public MessageBoxScreen(string message)
        {
            _message = message;

            IsPopup = true;
            HasCursor = true;

            TransitionOnTime = TimeSpan.FromSeconds(0.4);
            TransitionOffTime = TimeSpan.FromSeconds(0.4);
        }

        /// <summary>
        /// Loads graphics content for this screen. This uses the shared ContentManager
        /// provided by the Game class, so the content will remain loaded forever.
        /// Whenever a subsequent MessageBoxScreen tries to load this same content,
        /// it will just get back another reference to the already loaded data.
        /// </summary>
        public override void LoadContent()
        {
            SpriteFont font = ScreenManager.Fonts.DetailsFont;
            ContentManager content = ScreenManager.Game.Content;
            _gradientTexture = content.Load<Texture2D>("Common/popup");

            // Center the message text in the viewport.
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            Vector2 viewportSize = new Vector2(viewport.Width, viewport.Height);
            Vector2 textSize = font.MeasureString(_message);
            _textPosition = (viewportSize - textSize) / 2;

            // The background includes a border somewhat larger than the text itself.
            const int hPad = 32;
            const int vPad = 16;

            _backgroundRectangle = new Rectangle((int)_textPosition.X - hPad,
                                                 (int)_textPosition.Y - vPad,
                                                 (int)textSize.X + hPad * 2,
                                                 (int)textSize.Y + vPad * 2);
        }

        /// <summary>
        /// Responds to user input, accepting or cancelling the message box.
        /// </summary>
        public override void HandleInput(InputHelper input, GameTime gameTime)
        {
            if (input.IsMenuSelect() || input.IsMenuCancel() ||
                input.IsNewMouseButtonPress(MouseButtons.LeftButton))
            {
                ExitScreen();
            }
        }

        /// <summary>
        /// Draws the message box.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Fonts.DetailsFont;

            // Fade the popup alpha during transitions.
            Color color = Color.White * TransitionAlpha * (2f / 3f);

            spriteBatch.Begin();

            // Draw the background rectangle.
            spriteBatch.Draw(_gradientTexture, _backgroundRectangle, color);

            // Draw the message box text.
            spriteBatch.DrawString(font, _message, _textPosition + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, _message, _textPosition, Color.White);

            spriteBatch.End();
        }
    }
}