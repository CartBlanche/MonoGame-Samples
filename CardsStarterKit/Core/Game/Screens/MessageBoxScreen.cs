//----------------------------------------------------------------------------- 
// MessageBoxScreen.cs
// Adapted from NetworkStateManagement sample for Blackjack
// Shows a simple message box with OK button
//-----------------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;
using GameStateManagement;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    /// <summary>
    /// A popup screen that displays a message and waits for the user to acknowledge.
    /// </summary>
    class MessageBoxScreen : GameScreen
    {
        string message;
        MenuEntry okMenuEntry;
        public event EventHandler<PlayerIndexEventArgs> Accepted;

        public MessageBoxScreen(string message)
        {
            this.message = message;
            IsPopup = true;
        }

        public override void LoadContent()
        {
            okMenuEntry = new MenuEntry("OK");
            okMenuEntry.Selected += OkMenuEntrySelected;
            //MenuEntries.Add(okMenuEntry);
            base.LoadContent();
        }

        void OkMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            Accepted?.Invoke(this, e);
            ExitScreen();
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.FadeBackBufferToBlack(0.5f);
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;
            Vector2 viewportSize = new Vector2(
                ScreenManager.GraphicsDevice.Viewport.Width,
                ScreenManager.GraphicsDevice.Viewport.Height);
            Vector2 textSize = font.MeasureString(message);
            Vector2 position = (viewportSize - textSize) / 2;
            spriteBatch.Begin();
            spriteBatch.DrawString(font, message, position, Color.White);
            spriteBatch.End();
        }
    }
}
