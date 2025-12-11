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

        // Icon support properties
        public Texture2D IconTexture { get; set; }
        public float IconScale { get; set; } = 1.0f;
        public Vector2 IconOffset { get; set; } = Vector2.Zero;
        public float TextIconSpacing { get; set; } = 10f;
        public Color IconTint { get; set; } = Color.White;
        public Color IconPressedTint { get; set; } = new Color(200, 200, 200);
        public Rectangle? IconSourceRect { get; set; } = null;

        string regularTexture;
        string pressedTexture;
        string iconTexture;

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
            : this(regularTexture, pressedTexture, null, input, cardGame, sharedSpriteBatch, globalTransformation)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Button"/> class with icon support.
        /// </summary>
        /// <param name="regularTexture">The name of the button's texture.</param>
        /// <param name="pressedTexture">The name of the texture to display when the
        /// button is pressed.</param>
        /// <param name="iconTexture">The name of the icon texture (optional).</param>
        /// <param name="input">A <see cref="GameStateManagement.InputState"/> object
        /// which can be used to retrieve user input.</param>
        /// <param name="cardGame">The associated card game.</param>
        /// <param name="sharedSpriteBatch">The sprite batch used for drawing.</param>
        /// <param name="globalTransformation">The global transformation matrix.</param>
        /// <remarks>Texture names are relative to the "Images" content
        /// folder.</remarks>
        public Button(string regularTexture, string pressedTexture, string iconTexture, InputState input,
            CardsGame cardGame, SpriteBatch sharedSpriteBatch, Matrix globalTransformation)
            : base(cardGame, null, sharedSpriteBatch, globalTransformation)
        {
            this.input = input;
            this.regularTexture = regularTexture;
            this.pressedTexture = pressedTexture;
            this.iconTexture = iconTexture;
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
        /// Loads the content required by the button.
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
            if (iconTexture != null)
            {
                IconTexture = Game.Content.Load<Texture2D>(Path.Combine("Images", iconTexture));
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

            // Use transformed cursor location for all input types (handles scaling/letterboxing)
            Vector2 transformedCursorPos = input.CurrentCursorLocation;

            if (UIUtility.IsDesktop)
            {
                // Handle mouse input - detect click (press + release)
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                {
                    pressed = true;
                    position = transformedCursorPos;
                }

                // Handle button press logic for desktop
                if (pressed)
                {
                    if (!isKeyDown && IntersectWith(position))
                    {
                        isPressed = true;
                        isKeyDown = true;
                    }
                }
                else
                {
                    if (isPressed && IntersectWith(transformedCursorPos))
                    {
                        FireClick();
                    }

                    isPressed = false;
                    isKeyDown = false;
                }
            }
            else if (UIUtility.IsMobile)
            {
                // Handle touch input with gestures
                if (input.Gestures.Count > 0 && input.Gestures[0].GestureType == GestureType.Tap)
                {
                    // Use transformed cursor location (already set by InputState)
                    position = transformedCursorPos;

                    if (IntersectWith(position))
                    {
                        FireClick();
                    }
                }
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

            // Draw button background
            spriteBatch.Draw(isPressed ? PressedTexture : RegularTexture, Bounds, Color.White);

            // Calculate if we have icon and/or text
            bool hasIcon = IconTexture != null;
            bool hasText = Font != null && !string.IsNullOrEmpty(Text);

            if (hasIcon || hasText)
            {
                // Measure content sizes
                Vector2 iconSize = Vector2.Zero;
                Vector2 textSize = Vector2.Zero;

                if (hasIcon)
                {
                    Rectangle sourceRect = IconSourceRect ?? new Rectangle(0, 0, IconTexture.Width, IconTexture.Height);
                    iconSize = new Vector2(sourceRect.Width * IconScale, sourceRect.Height * IconScale);
                }

                if (hasText)
                {
                    textSize = Font.MeasureString(Text);
                }

                // Calculate total content height and starting Y position
                float totalContentHeight = iconSize.Y + (hasIcon && hasText ? TextIconSpacing : 0) + textSize.Y;
                float startY = Bounds.Y + (Bounds.Height - totalContentHeight) / 2;

                // Apply pressed offset for tactile feedback
                Vector2 pressedOffset = isPressed ? new Vector2(0, 2) : Vector2.Zero;

                // Draw icon if present
                if (hasIcon)
                {
                    Rectangle sourceRect = IconSourceRect ?? new Rectangle(0, 0, IconTexture.Width, IconTexture.Height);
                    Vector2 iconPosition = new Vector2(
                        Bounds.X + (Bounds.Width - iconSize.X) / 2,
                        startY
                    );
                    iconPosition += IconOffset + pressedOffset;

                    Color iconColor = isPressed ? IconPressedTint : IconTint;

                    spriteBatch.Draw(
                        IconTexture,
                        iconPosition,
                        sourceRect,
                        iconColor,
                        0f,
                        Vector2.Zero,
                        IconScale,
                        SpriteEffects.None,
                        0f
                    );

                    startY += iconSize.Y + TextIconSpacing;
                }

                // Draw text if present
                if (hasText)
                {
                    Vector2 textPosition = new Vector2(
                        Bounds.X + (Bounds.Width - textSize.X) / 2,
                        startY
                    );
                    textPosition += pressedOffset;

                    spriteBatch.DrawString(Font, Text, textPosition, Color.White);
                }
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