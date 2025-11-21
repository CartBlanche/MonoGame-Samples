//-----------------------------------------------------------------------------
// ShuffleAnimationComponent.cs
//
// Game component that manages a shuffle animation, creates and coordinates
// all animated card components, and handles cleanup
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    /// <summary>
    /// Component that orchestrates a shuffle animation by creating animated
    /// cards and managing their lifecycle
    /// </summary>
    public class ShuffleAnimationComponent : DrawableGameComponent
    {
        private readonly ShuffleAnimation shuffleAnimation;
        private readonly List<TraditionalCard> deck;
        private readonly SpriteBatch spriteBatch;
        private readonly Matrix globalTransformation;
        private List<AnimatedCardsGameComponent> animatedCards;
        private TimeSpan elapsedTime;
        private bool animationStarted = false;
        private bool animationCompleted = false;

        /// <summary>
        /// Gets whether the shuffle animation has completed
        /// </summary>
        public bool IsComplete => animationCompleted;

        /// <summary>
        /// Creates a new shuffle animation component
        /// </summary>
        /// <param name="game">The game instance</param>
        /// <param name="shuffleAnimation">The shuffle animation to perform</param>
        /// <param name="deck">The deck of cards to shuffle</param>
        /// <param name="spriteBatch">Shared sprite batch for rendering</param>
        /// <param name="globalTransformation">Transformation matrix for scaling</param>
        public ShuffleAnimationComponent(
            Game game,
            ShuffleAnimation shuffleAnimation,
            List<TraditionalCard> deck,
            SpriteBatch spriteBatch,
            Matrix globalTransformation)
            : base(game)
        {
            this.shuffleAnimation = shuffleAnimation;
            this.deck = deck;
            this.spriteBatch = spriteBatch;
            this.globalTransformation = globalTransformation;
            
            // Component should not be visible initially
            Visible = false;
        }

        /// <summary>
        /// Initialize and start the animation
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            
            // Create all animated cards
            animatedCards = shuffleAnimation.CreateAnimatedCards(deck, spriteBatch, globalTransformation);
            
            // Add all card components to the game
            foreach (var card in animatedCards)
            {
                Game.Components.Add(card);
            }
            
            // Make component visible and trigger start callback
            Visible = true;
            animationStarted = true;
            shuffleAnimation.OnAnimationStart?.Invoke();
        }

        /// <summary>
        /// Update animation progress and check for completion
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!animationStarted)
                return;

            elapsedTime += gameTime.ElapsedGameTime;

            // Check if animation duration has elapsed
            if (elapsedTime >= shuffleAnimation.Duration && !animationCompleted)
            {
                CompleteAnimation();
            }
        }

        /// <summary>
        /// Called when animation finishes
        /// </summary>
        private void CompleteAnimation()
        {
            animationCompleted = true;
            
            // CRITICAL: Hide cards immediately before doing anything else
            // This prevents the "ghost deck" from appearing at shuffle position
            if (animatedCards != null)
            {
                foreach (var card in animatedCards)
                {
                    card.Visible = false;
                }
            }
            
            // Invoke completion callback
            shuffleAnimation.OnAnimationComplete?.Invoke();
            
            // Clean up all card components
            CleanupCards();
            
            // Remove this component from the game
            Game.Components.Remove(this);
        }

        /// <summary>
        /// Removes all animated card components from the game
        /// </summary>
        private void CleanupCards()
        {
            if (animatedCards != null)
            {
                // First make all cards invisible
                foreach (var card in animatedCards)
                {
                    card.Visible = false;
                }

                // Then remove them from the game
                foreach (var card in animatedCards)
                {
                    if (Game.Components.Contains(card))
                    {
                        Game.Components.Remove(card);
                    }
                }
                animatedCards.Clear();
            }
        }

        /// <summary>
        /// Dispose and cleanup
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupCards();
            }
            base.Dispose(disposing);
        }
    }
}
