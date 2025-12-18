//-----------------------------------------------------------------------------
// ShuffleAnimation.cs
//
// Abstract base class for card shuffle animations
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    /// <summary>
    /// Abstract base class for shuffle animations. Defines the interface for 
    /// creating different types of card shuffles (riffle, overhand, Hindu, etc.)
    /// </summary>
    public abstract class ShuffleAnimation
    {
        /// <summary>
        /// The position where the shuffle animation takes place (typically dealer position)
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Total duration of the shuffle animation
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The card game this animation belongs to
        /// </summary>
        protected CardsGame CardGame { get; private set; }

        /// <summary>
        /// Random number generator for animation variations
        /// </summary>
        protected Random Random { get; private set; }

        /// <summary>
        /// Size of a single card
        /// </summary>
        protected Vector2 CardSize { get; private set; }

        /// <summary>
        /// Callback to invoke when the animation starts
        /// </summary>
        public Action OnAnimationStart { get; set; }

        /// <summary>
        /// Callback to invoke when the animation completes
        /// </summary>
        public Action OnAnimationComplete { get; set; }

        /// <summary>
        /// Creates a new shuffle animation
        /// </summary>
        /// <param name="cardGame">The card game instance</param>
        /// <param name="position">Position where shuffle occurs</param>
        /// <param name="duration">How long the animation should last</param>
        /// <param name="cardSize">Size of each card</param>
        protected ShuffleAnimation(CardsGame cardGame, Vector2 position, TimeSpan duration, Vector2 cardSize)
        {
            CardGame = cardGame;
            Position = position;
            Duration = duration;
            CardSize = cardSize;
            Random = new Random();
        }

        /// <summary>
        /// Creates the animated card components for this shuffle animation.
        /// Each shuffle type implements this differently.
        /// </summary>
        /// <param name="deck">The list of cards to shuffle</param>
        /// <param name="spriteBatch">Shared sprite batch for rendering</param>
        /// <param name="globalTransformation">Transformation matrix for scaling</param>
        /// <returns>List of animated card components with animations applied</returns>
        public abstract List<AnimatedCardsGameComponent> CreateAnimatedCards(
            List<TraditionalCard> deck, 
            SpriteBatch spriteBatch, 
            Matrix globalTransformation);

        /// <summary>
        /// Helper method to create a standard card component
        /// </summary>
        protected AnimatedCardsGameComponent CreateCardComponent(
            TraditionalCard card,
            SpriteBatch spriteBatch,
            Matrix globalTransformation,
            Vector2 startPosition,
            bool faceDown = true)
        {
            var cardComponent = new AnimatedCardsGameComponent(card, CardGame, spriteBatch, globalTransformation)
            {
                CurrentPosition = startPosition,
                IsFaceDown = faceDown,
                Visible = true,
                UseManagedBatch = true  // Shuffle animations use managed batching
            };
            return cardComponent;
        }

        /// <summary>
        /// Helper to add a transition animation to a card
        /// </summary>
        protected void AddTransition(
            AnimatedCardsGameComponent card,
            Vector2 destination,
            TimeSpan startDelay,
            TimeSpan duration)
        {
            var transition = new TransitionGameComponentAnimation(card.CurrentPosition, destination)
            {
                StartDelay = startDelay,
                Duration = duration
            };
            card.AddAnimation(transition);
        }

        /// <summary>
        /// Helper to add a scale animation to a card
        /// </summary>
        protected void AddScale(
            AnimatedCardsGameComponent card,
            float startScale,
            float endScale,
            TimeSpan startDelay,
            TimeSpan duration)
        {
            var scale = new ScaleGameComponentAnimation(startScale, endScale)
            {
                StartDelay = startDelay,
                Duration = duration
            };
            card.AddAnimation(scale);
        }
    }
}
