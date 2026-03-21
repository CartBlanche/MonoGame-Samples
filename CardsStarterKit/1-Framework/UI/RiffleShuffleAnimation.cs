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
        // Animation timing ratios (must sum to 1.0)
        private const float SplitPhaseRatio = 0.2f;     // Cards split into two halves
        private const float PauseAfterSplitRatio = 0.1f; // Pause to show the split (longer)
        private const float CascadePhaseRatio = 0.55f;  // Interleave cascade (slower for polish)
        private const float PauseBeforeGatherRatio = 0.1f; // Pause before pushing together (longer)
        private const float GatherPhaseRatio = 0.05f;   // Quick final gather

        // Card display settings
        private const int MaxVisibleCards = 18;
        private const int CardDisplayStep = 3; // Show every 3rd card
        private const float CardDepthSpacing = 0.5f;

        // Cascade animation settings
        private const float CascadeArcRatio = 0.2f; // Arc up takes 20% of cascade time
        private const float CascadeDownRatio = 0.3f; // Arc down takes 30% of cascade time
        private const float CascadeStaggerRatio = 0.7f; // Cards stagger over 70% of cascade phase

        // Randomization ranges
        private const int FinalPositionRandomX = 8; // ±8 pixels horizontal
        private const int FinalPositionRandomY = 3; // ±3 pixels vertical
        private const int FinalOffsetX = 10; // Offset between left/right halves

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

            // Only show a subset of cards for a clear visual effect
            int cardsToShow = Math.Min(MaxVisibleCards, deck.Count / CardDisplayStep);
            int step = deck.Count / cardsToShow;

            // Split into two halves
            int halfPoint = cardsToShow / 2;

            // Time calculations for animation phases
            TimeSpan splitDuration = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * SplitPhaseRatio);
            TimeSpan pauseAfterSplit = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * PauseAfterSplitRatio);
            TimeSpan cascadeDuration = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * CascadePhaseRatio);
            TimeSpan pauseBeforeGather = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * PauseBeforeGatherRatio);
            TimeSpan gatherDuration = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * GatherPhaseRatio);

            for (int i = 0; i < cardsToShow; i++)
            {
                int deckIndex = i * step;
                if (deckIndex >= deck.Count) break;
                
                // Determine which half this card belongs to
                bool isLeftHalf = i < halfPoint;
                int cardIndexInHalf = isLeftHalf ? i : (i - halfPoint);
                int totalInHalf = halfPoint;
                
                // Create card component with slight vertical offset for depth
                float depthOffset = i * CardDepthSpacing;
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
                TimeSpan cascadeDelay = splitDuration + pauseAfterSplit +
                    TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * cascadeProgress * CascadeStaggerRatio);

                // Create a high arc path for visibility
                Vector2 midPoint = Position + new Vector2(
                    isLeftHalf ? -SplitDistance / 2 : SplitDistance / 2,
                    -CascadeHeight);

                Vector2 finalPosition = Position + new Vector2(
                    (isLeftHalf ? -FinalOffsetX : FinalOffsetX) + Random.Next(-FinalPositionRandomX, FinalPositionRandomX),
                    depthOffset + Random.Next(-FinalPositionRandomY, FinalPositionRandomY));

                // Arc up
                AddTransition(
                    cardComponent,
                    midPoint,
                    cascadeDelay,
                    TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * CascadeArcRatio));

                // Rotate during arc up - left cards tilt left, right cards tilt right
                float targetRotation = isLeftHalf ? -MaxRotation : MaxRotation;
                AddRotation(
                    cardComponent,
                    0f,
                    targetRotation,
                    cascadeDelay,
                    TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * CascadeArcRatio));

                // Arc down to center
                AddTransition(
                    cardComponent,
                    finalPosition,
                    cascadeDelay + TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * CascadeArcRatio),
                    TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * CascadeDownRatio));

                // Rotate back to flat during arc down
                AddRotation(
                    cardComponent,
                    targetRotation,
                    0f,
                    cascadeDelay + TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * CascadeArcRatio),
                    TimeSpan.FromMilliseconds(cascadeDuration.TotalMilliseconds * CascadeDownRatio));

                // Phase 3: Gather into neat pile
                TimeSpan gatherDelay = splitDuration + pauseAfterSplit + cascadeDuration + pauseBeforeGather;
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
