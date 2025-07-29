//-----------------------------------------------------------------------------
// MenuEntry.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace XnaGraphicsDemo
{
    /// <summary>
    /// Base class for each entry in a MenuComponent.
    /// </summary>
    class MenuEntry
    {
        // Constants.
        public const int Height = 64;
        public const int Border = 32;


        // Properties.
        /// <summary>
        /// Gets or sets the text displayed for this menu entry.
        /// </summary>
        public virtual string Text { get; set; }
        /// <returns>The text displayed for this menu entry.</returns>
        /// <summary>
        /// Gets or sets the position of this menu entry on the screen.
        /// </summary>
        public Vector2 Position { get; set; }
        /// <returns>The position of this menu entry on the screen.</returns>
        /// <summary>
        /// Gets or sets whether this menu entry is currently focused.
        /// </summary>
        public bool IsFocused { get; set; }
        /// <returns>True if the menu entry is focused; otherwise, false.</returns>
        /// <summary>
        /// Gets or sets whether this menu entry can be dragged.
        /// </summary>
        public bool IsDraggable { get; set; }
        /// <returns>True if the menu entry can be dragged; otherwise, false.</returns>
        /// <summary>
        /// Gets or sets the action to invoke when this menu entry is clicked.
        /// </summary>
        public Action Clicked { get; set; }
        /// <returns>The action to invoke when this menu entry is clicked.</returns>

        /// <summary>
        /// Gets the color of the menu entry, blue if focused, white otherwise.
        /// </summary>
        public Color Color { get { return IsFocused ? Color.Blue : Color.White; } }
        /// <returns>The color of the menu entry.</returns>

        Vector2 positionOffset;

        
        /// <summary>
        /// Draws the menu entry.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch used for drawing.</param>
        /// <param name="font">The font used to draw the text.</param>
        /// <param name="blankTexture">A blank texture for drawing backgrounds or highlights.</param>
        public virtual void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D blankTexture)
        {
            positionOffset = new Vector2(0, (Height - font.LineSpacing) / 2);

            spriteBatch.DrawString(font, Text, Position + positionOffset, Color);
        }


        /// <summary>
        /// Handles clicks on this menu entry, invoking the Clicked action and spawning feedback if not draggable.
        /// </summary>
        public virtual void OnClicked()
        {
            // If we have a click delegate, call that now.
            if (Clicked != null)
                Clicked();

            // If we are not draggable, spawn a visual feedback effect.
            if (!IsDraggable)
                DemoGame.SpawnZoomyText(Text, Position + positionOffset);
        }


        /// <summary>
        /// Handles dragging this menu entry from left to right.
        /// </summary>
        /// <param name="delta">The amount the pointer has moved since the last drag event.</param>
        public virtual void OnDragged(float delta)
        {
        }
    }


    /// <summary>
    /// Menu entry subclass for boolean toggle values.
    /// </summary>
    class BoolMenuEntry : MenuEntry
    {
        // Properties.
        public bool Value { get; set; }
        public string Label { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="BoolMenuEntry"/> class with the specified label.
        /// </summary>
        /// <param name="label">The label for the toggle entry.</param>
        public BoolMenuEntry(string label)
        {
            Label = label;
        }


        /// <summary>
        /// Click handler toggles the boolean value and invokes the base click logic.
        /// </summary>
        public override void OnClicked()
        {
            Value = !Value;

            base.OnClicked();
        }


        /// <summary>
        /// Gets the display text for the toggle entry, showing the label and current value.
        /// </summary>
        public override string Text
        {
            get { return Label + " " + (Value ? "on" : "off"); }
            set { }
        }
    }


    /// <summary>
    /// Menu entry subclass for floating point slider values.
    /// </summary>
    class FloatMenuEntry : MenuEntry
    {
        // Properties.
        public float Value { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="FloatMenuEntry"/> class and marks it as draggable.
        /// </summary>
        public FloatMenuEntry()
        {
            IsDraggable = true;
        }


        /// <summary>
        /// Drag handler changes the slider position.
        /// </summary>
        /// <param name="delta">The amount the pointer has moved since the last drag event.</param>
        public override void OnDragged(float delta)
        {
            const float speed = 1f / 300;

            Value = MathHelper.Clamp(Value + delta * speed, 0, 1);
        }


        /// <summary>
        /// Custom draw function displays a slider bar in addition to the item text.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch used for drawing.</param>
        /// <param name="font">The font used to draw the text.</param>
        /// <param name="blankTexture">A blank texture for drawing the slider bar.</param>
        public override void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D blankTexture)
        {
            base.Draw(spriteBatch, font, blankTexture);

            Vector2 size = font.MeasureString(Text);
            size.Y /= 2;

            Vector2 pos = Position + size;

            pos.X += 8;
            pos.Y += (Height - font.LineSpacing) / 2;

            float w = 480 - Border - pos.X;

            spriteBatch.Draw(blankTexture, new Rectangle((int)pos.X, (int)pos.Y - 3, (int)(w * Value), 6), Color);
        }
    }
}
