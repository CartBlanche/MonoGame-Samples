//-----------------------------------------------------------------------------
// Human.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Human.cs
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
using Microsoft.Xna.Framework.Input.Touch;

namespace CatapultGame
{
	enum PlayerSide
	{
		Left,
		Right
	}

	class Human : Player
	{
		// Drag variables to hold first and last gesture samples
		GestureSample? prevSample;
		GestureSample? firstSample;
		public bool IsAI { get; protected set; }
		public bool isDragging { get; set; }
		// Pull distance, in screen pixels, that gives full power. It was the whole portrait diagonal,
		// which left no room to pull back at an angle.
		readonly float maxDragDelta = 450f;
		// Textures & position & spriteEffects used for Catapult
		Texture2D arrow;
		// The AI shows its aim settling in before it fires; these replace the real values while it does
		protected float? angleOverride;
		protected float? strengthOverride;
		float arrowScale;
		Vector2 catapultPosition;
		PlayerSide playerSide;
		SpriteEffects spriteEffect = SpriteEffects.None;

		public Human (Game game) : base(game)
		{
		}

		public Human (Game game, SpriteBatch screenSpriteBatch, PlayerSide playerSide) : base(game, screenSpriteBatch)
		{
			string idleTextureName = "";
			this.playerSide = playerSide;

			if (playerSide == PlayerSide.Left) {
				catapultPosition = new Vector2 (140, 332);
				idleTextureName = "Textures/Catapults/Blue/blueIdle/blueIdle";
			} else {
				catapultPosition = new Vector2 (600, 332);
				spriteEffect = SpriteEffects.FlipHorizontally;
				idleTextureName = "Textures/Catapults/Red/redIdle/redIdle";
			}

			Catapult = new Catapult (game, screenSpriteBatch,
						idleTextureName, catapultPosition, spriteEffect,
						playerSide == PlayerSide.Left ? false : true);
		}

		public override void Initialize ()
		{
			arrow = curGame.Content.Load<Texture2D> ("Textures/HUD/Arrow");

			Catapult.Initialize ();

			base.Initialize ();
		}

		/// <summary>
		/// Function processes the user input
		/// </summary>
		/// <param name="gestureSample"></param>
		public void HandleInput (GestureSample gestureSample)
		{
			// Process input only if in Human's turn
			if (IsActive) {
				// Process any Drag gesture
				if (gestureSample.GestureType == GestureType.FreeDrag) {
					// If drag just began save the sample for future
					// calculations and start Aim "animation"
					if (null == firstSample) {
						firstSample = gestureSample;
						Catapult.CurrentState = CatapultState.Aiming;
					}

					// save the current gesture sample
					prevSample = gestureSample;

					// calculate the delta between first sample and current
					// sample to present visual sound on screen
					Vector2 delta = prevSample.Value.Position - firstSample.Value.Position;
					Aim (delta);
					isDragging = true;
				} else if (gestureSample.GestureType == GestureType.DragComplete) {
					// calc velocity based on delta between first and last
					// gesture samples
					if (null != firstSample) {
						Catapult.ShotVelocity = MinShotStrength + Catapult.ShotStrength *
						(MaxShotStrength - MinShotStrength);
						Catapult.Fire (Catapult.ShotVelocity, Catapult.ShotAngle);
						Catapult.CurrentState = CatapultState.Firing;
					}

					// turn off dragging state
					ResetDragState ();
				}
			}
		}

		Vector2? firstMouseSample = null;
		Vector2? prevMouseSample = null;

