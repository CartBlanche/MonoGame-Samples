//-----------------------------------------------------------------------------
// TransitionGameComponentAnimation.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace CardsFramework
{
    /// <summary>
    /// An animation which moves a component from one point to the other.
    /// </summary>
    public class TransitionGameComponentAnimation : AnimatedGameComponentAnimation
    {
        Vector2 sourcePosition;
        Vector2 positionDelta;
        float percent = 0;
        Vector2 destinationPosition;

        /// <summary>
        /// The CurrentScale value at the start of the animation (default 1.0).
        /// </summary>
        public float StartScale { get; set; } = 1.0f;

        /// <summary>
        /// The CurrentScale value to reach at the end of the animation (default 1.0).
        /// Override to match the visual size of the landing target, e.g. 1/ChipDrawScaleMultiplier
        /// when returning chips to the selector so they appear full-size on arrival.
        /// </summary>
        public float FinalScale { get; set; } = 1.0f;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="sourcePosition">The source position.</param>
        /// <param name="destinationPosition">The destination position.</param>
        public TransitionGameComponentAnimation(Vector2 sourcePosition,
            Vector2 destinationPosition)
        {
            this.destinationPosition = destinationPosition;
            this.sourcePosition = sourcePosition;
            positionDelta = destinationPosition - sourcePosition;
        }

        /// <summary>
        /// Runs the transition animation.
        /// </summary>
        /// <param name="gameTime">Game time information.</param>
        public override void Run(GameTime gameTime)
        {
            if (IsStarted())
            {
                // Calculate the animation's completion percentage.
                percent += (float)(gameTime.ElapsedGameTime.TotalSeconds / Duration.TotalSeconds);
                percent = MathHelper.Clamp(percent, 0f, 1f);

                // Apply cubic ease-out for smooth sliding
                float easedPercent = EaseOutCubic(percent);
                Component.CurrentPosition = sourcePosition + positionDelta * easedPercent;

                // Enable shadow and calculate dynamic offset based on animation progress
                // Shadow peaks at midpoint (halfway through the motion)
                Component.DrawShadow = true;
                float shadowIntensity = 1f - Math.Abs(percent - 0.5f) * 2f;
                float baseOffset = 8f;
                float shadowOffset = baseOffset + (12f * shadowIntensity); // Ranges from 8 to 20 pixels
                Component.ShadowOffset = new Vector2(shadowOffset, shadowOffset);

                // Scale up during motion, peaking at temporal midpoint (creates "swooping" effect)
                // Base interpolates from StartScale to FinalScale; swoop bump added on top.
                // Use raw percent (not eased) so the peak is centred in time, not front-loaded
                float scaleIntensity = 1f - Math.Abs(percent - 0.5f) * 2f;
                float baseScale = MathHelper.Lerp(StartScale, FinalScale, percent);
                Component.CurrentScale = baseScale + (0.4f * scaleIntensity);

                if (IsDone())
                {
                    Component.CurrentPosition = destinationPosition;
                    Component.DrawShadow = false;
                    Component.CurrentScale = FinalScale;
                }
            }

        }

        /// <summary>
        /// Cubic ease-out function for smooth animation.
        /// </summary>
        private float EaseOutCubic(float t)
        {
            return 1f - (float)Math.Pow(1f - t, 3);
        }
    }
}