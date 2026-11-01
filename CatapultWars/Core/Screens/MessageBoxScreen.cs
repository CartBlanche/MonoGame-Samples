//-----------------------------------------------------------------------------
// MessageBoxScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace CatapultGame
{
    /// <summary>
    /// A popup message box screen, used to display "are you sure?"
    /// confirmation messages.
    /// </summary>
    class MessageBoxScreen : GameScreen
    {

        string message;
        Texture2D gradientTexture;



        public event EventHandler<PlayerIndexEventArgs> Accepted;
        public event EventHandler<PlayerIndexEventArgs> Cancelled;




        /// <summary>
        /// Constructor automatically includes the standard "A=ok, B=cancel"
        /// usage text prompt.
        /// </summary>
        public MessageBoxScreen(string message)
            : this(message, true)
        { }


        /// <summary>
        /// Constructor lets the caller specify whether to include the standard
        /// "A=ok, B=cancel" usage text prompt.
        /// </summary>
        public MessageBoxScreen(string message, bool includeUsageText)
        {
            if (includeUsageText)
                this.message = message + Resources.MessageBoxUsage;
            else
                this.message = message;

            IsPopup = true;
            EnabledGestures = GestureType.Tap;

            TransitionOnTime = TimeSpan.FromSeconds(0.2);
            TransitionOffTime = TimeSpan.FromSeconds(0.2);
        }


        /// <summary>
        /// Loads graphics content for this screen. This uses the shared ContentManager
        /// provided by the Game class, so the content will remain loaded forever.
        /// Whenever a subsequent MessageBoxScreen tries to load this same content,
        /// it will just get back another reference to the already loaded data.
        /// </summary>
        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;

            gradientTexture = content.Load<Texture2D>("gradient");
        }





        /// <summary>
        /// Responds to user input, accepting or cancelling the message box.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            PlayerIndex playerIndex;

            // We pass in our ControllingPlayer, which may either be null (to
            // accept input from any player) or a specific index. If we pass a null
            // controlling player, the InputState helper returns to us which player
            // actually provided the input. We pass that through to our Accepted and
            // Cancelled events, so they can tell which player triggered them.
            // A tap or click inside the box accepts it, and outside cancels, for phones with no keys.
            bool? pointerAccepts = null;
            Rectangle box = BoxRectangle();
            if (input.MouseGesture.HasFlag(MouseGestureType.LeftClick))
                pointerAccepts = box.Contains(new Point((int)input.CurrentMousePosition.X, (int)input.CurrentMousePosition.Y));
            foreach (GestureSample gesture in input.Gestures)
            {
                if (gesture.GestureType == GestureType.Tap)
                    pointerAccepts = box.Contains(new Point((int)gesture.Position.X, (int)gesture.Position.Y));
            }

            if (input.IsMenuSelect(ControllingPlayer, out playerIndex) || pointerAccepts == true)
            {
                // Raise the accepted event, then exit the message box.
                if (Accepted != null)
                    Accepted(this, new PlayerIndexEventArgs(playerIndex));

                ExitScreen();
            }
            else if (input.IsMenuCancel(ControllingPlayer, out playerIndex) || pointerAccepts == false)
            {
                // Raise the cancelled event, then exit the message box.
                if (Cancelled != null)
                    Cancelled(this, new PlayerIndexEventArgs(playerIndex));

                ExitScreen();
            }
        }





        // The message text is centred in the viewport.
        Vector2 TextPosition()
        {
            Vector2 viewportSize = new Vector2(ScreenManager.BASE_BUFFER_WIDTH, ScreenManager.BASE_BUFFER_HEIGHT);
            return (viewportSize - ScreenManager.Font.MeasureString(message)) / 2;
        }

        // The background includes a border somewhat larger than the text itself.
        Rectangle BoxRectangle()
        {
            const int hPad = 32;
            const int vPad = 16;

            Vector2 textPosition = TextPosition();
            Vector2 textSize = ScreenManager.Font.MeasureString(message);
            return new Rectangle((int)textPosition.X - hPad, (int)textPosition.Y - vPad,
                                 (int)textSize.X + hPad * 2, (int)textSize.Y + vPad * 2);
        }

        /// <summary>
        /// Draws the message box.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;

            // Darken down any other screens that were drawn beneath the popup.
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            Vector2 textPosition = TextPosition();
            Rectangle backgroundRectangle = BoxRectangle();

            // Fade the popup alpha during transitions.
            Color color = Color.White * TransitionAlpha;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

            // Draw the background rectangle.
            spriteBatch.Draw(gradientTexture, backgroundRectangle, color);

            // Draw the message box text.
            spriteBatch.DrawString(font, message, textPosition, color);

            spriteBatch.End();
        }


    }
}