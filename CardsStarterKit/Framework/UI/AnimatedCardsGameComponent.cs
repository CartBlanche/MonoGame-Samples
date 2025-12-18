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
        private Texture2D cachedFaceUpTexture;
        private Texture2D cachedFaceDownTexture;

        /// <summary>
        /// When true, assumes SpriteBatch.Begin() has already been called by parent.
        /// When false, manages its own Begin/End (legacy behavior for Game.Components).
        /// </summary>
        public bool UseManagedBatch { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="card">The card associated with the animation component.</param>
        /// <param name="cardGame">The associated game.</param>
        public AnimatedCardsGameComponent(TraditionalCard card, CardsGame cardGame, SpriteBatch sharedSpriteBatch = null, Matrix? globalTransformation = null)
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

            // Cache both face-up and face-down textures on first access
            // Then switch between them based on IsFaceDown state
            if (cachedFaceDownTexture == null)
            {
                cachedFaceDownTexture = CardGame.cardsAssets["CardBack_" + CardGame.Theme];
            }
            if (cachedFaceUpTexture == null)
            {
                cachedFaceUpTexture = CardGame.cardsAssets[UIUtility.GetCardAssetName(Card)];
            }

            // Use the appropriate cached texture based on current state
            CurrentFrame = IsFaceDown ? cachedFaceDownTexture : cachedFaceUpTexture;
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public override void Draw(GameTime gameTime)
        {
            if (UseManagedBatch)
            {
                // Optimized path: Parent manages the batch (e.g., ShuffleAnimationComponent)
                // Draw directly without Begin/End for better performance
                if (CurrentFrame != null)
                {
                    // Calculate origin point (center of texture for rotation)
                    Vector2 origin = new Vector2(CurrentFrame.Width / 2f, CurrentFrame.Height / 2f);

                    if (CurrentDestination.HasValue)
                    {
                        // When using destination rectangle with rotation, we need center position
                        Vector2 centerPosition = new Vector2(
                            CurrentDestination.Value.X + CurrentDestination.Value.Width / 2f,
                            CurrentDestination.Value.Y + CurrentDestination.Value.Height / 2f);

                        // Calculate scale to match destination size
                        Vector2 scale = new Vector2(
                            CurrentDestination.Value.Width / (float)CurrentFrame.Width,
                            CurrentDestination.Value.Height / (float)CurrentFrame.Height);

                        spriteBatch.Draw(CurrentFrame, centerPosition, null, Color.White,
                            CurrentRotation, origin, scale, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        // Position + origin to center the rotation point
                        Vector2 centerPosition = CurrentPosition + origin;

                        spriteBatch.Draw(CurrentFrame, centerPosition, null, Color.White,
                            CurrentRotation, origin, 1f, SpriteEffects.None, 0f);
                    }
                }
            }
            else
            {
                // Legacy path: Manage our own batch (for Game.Components usage)
                base.Draw(gameTime);
            }
        }
    }
}