//-----------------------------------------------------------------------------
// AnimatedCardsGameComponent.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using CardsFramework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace CardsFramework
{
    /// <summary>
    /// An <see cref="AnimatedGameComponent"/> implemented for a card game
    /// </summary>
    public class AnimatedCardsGameComponent : AnimatedGameComponent
    {
        public TraditionalCard Card { get; private set; }

        private SpriteBatch spriteBatch;
        private Matrix globalTransformation;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="card">The card associated with the animation component.</param>
        /// <param name="cardGame">The associated game.</param>
        public AnimatedCardsGameComponent(TraditionalCard card, CardsGame cardGame, SpriteBatch? sharedSpriteBatch = null, Matrix? globalTransformation = null)
            : base(cardGame, null, sharedSpriteBatch, globalTransformation)
        {
            Card = card;
            this.spriteBatch = sharedSpriteBatch;
            this.globalTransformation = globalTransformation ?? Matrix.Identity;
        }

        /// <summary>
        /// Updates the component.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);


            CurrentFrame = IsFaceDown ? CardGame.cardsAssets["CardBack_" + CardGame.Theme] :
                CardGame.cardsAssets[UIUtilty.GetCardAssetName(Card)];
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);

            // Draw the current at the designated destination, or at the initial 
            // position if a destination has not been set
            if (CurrentFrame != null)
            {
                if (CurrentDestination.HasValue)
                {
                    spriteBatch.Draw(CurrentFrame,
                        CurrentDestination.Value, Color.White);
                }
                else
                {
                    spriteBatch.Draw(CurrentFrame,
                        CurrentPosition, Color.White);
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}