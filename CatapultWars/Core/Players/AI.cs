//-----------------------------------------------------------------------------
// AI.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// AI.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    class AI : Human
    {
        Random random;

        // The AI pulls back for a moment before firing, so the player can watch it settle on a shot
        bool settling;
        float settleTime;
        float settleDuration;
        float finalStrength;
        float finalAngle;

        public AI(Game game)
            : base(game)
        {
        }

        public AI(Game game, SpriteBatch screenSpriteBatch)
            : base(game, screenSpriteBatch, PlayerSide.Right)
        {
            Catapult = new Catapult(game, screenSpriteBatch,
                            "Textures/Catapults/Red/redIdle/redIdle",
                            Battlefield.RightCatapult, SpriteEffects.FlipHorizontally, true);
        }

        public override void Initialize()
        {
            //Initialize randomizer
            random = new Random();
			IsAI = true;
            //Catapult.Initialize();

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // Check if it is time to take a shot
            if (Catapult.CurrentState == CatapultState.Aiming &&
                !Catapult.AnimationRunning)
            {
                float shotVelocity, shotAngle;
                PickShot(out shotVelocity, out shotAngle);

                // Same scale as a person's pull, but never zero: the pull-back animation needs a frame to stop on
                finalStrength = Math.Max((shotVelocity - MinShotStrength) / (MaxShotStrength - MinShotStrength), 0.06f);
                finalAngle = shotAngle;
                Catapult.ShotStrength = finalStrength;
                Catapult.ShotVelocity = shotVelocity;
                Catapult.ShotAngle = shotAngle;

                // About as long as the pull-back animation plus the pause before the shot
                settleDuration = 18 * finalStrength / Catapult.AnimationFramesPerSecond + Catapult.AIStallSeconds - 0.05f;
                settleTime = 0;
                settling = true;
                isDragging = true;
            }

            if (settling)
            {
                if (Catapult.CurrentState != CatapultState.Aiming && Catapult.CurrentState != CatapultState.Stalling)
                {
                    // Fired
                    settling = false;
                    isDragging = false;
                    angleOverride = strengthOverride = null;
                }
                else
                {
                    settleTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    float t = MathHelper.Clamp(settleTime / settleDuration, 0f, 1f);
                    float eased = t * t * (3f - 2f * t);

                    // Ease towards the shot with a wobble that dies away, like a hand finding the mark
                    float wobble = (1f - eased) * (float)Math.Sin(settleTime * 11f);
                    strengthOverride = MathHelper.Clamp(finalStrength * eased + 0.06f * wobble, 0f, 1f);
                    angleOverride = MathHelper.Clamp(
                        MathHelper.Lerp(Ballistics.DefaultAngle, finalAngle, eased) + 0.12f * wobble,
                        Ballistics.MinAngle, Ballistics.MaxAngle);
                    ArrowScale = 0.93f * strengthOverride.Value;
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Tries a handful of angles on paper, with the current wind and the hill in the way,
        /// and takes the strength that lands closest to an aim point near the enemy. The aim
        /// point is deliberately off by up to 140 pixels, so the AI misses as often as a person.
        /// </summary>
        void PickShot(out float bestVelocity, out float bestAngle)
        {
            float targetX = Catapult.EnemyCentre.X + random.Next(-140, 141);
            bestVelocity = random.Next((int)MinShotStrength, (int)MaxShotStrength);
            bestAngle = Ballistics.DefaultAngle;
            float bestError = float.MaxValue;

            for (int attempt = 0; attempt < 8 && bestError > 25f; attempt++)
            {
                float angle = MathHelper.ToRadians(random.Next(30, 71));
                for (float v = MinShotStrength; v <= MaxShotStrength; v += 2f)
                {
                    float landing = Ballistics.LandingX(Catapult.ProjectileStart, Catapult.FireDirection,
                        v, angle, Catapult.Wind);
                    float error = Math.Abs(landing - targetX);
                    if (error < bestError)
                    {
                        bestError = error;
                        bestVelocity = v;
                        bestAngle = angle;
                    }
                }
            }
        }
    }
}
