//-----------------------------------------------------------------------------
// SpriteSheet.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;


namespace TiledSprites
{

    /// <summary>
    /// Stores entries for individual sprites on a single texture.
    /// </summary>
    public class SpriteSheet
    {
        private Texture2D texture;
        private Dictionary<int, Rectangle> spriteDefinitions;

        /// <summary>
        /// Create a new Sprite Sheet
        /// </summary>
        public SpriteSheet(Texture2D sheetTexture)
        {
            texture = sheetTexture;
            spriteDefinitions = new Dictionary<int, Rectangle>();
        }

        /// <summary>
        /// Add a source sprite for fast retrieval
        /// </summary>
        public void AddSourceSprite(int key, Rectangle rect)
        {
            spriteDefinitions.Add(key, rect);
        }

        /// <summary>
        /// Get the source sprite texture
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                return texture;
            }
        }

        /// <summary>
        /// Get the rectangle that defines the source sprite
        /// on the sheet.
        /// </summary>
        public Rectangle this[int i]
        {
            get
            {
                return spriteDefinitions[i];
            }
        }

        /// <summary>
        /// A faster lookup using refs to avoid stack copies.
        /// </summary>
        public void GetRectangle(ref int i, out Rectangle rect)
        {
            rect = spriteDefinitions[i];
        }
    }
}