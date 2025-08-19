
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace NetRumble
{
    // Modern async/await version using Task<T>
    class NetworkBusyScreen<T> : GameScreen
    {
        readonly Task<T> task;
        readonly CancellationTokenSource cts;
        bool completionRaised;
        Texture2D busyTexture;
        string message;

        event EventHandler<OperationCompletedEventArgs> operationCompleted;
        public event EventHandler<OperationCompletedEventArgs> OperationCompleted
        {
            add { operationCompleted += value; }
            remove { operationCompleted -= value; }
        }

        public NetworkBusyScreen(string message, Task<T> task)
        {
            this.message = message;
            this.task = task;
            this.cts = null;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.1);
            TransitionOffTime = TimeSpan.FromSeconds(0.2);
        }

        public NetworkBusyScreen(string message, Task<T> task, CancellationTokenSource cts)
        {
            this.message = message;
            this.task = task;
            this.cts = cts;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.1);
            TransitionOffTime = TimeSpan.FromSeconds(0.2);
        }

        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;
            busyTexture = content.Load<Texture2D>("Textures/chatTalking");
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Optional: allow user to cancel (Esc or B)
            if (!completionRaised && cts != null)
            {
                var kb = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                if (kb.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                {
                    cts.Cancel();
                }
                var gp = Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
                if (gp.IsConnected && gp.IsButtonDown(Microsoft.Xna.Framework.Input.Buttons.B))
                {
                    cts.Cancel();
                }
            }

            // Has our asynchronous operation completed?
            if (!completionRaised && task != null && task.IsCompleted)
            {
                object resultObject = default(T);
                Exception error = null;
                try
                {
                    resultObject = task.Result;
                }
                catch (Exception ex)
                {
                    error = (task?.Exception?.GetBaseException()) ?? ex;
                }

                var handler = operationCompleted;
                if (handler != null)
                {
                    handler(this, new OperationCompletedEventArgs(resultObject, error));
                }

                completionRaised = true;
                operationCompleted = null;
                ExitScreen();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;

            const int hPad = 32;
            const int vPad = 16;

            // Center the message text in the viewport.
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            Vector2 viewportSize = new Vector2(viewport.Width, viewport.Height);
            Vector2 textSize = font.MeasureString(message);

            // Add enough room to spin a texture.
            Vector2 busyTextureSize = new Vector2(busyTexture.Width * 0.8f);
            Vector2 busyTextureOrigin = new Vector2(busyTexture.Width / 2, busyTexture.Height / 2);

            textSize.X = Math.Max(textSize.X, busyTextureSize.X);
            textSize.Y += busyTextureSize.Y + vPad;

            Vector2 textPosition = (viewportSize - textSize) / 2;

            // The background includes a border somewhat larger than the text itself.
            Rectangle backgroundRectangle = new Rectangle((int)textPosition.X - hPad,
                                                          (int)textPosition.Y - vPad,
                                                          (int)textSize.X + hPad * 2,
                                                          (int)textSize.Y + vPad * 2);

            // Fade the popup alpha during transitions.
            Color color = new Color((byte)255, (byte)255, (byte)255, (byte)TransitionAlpha);

            // Draw the background rectangle.
            Rectangle backgroundRectangle2 = new Rectangle(backgroundRectangle.X - 1, 
                backgroundRectangle.Y - 1, backgroundRectangle.Width + 2, 
                backgroundRectangle.Height + 2);
            ScreenManager.DrawRectangle(backgroundRectangle2, new Color((byte)128, (byte)128, (byte)128,
                (byte)(192.0f * (float)TransitionAlpha / 255.0f)));
            ScreenManager.DrawRectangle(backgroundRectangle, new Color((byte)0, (byte)0, (byte)0,
                (byte)(232.0f * (float)TransitionAlpha / 255.0f)));

            spriteBatch.Begin();
            // Draw the message box text.
            spriteBatch.DrawString(font, message, textPosition, color);

            // Draw the spinning busy progress indicator.
            float busyTextureRotation = (float)gameTime.TotalGameTime.TotalSeconds * 3;

            Vector2 busyTexturePosition = new Vector2(textPosition.X + textSize.X / 2,
                textPosition.Y + textSize.Y - busyTextureSize.Y / 2);

            spriteBatch.Draw(busyTexture, busyTexturePosition, null, color,
                busyTextureRotation, busyTextureOrigin, 0.8f, 
                SpriteEffects.None, 0);

            spriteBatch.End();
        }
    }
}
