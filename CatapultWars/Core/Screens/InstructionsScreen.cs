//-----------------------------------------------------------------------------
// BackgroundScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// using System.Threading;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;

namespace CatapultGame
{
	class InstructionsScreen : GameScreen
	{
		Texture2D background;
		SpriteFont font;
		// Background threaded loading removed; we load on the main thread.

		public InstructionsScreen ()
		{
			EnabledGestures = GestureType.Tap;

			TransitionOnTime = TimeSpan.FromSeconds (0);
			TransitionOffTime = TimeSpan.FromSeconds (0.5);
		}

		public override void LoadContent ()
		{
			background = Load<Texture2D> ("Textures/Backgrounds/instructions");
			font = Load<SpriteFont> ("Fonts/MenuFont");
		}

		public override void Update (GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update (gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		public override void HandleInput (InputState input)
		{
			PlayerIndex player;
			if (input.IsNewKeyPress (Keys.Space, ControllingPlayer, out player) ||
			    input.IsNewKeyPress (Keys.Enter, ControllingPlayer, out player) ||
			    input.MouseGesture.HasFlag(MouseGestureType.LeftClick) ||
			    input.IsNewButtonPress (Buttons.Start, ControllingPlayer, out player)) {
				// Start gameplay immediately on the main thread
				ExitScreen();
				ScreenManager.AddScreen(new GameplayScreen(), null);
			}

			foreach (var gesture in input.Gestures) {
				if (gesture.GestureType == GestureType.Tap) {
					ExitScreen();
					ScreenManager.AddScreen(new GameplayScreen(), null);
				}
			}

			base.HandleInput (input);
		}

		public override void Draw (GameTime gameTime)
		{
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

			spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

			// Draw Background
			spriteBatch.Draw (background, new Vector2 (0, 0),
					new Color (255, 255, 255, TransitionAlpha));



			spriteBatch.End ();
		}
	}
}
