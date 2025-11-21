//-----------------------------------------------------------------------------
// RiffleShuffleAnimation.cs
//
// Implements a classic riffle shuffle animation where the deck is split
// in half and cards cascade together in an interleaving pattern
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    /// <summary>
    /// Riffle shuffle: splits deck into two halves, then interleaves them
    /// with a cascading visual effect. This is the classic casino shuffle.
    /// </summary>
    public class RiffleShuffleAnimation : ShuffleAnimation
    {
        /// <summary>
        /// How far apart the two halves split (in pixels)
        /// </summary>
        public float SplitDistance { get; set; } = 120f;

        /// <summary>
        /// Maximum rotation angle for cards during cascade (in radians)
        /// </summary>
        public float MaxRotation { get; set; } = 0.15f;

        /// <summary>
        /// Height of the cascade arc
        /// </summary>
        public float CascadeHeight { get; set; } = 60f;

        /// <summary>
        /// Number of times to repeat the shuffle
        /// </summary>
        public int ShuffleCycles { get; set; } = 2;

        /// <summary>
        /// Creates a new riffle shuffle animation
        /// </summary>
        public RiffleShuffleAnimation(CardsGame cardGame, Vector2 position, TimeSpan duration, Vector2 cardSize)
            : base(cardGame, position, duration, cardSize)
        {
        }

        /// <summary>
        /// Creates the animated cards with riffle shuffle animations
        /// </summary>
        public override List<AnimatedCardsGameComponent> CreateAnimatedCards(
            List<TraditionalCard> deck,
            SpriteBatch spriteBatch,
            Matrix globalTransformation)
        {
            var animatedCards = new List<AnimatedCardsGameComponent>();
            
            // Only show a subset of cards for a clear visual effect (every 3rd card)
            int cardsToShow = Math.Min(18, deck.Count / 3); // Show ~18 cards max
            int step = deck.Count / cardsToShow;
            
            // Split into two halves
            int halfPoint = cardsToShow / 2;
            
            // Time calculations for animation phases
            TimeSpan splitDuration = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * 0.2);
            TimeSpan cascadeDuration = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * 0.6);
            TimeSpan gatherDuration = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * 0.2);

            for (int i = 0; i < cardsToShow; i++)
            {
                int deckIndex = i * step;
                if (deckIndex >= deck.Count) break;
                
                // Determine which half this card belongs to
                bool isLeftHalf = i < halfPoint;
                int cardIndexInHalf = isLeftHalf ? i : (i - halfPoint);
                int totalInHalf = halfPoint;
                
                // Create card component with slight vertical offset for depth
                float depthOffset = i * 0.5f;
                var cardComponent = CreateCardComponent(deck[deckIndex], spriteBatch, globalTransformation, 
                    Position + new Vector2(0, depthOffset), true);
                animatedCards.Add(cardComponent);

                // Phase 1: Split the deck into two piles
                Vector2 splitPosition = Position + new Vector2(
                    isLeftHalf ? -SplitDistance : SplitDistance,
                    depthOffset);
                
                AddTransition(
                    cardComponent,
                    splitPosition,
                    TimeSpan.Zero,
                    splitDuration);

                // Phase 2: Cascade/riffle together with visible arc
                // Stagger the cascade timing so cards drop one by one
                double cascadeProgress = (double)cardIndexInHalf / totalInHalf;
                TimeSpan cascadeDelay = splitDuration + 
                    TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * cascadeProgress * 0.7);
                
                // Create a high arc path for visibility
                Vector2 midPoint = Position + new Vector2(
                    isLeftHalf ? -SplitDistance / 2 : SplitDistance / 2,
                    -CascadeHeight);
                
                Vector2 finalPosition = Position + new Vector2(
                    (isLeftHalf ? -10 : 10) + Random.Next(-8, 8), // Slight spread
                    depthOffset + Random.Next(-3, 3));

                // Arc up
                AddTransition(
                    cardComponent,
                    midPoint,
                    cascadeDelay,
                    TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * 0.2));
                
                // Arc down to center
                AddTransition(
                    cardComponent,
                    finalPosition,
                    cascadeDelay + TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * 0.2),
                    TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * 0.3));

                // Phase 3: Gather into neat pile
                TimeSpan gatherDelay = splitDuration + cascadeDuration;
                AddTransition(
                    cardComponent,
                    Position,
                    gatherDelay,
                    gatherDuration);
            }

            return animatedCards;
        }
    }
}
