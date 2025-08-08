//-----------------------------------------------------------------------------
// Button.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameStateManagement;
using CardsFramework;
using Microsoft.Xna.Framework.Input.Touch;
using System.IO;

namespace Blackjack
{
    public class Button : AnimatedGameComponent
    {
        bool isKeyDown = false;
        bool isPressed = false;
        SpriteBatch spriteBatch;

        public Texture2D RegularTexture { get; set; }
        public Texture2D PressedTexture { get; set; }
        public SpriteFont Font { get; set; }
        public Rectangle Bounds { get; set; }

        string regularTexture;
        string pressedTexture;

        public event EventHandler Click;
        InputState input;

        InputHelper inputHelper;

        private Matrix globalTransformation;


        /// <summary>
        /// Creates a new instance of the <see cref="Button"/> class.
        /// </summary>
        /// <param name="regularTexture">The name of the button's texture.</param>
        /// <param name="pressedTexture">The name of the texture to display when the 
        /// button is pressed.</param>
        /// <param name="input">A <see cref="GameStateManagement.InputState"/> object
        /// which can be used to retrieve user input.</param>
        /// <param name="cardGame">The associated card game.</param>
        /// <param name="sharedSpriteBatch">The sprite batch used for drawing.</param>
        /// <param name="globalTransformation">The global transformation matrix.</param>
        /// <remarks>Texture names are relative to the "Images" content 
        /// folder.</remarks>
        public Button(string regularTexture, string pressedTexture, InputState input,
            CardsGame cardGame, SpriteBatch sharedSpriteBatch, Matrix globalTransformation)
            : base(cardGame, null, sharedSpriteBatch, globalTransformation)
        {
            this.input = input;
            this.regularTexture = regularTexture;
            this.pressedTexture = pressedTexture;
            this.spriteBatch = sharedSpriteBatch;
            this.globalTransformation = globalTransformation;
        }

        /// <summary>
        /// Initializes the button.
        /// </summary>
        public override void Initialize()
        {
            // Get Xbox cursor
            inputHelper = null;
            for (int componentIndex = 0; componentIndex < Game.Components.Count; componentIndex++)
            {
                if (Game.Components[componentIndex] is InputHelper)
                {
                    inputHelper = (InputHelper)Game.Components[componentIndex];
                    break;
                }
            }

            base.Initialize();
        }

        /// <summary>
        /// Loads the content required bt the button.
        /// </summary>
        protected override void LoadContent()
        {
            if (regularTexture != null)
            {
                RegularTexture = Game.Content.Load<Texture2D>(Path.Combine("Images", regularTexture));
            }
            if (pressedTexture != null)
            {
                PressedTexture = Game.Content.Load<Texture2D>(Path.Combine("Images", pressedTexture));
            }

            base.LoadContent();
        }

        /// <summary>
        /// Performs update logic for the button.
        /// </summary>
        /// <param name="gameTime">The time that has passed since the last call to 
        /// this method.</param>
        public override void Update(GameTime gameTime)
        {
            if (RegularTexture != null)
            {
                HandleInput();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Handle the input of adding chip on all platform
        /// </summary>
        /// <param name="mouseState">Mouse input information.</param>
        /// <param name="inputHelper">Input of Xbox simulated cursor.</param>
        private void HandleInput()
        {
            bool pressed = false;
            Vector2 position = Vector2.Zero;

            // Check for tap gestures
            if (input.Gestures.Count > 0 && input.Gestures[0].GestureType == GestureType.Tap)
            {
                pressed = true;
                position = input.Gestures[0].Position;
            }

            // Check for mouse input
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
            {
                pressed = true;
                position = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
            }

            // Handle button press logic
            if (pressed)
            {
                if (!isKeyDown && IntersectWith(position))
                {
                    isPressed = true;

                    if (UIUtilty.IsMobile)
                    {
                        FireClick();
                        isPressed = false;
                    }

                    isKeyDown = true;
                }
            }
            else
            {
                if (isPressed && (IntersectWith(new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y)) ||
                                  IntersectWith(inputHelper?.PointPosition ?? Vector2.Zero)))
                {
                    FireClick();
                }

                isPressed = false;
                isKeyDown = false;
            }
        }

        /// <summary>
        /// Checks if the button intersects with a specified position
        /// </summary>
        /// <param name="position">The position to check intersection against.</param>
        /// <returns>True if the position intersects with the button, 
        /// false otherwise.</returns>
        private bool IntersectWith(Vector2 position)
        {
            Rectangle touchTap = new Rectangle((int)position.X - 1, (int)position.Y - 1, 2, 2);
            return Bounds.Intersects(touchTap);
        }

        /// <summary>
        /// Fires the button's click event.
        /// </summary>
        public void FireClick()
        {
            if (Click != null)
            {
                Click(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Draws the button.
        /// </summary>
        /// <param name="gameTime">The time that has passed since the last call to 
        /// this method.</param>
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);

            spriteBatch.Draw(isPressed ? PressedTexture : RegularTexture, Bounds, Color.White);
            if (Font != null)
            {
                Vector2 textPosition = Font.MeasureString(Text);
                textPosition = new Vector2(Bounds.Width - textPosition.X,
                    Bounds.Height - textPosition.Y);
                textPosition /= 2;
                textPosition.X += Bounds.X;
                textPosition.Y += Bounds.Y;
                spriteBatch.DrawString(Font, Text, textPosition, Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            Click = null;
            base.Dispose(disposing);
        }
    }
}