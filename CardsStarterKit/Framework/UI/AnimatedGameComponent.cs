//-----------------------------------------------------------------------------
// AnimatedGameComponent.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CardsFramework;

namespace CardsFramework
{
    /// <summary>
    /// A game component.
    /// Enable variable display while managing and displaying a set of
    /// <see cref="AnimatedGameComponentAnimation">Animations</see>
    /// </summary>
    public class AnimatedGameComponent : DrawableGameComponent
    {

        public Texture2D CurrentFrame { get; set; }
        public Rectangle? CurrentSegment { get; set; }
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public bool IsFaceDown = true;
        public Vector2 CurrentPosition { get; set; }
        public Rectangle? CurrentDestination { get; set; }
        public float CurrentRotation { get; set; } = 0f;

        List<AnimatedGameComponentAnimation> runningAnimations =
            new List<AnimatedGameComponentAnimation>();

        /// <summary>
        /// Whether or not an animation belonging to the component is running.
        /// </summary>
        public virtual bool IsAnimating { get { return runningAnimations.Count > 0; } }

        public CardsGame CardGame { get; private set; }

        private SpriteBatch spriteBatch;
        private Matrix globalTransformation;

        /// <summary>
        /// Initializes a new instance of the class, using black text color.
        /// </summary>
        /// <param name="cardGame">The associated card game.</param>
        /// <param name="currentFrame">The texture serving as the current frame
        /// to display as the component.</param>
        public AnimatedGameComponent(CardsGame cardGame, Texture2D currentFrame, SpriteBatch sharedSpriteBatch, Matrix? globalTransformation = null)
            : base(cardGame.Game)
        {
            if (sharedSpriteBatch == null)
                throw new ArgumentNullException(nameof(sharedSpriteBatch), "AnimatedGameComponent requires a valid SpriteBatch.");
                
            CardGame = cardGame;
            CurrentFrame = currentFrame;
            TextColor = Color.Black;
            this.spriteBatch = sharedSpriteBatch;
            this.globalTransformation = globalTransformation ?? Matrix.Identity;
        }

        /// <summary>
        /// Keeps track of the component's animations.
        /// </summary>
        /// <param name="gameTime">The time which as elapsed since the last call
        /// to this method.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Iterate backwards to safely remove completed animations
            for (int animationIndex = runningAnimations.Count - 1; animationIndex >= 0; animationIndex--)
            {
                runningAnimations[animationIndex].AccumulateElapsedTime(gameTime.ElapsedGameTime);
                runningAnimations[animationIndex].Run(gameTime);
                if (runningAnimations[animationIndex].IsDone())
                {
                    runningAnimations.RemoveAt(animationIndex);
                }
            }
        }

        /// <summary>
        /// Draws the animated component and its associated text, if it exists, at
        /// the object's set destination. If a destination is not set, its initial
        /// position is used.
        /// </summary>
        /// <param name="gameTime">The time which as elapsed since the last call
        /// to this method.</param>
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);

            if (CurrentFrame != null)
            {
                // Calculate origin point (center of texture for rotation)
                Vector2 origin = new Vector2(CurrentFrame.Width / 2f, CurrentFrame.Height / 2f);

                // Draw at the destination if one is set
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

                    spriteBatch.Draw(CurrentFrame, centerPosition, CurrentSegment, Color.White,
                        CurrentRotation, origin, scale, SpriteEffects.None, 0f);

                    if (Text != null)
                    {
                        Vector2 size = CardGame.Font.MeasureString(Text);
                        Vector2 textPosition = new Vector2(centerPosition.X - size.X / 2,
                            centerPosition.Y - size.Y / 2);
                        spriteBatch.DrawString(CardGame.Font, Text, textPosition, TextColor);
                    }
                }
                // Draw at the component's position if there is no destination
                else
                {
                    // Position + origin to center the rotation point
                    Vector2 centerPosition = CurrentPosition + origin;

                    spriteBatch.Draw(CurrentFrame, centerPosition, CurrentSegment, Color.White,
                        CurrentRotation, origin, 1f, SpriteEffects.None, 0f);

                    if (Text != null)
                    {
                        Vector2 size = CardGame.Font.MeasureString(Text);
                        Vector2 textPosition = new Vector2(centerPosition.X - size.X / 2,
                            centerPosition.Y - size.Y / 2);
                        spriteBatch.DrawString(CardGame.Font, Text, textPosition, TextColor);
                    }
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Adds an animation to the animated component.
        /// </summary>
        /// <param name="animation">The animation to add.</param>
        public void AddAnimation(AnimatedGameComponentAnimation animation)
        {
            animation.Component = this;
            runningAnimations.Add(animation);
        }

        /// <summary>
        /// Calculate the estimated time at which the longest lasting animation currently managed 
        /// will complete.
        /// </summary>
        /// <returns>The estimated time for animation complete </returns>
        public virtual TimeSpan EstimatedTimeForAnimationsCompletion()
        {
            TimeSpan result = TimeSpan.Zero;

            if (IsAnimating)
            {
                for (int animationIndex = 0; animationIndex < runningAnimations.Count; animationIndex++)
                {
                    if (runningAnimations[animationIndex].EstimatedTimeForAnimationCompletion > result)
                    {
                        result = runningAnimations[animationIndex].EstimatedTimeForAnimationCompletion;
                    }
                }
            }

            return result;
        }
    }
}