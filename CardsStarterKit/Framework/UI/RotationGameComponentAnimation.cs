//-----------------------------------------------------------------------------
// RotationGameComponentAnimation.cs
//
// Animation which rotates a component over time
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;

namespace CardsFramework
{
    /// <summary>
    /// An animation which rotates a component from one angle to another.
    /// </summary>
    public class RotationGameComponentAnimation : AnimatedGameComponentAnimation
    {
        private float percent = 0;
        private float beginRotation;
        private float rotationDelta;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="beginRotation">The initial rotation angle in radians.</param>
        /// <param name="endRotation">The eventual rotation angle in radians.</param>
        public RotationGameComponentAnimation(float beginRotation, float endRotation)
        {
            this.beginRotation = beginRotation;
            rotationDelta = endRotation - beginRotation;
        }

        /// <summary>
        /// Runs the rotation animation.
        /// </summary>
        /// <param name="gameTime">Game time information.</param>
        public override void Run(GameTime gameTime)
        {
            if (IsStarted())
            {
                // Calculate the completion percent of animation
                percent += (float)(gameTime.ElapsedGameTime.TotalSeconds / Duration.TotalSeconds);
                percent = MathHelper.Clamp(percent, 0f, 1f);

                // Smoothly interpolate rotation
                Component.CurrentRotation = beginRotation + rotationDelta * percent;
            }
        }
    }
}
