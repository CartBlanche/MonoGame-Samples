//-----------------------------------------------------------------------------
// BlackJackTable.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CardsFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    class BlackJackTable : GameTable
    {
        public Texture2D RingTexture { get; private set; }
        public Vector2 RingOffset { get; private set; }


        public BlackJackTable(Vector2 ringOffset, Rectangle tableBounds, Vector2 dealerPosition, int places,
            Func<int, Vector2> placeOrder, string theme, Game game, SpriteBatch spriteBatch, Matrix? globalTransformation = null)
            : base(tableBounds, dealerPosition, places, placeOrder, theme, game, spriteBatch, globalTransformation)
        {
            RingOffset = ringOffset;
        }

        /// <summary>
        /// Load the component assets
        /// </summary>
        protected override void LoadContent()
        {
            string assetName = string.Format(Path.Combine("Images", "UI", "ring"));
            RingTexture = Game.Content.Load<Texture2D>(assetName);

            base.LoadContent();
        }

        /// <summary>
        /// Draw the rings of the chip on the table
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);

            // Scale rings down for 7 players
            float ringScale = UIConstants.ChipScaleRatio;
            
            for (int placeIndex = 0; placeIndex < Places; placeIndex++)
            {
                Vector2 position = PlaceOrder(placeIndex) + RingOffset;
                Vector2 origin = new Vector2(RingTexture.Width / 2f, RingTexture.Height / 2f);
                
                SpriteBatch.Draw(RingTexture, position, null, Color.White, 0f, origin, ringScale, SpriteEffects.None, 0f);
            }

            SpriteBatch.End();
        }
    }
}