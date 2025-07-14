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
using GameStateManagement;

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
		// Constant for longest distance possible between drag points
		readonly float maxDragDelta = (new Vector2 (480, 800)).Length ();
		// Textures & position & spriteEffects used for Catapult
		Texture2D arrow;
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
					Catapult.ShotStrength = delta.Length () / maxDragDelta;
					float baseScale = 0.001f;
					arrowScale = baseScale * delta.Length ();
					isDragging = true;
				} else if (gestureSample.GestureType == GestureType.DragComplete) {
					// calc velocity based on delta between first and last
					// gesture samples
					if (null != firstSample) {
						Vector2 delta = prevSample.Value.Position - firstSample.Value.Position;
						Catapult.ShotVelocity = MinShotStrength + Catapult.ShotStrength *
									(MaxShotStrength - MinShotStrength);
						Catapult.Fire (Catapult.ShotVelocity);
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
					Catapult.ShotStrength = delta.Length () / maxDragDelta;
					float baseScale = 0.001f;
					arrowScale = baseScale * delta.Length ();
					isDragging = true;
				} else if (input.MouseGesture.HasFlag (MouseGestureType.DragComplete)) {
					// calc velocity based on delta between first and last
					// gesture samples
					if (null != firstMouseSample) {
						Vector2 delta = (Vector2)prevMouseSample - (Vector2)firstMouseSample;
						Catapult.ShotVelocity = MinShotStrength + Catapult.ShotStrength *
						    (MaxShotStrength - MinShotStrength);
						Catapult.Fire (Catapult.ShotVelocity);
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

		public void DrawDragArrow (float arrowScale)
		{
			if (playerSide == PlayerSide.Left) {
				spriteBatch.Draw (arrow, catapultPosition + new Vector2 (0, -40),
						null, Color.Blue, 0,
						Vector2.Zero, new Vector2 (arrowScale, 0.1f), spriteEffect, 0);
			} else {
				spriteBatch.Draw (arrow, catapultPosition + new Vector2 (-arrow.Width * arrowScale + 40, -40),
						null, Color.Red, 0,
						Vector2.Zero, new Vector2 (arrowScale, 0.1f), spriteEffect, 0);
			}
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
