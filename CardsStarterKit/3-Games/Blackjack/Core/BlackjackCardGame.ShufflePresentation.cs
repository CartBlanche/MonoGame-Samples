//-----------------------------------------------------------------------------
// BlackjackCardGame.ShufflePresentation.cs
//
// Partial class containing dealer deck and shuffle presentation behavior.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        private List<AnimatedCardsGameComponent> dealerDeckCards = new List<AnimatedCardsGameComponent>();

        /// <summary>
        /// Shows the card shuffling animation.
        /// </summary>
        private void ShowShuffleAnimation()
        {
            // Hide dealer deck cards during shuffle (they'll be part of the shuffle animation)
            foreach (var deckCard in dealerDeckCards)
            {
                deckCard.Visible = false;
            }

            dealerDeckCards.Clear();

            // Create a list of cards for the shuffle animation (only show a subset for performance)
            // Using 52 cards (one deck) is enough for a good visual effect
            var deckCards = new List<TraditionalCard>();
            int cardsToShow = Math.Min(52, dealer.Count); // Show max 52 cards
            for (int i = 0; i < cardsToShow; i++)
            {
                deckCards.Add(dealer[i]);
            }

            // Calculate shuffle position in top-center (where shuffle visibly happens)
            Rectangle tableBounds = GameTable.TableBounds;
            int cardWidth = UIConstants.GetCardWidth(screenManager.SafeArea.Width);
            int cardHeight = UIConstants.GetCardHeight(screenManager.SafeArea.Height);
            float shuffleCenterX = tableBounds.Left + (tableBounds.Width / 2f);
            float shuffleY = tableBounds.Top + 40;
            Vector2 shufflePosition = new Vector2(shuffleCenterX, shuffleY);

            // Calculate final deck position (top-right where deck sits for dealing)
            float finalDeckX = tableBounds.Right - cardWidth - 65;
            float finalDeckY = tableBounds.Top + 15;
            Vector2 finalDeckPosition = new Vector2(finalDeckX, finalDeckY);

            // Get scaled card size and shuffle parameters
            Vector2 cardSize = UIConstants.GetCardSize(screenManager.SafeArea.Width, screenManager.SafeArea.Height);
            float splitDistance = UIConstants.GetShuffleSplitDistance(screenManager.SafeArea.Width);
            float cascadeHeight = UIConstants.GetShuffleCascadeHeight(screenManager.SafeArea.Height);

            // Create a riffle shuffle animation at top-center
            var shuffleAnimation = new RiffleShuffleAnimation(
                    this,
                    shufflePosition,
                    TimeSpan.FromSeconds(1.0 * AnimationSpeedMultiplier),
                    cardSize)
                {
                    SplitDistance = splitDistance,
                    CascadeHeight = cascadeHeight
                };

            // Set up callbacks
            shuffleAnimation.OnAnimationComplete = () =>
            {
                AudioManager.PlaySound("Shuffle");

                // Animate 4 cards from center to dealer deck position (top-right)
                // This creates a nice visual transition instead of the deck magically appearing
                AnimateDeckToDealerPosition(shufflePosition, finalDeckPosition, deckCards);

                // Transition to betting state
                State = BlackjackGameState.Betting;
            };

            // Create and initialize the shuffle animation component
            var shuffleComponent = new ShuffleAnimationComponent(
                Game,
                shuffleAnimation,
                deckCards,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation);

            Game.Components.Add(shuffleComponent);
            shuffleComponent.Initialize();
        }

        /// <summary>
        /// Animates 4 cards from the shuffle center position to the dealer deck position.
        /// This creates a visual transition instead of the deck magically appearing.
        /// </summary>
        /// <param name="shufflePosition">Starting position (center of table)</param>
        /// <param name="dealerPosition">Ending position (top-right dealer deck)</param>
        /// <param name="deckCards">The deck of cards to animate from</param>
        private void AnimateDeckToDealerPosition(Vector2 shufflePosition, Vector2 dealerPosition,
            List<TraditionalCard> deckCards)
        {
            // Clear previous dealer deck cards
            dealerDeckCards.Clear();

            // Use 4 cards to represent the deck
            int cardsToAnimate = Math.Min(4, deckCards.Count);
            TimeSpan duration = TimeSpan.FromSeconds(0.6 * AnimationSpeedMultiplier);
            float dealerRotation = MathHelper.ToRadians(-47f); // Match the dealer deck's rotation

            for (int i = 0; i < cardsToAnimate; i++)
            {
                TraditionalCard card = deckCards[i];

                // Create an animated card component
                var animatedCard = new AnimatedCardsGameComponent(
                    card,
                    this,
                    screenManager.SpriteBatch,
                    screenManager.GlobalTransformation);

                // Position at shuffle center with slight offset for stacking
                float stackOffsetX = i * 2f;
                float stackOffsetY = i * 2f;
                animatedCard.CurrentPosition = shufflePosition + new Vector2(stackOffsetX, stackOffsetY);
                animatedCard.CurrentRotation = 0f;
                animatedCard.Visible = true;

                // Add to game components
                Game.Components.Add(animatedCard);

                // Track this as a dealer deck card so it won't be removed during hand cleanup
                dealerDeckCards.Add(animatedCard);

                // Create transition animation with the swooping effect
                var transitionAnim = new TransitionGameComponentAnimation(
                    animatedCard.CurrentPosition,
                    dealerPosition + new Vector2(stackOffsetX, stackOffsetY))
                {
                    Duration = duration
                };

                // Add animation immediately - all 4 cards animate together
                // The slight stacking offset creates a cascade visual effect
                animatedCard.AddAnimation(transitionAnim);

                // Add a rotation animation to rotate to match dealer deck angle
                var rotationAnim = new RotationGameComponentAnimation(0f, dealerRotation)
                {
                    Duration = duration
                };
                animatedCard.AddAnimation(rotationAnim);
            }
        }
    }
}
