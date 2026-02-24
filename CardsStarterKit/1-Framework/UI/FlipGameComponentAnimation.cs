//-----------------------------------------------------------------------------
// FlipGameComponentAnimation.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    public class FlipGameComponentAnimation : AnimatedGameComponentAnimation
    {
        protected int percent = 0;
        public bool IsFromFaceDownToFaceUp = true;

        /// <summary>
        /// Runs the flip animation, which makes the component appear as if it has
        /// been flipped.
        /// </summary>
        /// <param name="gameTime">Game time information.</param>
        public override void Run(GameTime gameTime)
        {
            Texture2D texture;
            if (IsStarted())
            {
                if (IsDone())
                {
                    // Finish the animation
                    Component.IsFaceDown = !IsFromFaceDownToFaceUp;
                    Component.CurrentDestination = null;
                    Component.Color = Color.White;
                    Component.DrawShadow = false;
                }
                else
                {
                    texture = Component.CurrentFrame;
                    if (texture != null)
                    {
                        // Calculate the completion percent of the animation
                        percent += (int)(((gameTime.ElapsedGameTime.TotalMilliseconds /
                            (Duration.TotalMilliseconds / AnimationCycles)) * 100));

                        if (percent >= 100)
                        {
                            percent = 0;
                        }

                        int currentPercent;
                        if (percent < 50)
                        {
                            // On the first half of the animation the component is
                            // on its initial size
                            currentPercent = percent;
                            Component.IsFaceDown = IsFromFaceDownToFaceUp;
                        }
                        else
                        {
                            // On the second half of the animation the component
                            // is flipped
                            currentPercent = 100 - percent;
                            Component.IsFaceDown = !IsFromFaceDownToFaceUp;
                        }

                        // Using trickery and jiggery pokery instead of shaders.

                        // Shrink and widen the component to look like it is flipping
                        // Use the card draw scale so the flip animates from the correct visual size
                        float drawScale = Component.IsCard ? AnimationConstants.CardDrawScaleMultiplier : AnimationConstants.ChipDrawScaleMultiplier;
                        int scaledWidth = (int)(texture.Width * drawScale);
                        int scaledHeight = (int)(texture.Height * drawScale);

                        float widthPercent = (float)currentPercent / 100f;
                        int newWidth = (int)(scaledWidth - scaledWidth * widthPercent * 2);
                        int xOffset = (int)(Component.CurrentPosition.X + scaledWidth * widthPercent)
                                      - (scaledWidth - texture.Width) / 2;

                        // 1. Add vertical scale bulge at midpoint (5% taller)
                        float normalizedPercent = (float)percent / 100f;
                        float verticalBulge = 1f + (0.05f * (1f - Math.Abs(normalizedPercent - 0.5f) * 2f));
                        int newHeight = (int)(scaledHeight * verticalBulge);
                        int yOffset = (int)(Component.CurrentPosition.Y - (newHeight - scaledHeight) / 2f)
                                      - (scaledHeight - texture.Height) / 2;

                        // 2. Modulate color brightness during flip (darker at edges when "angled away")
                        float brightness = 1f - (Math.Abs(normalizedPercent - 0.5f) * 0.3f);
                        Component.Color = Color.White * brightness;

                        // 3. Add shadow beneath card that shifts during flip
                        Component.DrawShadow = true;
                        float shadowShift = Math.Abs(normalizedPercent - 0.5f) * 4f;
                        Component.ShadowOffset = new Vector2(8f + shadowShift, 10f);

                        Component.CurrentDestination = new Rectangle(xOffset, yOffset, newWidth, newHeight);
                    }
                }
            }
        }
    }
}