		public void HandleInput (InputState input)
		{
			// Process input only if in Human's turn
			if (IsActive && !IsAI) {

				if (input.MouseGesture.HasFlag (MouseGestureType.FreeDrag)) {
					// If drag just began save the sample for future
					// calculations and start Aim "animation"
					if (null == firstMouseSample) {
						firstMouseSample = input.MouseDragStartPosition;
						Catapult.CurrentState = CatapultState.Aiming;
					}

					// save the current gesture sample
					prevMouseSample = input.CurrentMousePosition;

					// calculate the delta between first sample and current
					// sample to present visual sound on screen
					Vector2 delta = (Vector2)prevMouseSample - (Vector2)firstMouseSample;
					Aim (delta);
					isDragging = true;
				} else if (input.MouseGesture.HasFlag (MouseGestureType.DragComplete)) {
					// calc velocity based on delta between first and last
					// gesture samples
					if (null != firstMouseSample) {
						Catapult.ShotVelocity = MinShotStrength + Catapult.ShotStrength *
						(MaxShotStrength - MinShotStrength);
						Catapult.Fire (Catapult.ShotVelocity, Catapult.ShotAngle);
						Catapult.CurrentState = CatapultState.Firing;
					}

					ResetDragState ();
				}
			}
		}


		public override void Draw (GameTime gameTime)
		{
			if (isDragging)
				DrawDragArrow (arrowScale);

			base.Draw (gameTime);
		}

		/// <summary>
		/// Turns a drag into a shot, slingshot style: the further you pull back the harder the
		/// shot, and it flies the opposite way to the pull. Pulling down and back lobs it high,
		/// pulling straight back fires it flat.
		/// </summary>
		void Aim (Vector2 delta)
		{
			float length = delta.Length ();
			Catapult.ShotStrength = Math.Min (length / maxDragDelta, 1f);
			// The arrow is as long as it used to be at full power when the pull is at full power
			arrowScale = 0.93f * Catapult.ShotStrength;

			// Ignore a tiny wobble, and keep the default angle until the pull means something
			if (length < 8)
				return;

			// Forward is towards the enemy; pulling down means aiming up
			float forward = (playerSide == PlayerSide.Left ? -delta.X : delta.X);
			float up = delta.Y;
			float angle = (float)Math.Atan2 (up, forward);
			Catapult.ShotAngle = MathHelper.Clamp (angle, Ballistics.MinAngle, Ballistics.MaxAngle);
		}

		public void DrawDragArrow (float arrowScale)
		{
			// The arrow starts at the middle of the catapult and points along the shot
			float angle = DisplayAngle;
			float rotation = playerSide == PlayerSide.Left ? -angle : angle - MathHelper.Pi;
			spriteBatch.Draw (arrow, Catapult.Centre, null,
					playerSide == PlayerSide.Left ? Color.Blue : Color.Red, rotation,
					new Vector2 (0, arrow.Height / 2f), new Vector2 (arrowScale, 0.1f),
					SpriteEffects.None, 0);
		}

		/// <summary>The aim as the player should see it right now, in radians above the horizon.</summary>
		public float DisplayAngle
		{
			get { return angleOverride ?? Catapult.ShotAngle; }
		}

		/// <summary>The pull as the player should see it right now, 0 to 1.</summary>
		public float DisplayStrength
		{
			get { return strengthOverride ?? Catapult.ShotStrength; }
		}

		/// <summary>
		/// Angle and power for the on-screen readout: live while the player is pulling back,
		/// and the last shot fired the rest of the time. Power is 0 to 100.
		/// </summary>
		public bool TryGetReadout (out int angleDegrees, out int powerPercent)
		{
			if (isDragging) {
				angleDegrees = (int)Math.Round (MathHelper.ToDegrees (DisplayAngle));
				powerPercent = (int)Math.Round (DisplayStrength * 100f);
				return true;
			}
			if (Catapult.HasFired) {
				angleDegrees = (int)Math.Round (MathHelper.ToDegrees (Catapult.LastFiredAngle));
				powerPercent = (int)Math.Round ((Catapult.LastFiredVelocity - MinShotStrength) /
							(MaxShotStrength - MinShotStrength) * 100f);
				return true;
			}
			angleDegrees = powerPercent = 0;
			return false;
		}

		// used to set the arrow scale from networking
		internal float ArrowScale
		{
			get { return arrowScale; }
			set { arrowScale = value; }
		}
		/// <summary>
		/// /// /// Turn off dragging state and reset drag related variables
		/// </summary>
		public void ResetDragState ()
		{
			firstSample = null;
			prevSample = null;
			firstMouseSample = null;
			prevMouseSample = null;
			isDragging = false;
			arrowScale = 0;
			Catapult.ShotStrength = 0;
		}
	}
}
