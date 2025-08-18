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
using System.Threading;

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
		bool isLoading;
		GameplayScreen gameplayScreen;
		System.Threading.Thread thread;

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
			// If additional thread is running, skip
			if (null != thread) {
				// If additional thread finished loading and the screen is not exiting
				if (thread.ThreadState == System.Threading.ThreadState.Stopped  && !IsExiting) {
					isLoading = false;

					// Exit the screen and show the gameplay screen 
					// with pre-loaded assets
					ExitScreen ();
					ScreenManager.AddScreen (gameplayScreen, null);
				}
			}

			base.Update (gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		public override void HandleInput (InputState input)
		{
			if (isLoading == true)
            {
                // Exit the screen and show the gameplay screen 
					// with pre-loaded assets
				ExitScreen ();
				ScreenManager.AddScreen (gameplayScreen, null);
					
				base.HandleInput (input);
				return;
			}

			PlayerIndex player;
			if (input.IsNewKeyPress (Keys.Space, ControllingPlayer, out player) ||
			    input.IsNewKeyPress (Keys.Enter, ControllingPlayer, out player) ||
			    input.MouseGesture.HasFlag(MouseGestureType.LeftClick) ||
			    input.IsNewButtonPress (Buttons.Start, ControllingPlayer, out player)) {
				// Create a new instance of the gameplay screen
				gameplayScreen = new GameplayScreen ();
				gameplayScreen.ScreenManager = ScreenManager;

                // Start loading the resources in additional thread    
				thread = new System.Threading.Thread (new System.Threading.ThreadStart (gameplayScreen.LoadAssets));
				isLoading = true;
				// start it
				thread.Start ();
			}

			foreach (var gesture in input.Gestures) {
				if (gesture.GestureType == GestureType.Tap) {
					// Create a new instance of the gameplay screen
					gameplayScreen = new GameplayScreen ();
					gameplayScreen.ScreenManager = ScreenManager;
						
					// Start loading the resources in additional thread
					thread = new System.Threading.Thread (new System.Threading.ThreadStart (gameplayScreen.LoadAssets));
					isLoading = true;
					thread.Start ();				
				}
			}

			base.HandleInput (input);
		}

		public override void Draw (GameTime gameTime)
		{
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

			spriteBatch.Begin ();

			// Draw Background
			spriteBatch.Draw (background, new Vector2 (0, 0),
					new Color (255, 255, 255, TransitionAlpha));

			// If loading gameplay screen resource in the 
			// background show "Loading..." text
			if (isLoading) {
				string text = "Loading...";
				Vector2 size = font.MeasureString (text);
				Vector2 position = new Vector2 (
							(ScreenManager.GraphicsDevice.Viewport.Width - size.X) / 2,
							(ScreenManager.GraphicsDevice.Viewport.Height - size.Y) / 2);
				spriteBatch.DrawString (font, text, position, Color.Black);
				spriteBatch.DrawString (font, text, position - new Vector2 (-4, 4), new Color (255f, 150f, 0f));
			}

			spriteBatch.End ();
		}
	}
}
