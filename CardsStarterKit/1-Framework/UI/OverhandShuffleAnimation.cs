//-----------------------------------------------------------------------------
// OverhandShuffleAnimation.cs
//
// Implements an overhand shuffle animation where packets of cards are
// repeatedly lifted from the back and dropped onto the front, creating
// the most common casual shuffle pattern
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    /// <summary>
    /// Overhand shuffle: repeatedly pull small packets from back and drop on front.
    /// Uses scaling to simulate depth (cards lifting toward camera appear larger).
    /// </summary>
    public class OverhandShuffleAnimation : ShuffleAnimation
    {
        // Animation timing ratios (must sum to 1.0)
        private const float LiftPhaseRatio = 0.3f;
        private const float TransferPhaseRatio = 0.4f;
        private const float DropPhaseRatio = 0.3f;

        // Card display settings
        private const int MaxVisibleCards = 15;
        private const int CardDisplayStep = 3; // Show every 3rd card
        private const float CardDepthSpacing = 0.5f;

        // Scale settings for depth illusion
        private const float TableScale = 1.0f; // Normal size on table
        private const float LiftedScale = 1.18f; // Larger when lifted (closer to camera)

        // Randomization ranges
        private const int FinalPositionRandomX = 5;
        private const int FinalPositionRandomY = 2;

        /// <summary>
        /// How high cards lift vertically (in pixels)
        /// </summary>
        public float LiftHeight { get; set; } = 70f;

        /// <summary>
        /// How far forward cards move during transfer (in pixels)
        /// </summary>
        public float ForwardDistance { get; set; } = 50f;

        /// <summary>
        /// Maximum rotation angle during lift (in radians)
        /// </summary>
        public float MaxRotation { get; set; } = 0.08f;

        /// <summary>
        /// Number of packets to shuffle (more packets = more realistic)
        /// </summary>
        public int PacketCount { get; set; } = 6;

        /// <summary>
        /// Creates a new overhand shuffle animation
        /// </summary>
        public OverhandShuffleAnimation(CardsGame cardGame, Vector2 position, TimeSpan duration, Vector2 cardSize)
            : base(cardGame, position, duration, cardSize)
        {
        }

        /// <summary>
        /// Creates the animated cards with overhand shuffle animations
        /// </summary>
        public override List<AnimatedCardsGameComponent> CreateAnimatedCards(
            List<TraditionalCard> deck,
            SpriteBatch spriteBatch,
            Matrix globalTransformation)
        {
            var animatedCards = new List<AnimatedCardsGameComponent>();

            // Only show a subset of cards for clear visual effect
            int cardsToShow = Math.Min(MaxVisibleCards, deck.Count / CardDisplayStep);
            int step = deck.Count / cardsToShow;

            // Calculate how many cards per packet
            int cardsPerPacket = Math.Max(1, cardsToShow / PacketCount);

            // Time calculations for animation phases
            TimeSpan packetInterval = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds / PacketCount);
            TimeSpan liftDuration = TimeSpan.FromMilliseconds(packetInterval.TotalMilliseconds * LiftPhaseRatio);
            TimeSpan transferDuration = TimeSpan.FromMilliseconds(packetInterval.TotalMilliseconds * TransferPhaseRatio);
            TimeSpan dropDuration = TimeSpan.FromMilliseconds(packetInterval.TotalMilliseconds * DropPhaseRatio);

            for (int i = 0; i < cardsToShow; i++)
            {
                int deckIndex = i * step;
                if (deckIndex >= deck.Count) break;

                // Determine which packet this card belongs to
                int packetIndex = i / cardsPerPacket;
                if (packetIndex >= PacketCount) packetIndex = PacketCount - 1;

                // Create card component with slight vertical offset for depth
                float depthOffset = i * CardDepthSpacing;
                var cardComponent = CreateCardComponent(deck[deckIndex], spriteBatch, globalTransformation,
                    Position + new Vector2(0, depthOffset), true);
                animatedCards.Add(cardComponent);

                // Calculate timing for this packet
                TimeSpan packetDelay = TimeSpan.FromMilliseconds(packetIndex * packetInterval.TotalMilliseconds);

                // Phase 1: Lift up (cards move toward camera, appear larger)
                Vector2 liftPosition = Position + new Vector2(0, -LiftHeight + depthOffset);

                AddTransition(
                    cardComponent,
                    liftPosition,
                    packetDelay,
                    liftDuration);

                // Scale up during lift (simulates moving toward camera)
                AddScale(
                    cardComponent,
                    TableScale,
                    LiftedScale,
                    packetDelay,
                    liftDuration);

                // Slight tilt during lift for realism
                AddRotation(
                    cardComponent,
                    0f,
                    -MaxRotation,
                    packetDelay,
                    liftDuration);

                // Phase 2: Transfer forward over the pile
                Vector2 transferPosition = Position + new Vector2(ForwardDistance, -LiftHeight + depthOffset);

                AddTransition(
                    cardComponent,
                    transferPosition,
                    packetDelay + liftDuration,
                    transferDuration);

                // Stay scaled up during transfer (still elevated)
                AddScale(
                    cardComponent,
                    LiftedScale,
                    LiftedScale,
                    packetDelay + liftDuration,
                    transferDuration);

                // Rotate back to flat
                AddRotation(
                    cardComponent,
                    -MaxRotation,
                    0f,
                    packetDelay + liftDuration,
                    transferDuration);

                // Phase 3: Drop onto pile (cards move away from camera, appear smaller)
                Vector2 finalPosition = Position + new Vector2(
                    ForwardDistance + Random.Next(-FinalPositionRandomX, FinalPositionRandomX),
                    depthOffset + Random.Next(-FinalPositionRandomY, FinalPositionRandomY));

                AddTransition(
                    cardComponent,
                    finalPosition,
                    packetDelay + liftDuration + transferDuration,
                    dropDuration);

                // Scale back down as card drops to table (moves away from camera)
                AddScale(
                    cardComponent,
                    LiftedScale,
                    TableScale,
                    packetDelay + liftDuration + transferDuration,
                    dropDuration);
            }

            return animatedCards;
        }
    }
}
