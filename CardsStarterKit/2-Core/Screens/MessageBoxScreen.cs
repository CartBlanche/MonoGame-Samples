//----------------------------------------------------------------------------- 
// MessageBoxScreen.cs
// Adapted from NetworkStateManagement sample for Blackjack
// Shows a simple message box with OK button
//-----------------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;

using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework.Core
{
    /// <summary>
    /// A popup screen that displays a message and waits for the user to acknowledge.
    /// </summary>
    public class MessageBoxScreen : GameScreen
    {
        string message;
        MenuEntry okMenuEntry;
        public event EventHandler<PlayerIndexEventArgs> Accepted;

        readonly string okText;

        public MessageBoxScreen(string message, string okText = "OK")
        {
            this.message = message;
            this.okText = okText;
            IsPopup = true;
        }

        public override void LoadContent()
        {
            okMenuEntry = new MenuEntry(okText);
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
                ScreenManager.BaseScreenSize.X,
                ScreenManager.BaseScreenSize.Y);
            Vector2 textSize = font.MeasureString(message);
            Vector2 position = (viewportSize - textSize) / 2;
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
            spriteBatch.DrawString(font, message, position, Color.White);
            spriteBatch.End();
        }
    }
}