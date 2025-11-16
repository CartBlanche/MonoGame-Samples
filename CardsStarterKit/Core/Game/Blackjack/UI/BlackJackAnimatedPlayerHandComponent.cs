//-----------------------------------------------------------------------------
// BlackjackAnimatedHandComponent.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CardsFramework;
using Microsoft.Xna.Framework;

namespace Blackjack
{
    public class BlackjackAnimatedPlayerHandComponent : AnimatedHandGameComponent
    {
        Vector2 offset;
        private int horizontalSpacing;
        private int verticalSpacing;

        /// <summary>
        /// Creates a new instance of the 
        /// <see cref="BlackjackAnimatedPlayerHandComponent"/> class.
        /// </summary>
        /// <param name="place">A number indicating the hand's position on the 
        /// game table.</param>
        /// <param name="hand">The player's hand.</param>
        /// <param name="cardGame">The associated game.</param>

        public BlackjackAnimatedPlayerHandComponent(int place, Hand hand,
            CardsGame cardGame, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Matrix globalTransformation)
            : base(place, hand, cardGame, spriteBatch, globalTransformation)
        {
            // Move cards up above the chip circle by a card height + padding
            if (cardGame is BlackjackCardGame  blackjackGame)
            {
                int cardHeight = UIConstants.GetCardHeight(blackjackGame.ScreenManager.SafeArea.Height);
                this.offset = new Vector2(0, -(cardHeight / 2 )); // Above chip circle
            }
            else
            {
                this.offset = new Vector2(0, -120); // Fallback offset
            }
            InitializeSpacing(cardGame);
        }

        /// <summary>
        /// Creates a new instance of the 
        /// <see cref="BlackjackAnimatedPlayerHandComponent"/> class.
        /// </summary>
        /// <param name="place">A number indicating the hand's position on the 
        /// game table.</param>
        /// <param name="hand">The player's hand.</param>
        /// <param name="cardGame">The associated game.</param>
        /// <param name="offset">An offset which will be added to all card locations
        /// returned by this component.</param>
        public BlackjackAnimatedPlayerHandComponent(int place, Vector2 offset,
            Hand hand, CardsGame cardGame, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Matrix globalTransformation)
            : base(place, hand, cardGame, spriteBatch, globalTransformation)
        {
            // Apply additional Y offset to move cards above chip circle, keeping provided X offset
            var blackjackGame = cardGame as BlackjackCardGame;
            if (blackjackGame != null)
            {
                int cardHeight = UIConstants.GetCardHeight(blackjackGame.ScreenManager.SafeArea.Height);
                this.offset = new Vector2(offset.X, offset.Y - cardHeight - 15); // Above chip circle
            }
            else
            {
                this.offset = new Vector2(offset.X, offset.Y - 120); // Fallback offset
            }
            InitializeSpacing(cardGame);
        }

        /// <summary>
        /// Initialize card spacing based on screen dimensions
        /// </summary>
        private void InitializeSpacing(CardsGame cardGame)
        {
            var blackjackGame = cardGame as BlackjackCardGame;
            if (blackjackGame != null)
            {
                int screenWidth = blackjackGame.ScreenManager.SafeArea.Width;
                int screenHeight = blackjackGame.ScreenManager.SafeArea.Height;
                horizontalSpacing = UIConstants.GetPlayerCardHorizontalSpacing(screenWidth);
                verticalSpacing = UIConstants.GetPlayerCardVerticalSpacing(screenHeight);
            }
            else
            {
                // Fallback to reasonable defaults
                horizontalSpacing = 25;
                verticalSpacing = 30;
            }
        }

        /// <summary>
        /// Gets the position relative to the hand position at which a specific card
        /// contained in the hand should be rendered.
        /// </summary>
        /// <param name="cardLocationInHand">The card's location in the hand (0 is the
        /// first card in the hand).</param>
        /// <returns>An offset from the hand's location where the card should be 
        /// rendered.</returns>
        public override Vector2 GetCardRelativePosition(int cardLocationInHand)
        {
            return new Vector2(horizontalSpacing * cardLocationInHand, -verticalSpacing * cardLocationInHand) +
                offset;
        }
    }
}