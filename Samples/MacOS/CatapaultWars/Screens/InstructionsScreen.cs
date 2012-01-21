#region File Description
//-----------------------------------------------------------------------------
// BackgroundScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GameStateManagement;
using Microsoft.Xna.Framework.Input.Touch;

#if MACOS
using MonoMac.AppKit;
using MonoMac.Foundation;
#endif

#if IOS
using MonoTouch.UIKit;
using MonoTouch.Foundation;
#endif
#endregion

namespace CatapultGame
{
	class InstructionsScreen : GameScreen
	{
#region Fields
		Texture2D background;
		SpriteFont font;
		bool isLoading;
		GameplayScreen gameplayScreen;
		System.Threading.Thread thread;
#endregion

#region Initialization
		public InstructionsScreen ()
		{
			EnabledGestures = GestureType.Tap;

			TransitionOnTime = TimeSpan.FromSeconds (0);
			TransitionOffTime = TimeSpan.FromSeconds (0.5);
		}
#endregion

#region Loading
		public override void LoadContent ()
		{
			background = Load<Texture2D> ("Textures/Backgrounds/instructions");
			font = Load<SpriteFont> ("Fonts/MenuFont");
		}
#endregion

		public override void Update (GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			// If additional thread is running, skip
			if (null != thread) {
				// If additional thread finished loading and the screen is not exiting
				if (thread.ThreadState == System.Threading.ThreadState.Stopped && !IsExiting) {
					isLoading = false;

					// Exit the screen and show the gameplay screen 
					// with pre-loaded assets
					ExitScreen ();
					ScreenManager.AddScreen (gameplayScreen, null);
				}
			}
			base.Update (gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

#region Handle input
		public override void HandleInput (InputState input)
		{
			if (isLoading == true) {
				base.HandleInput (input);
				return;
			}
			PlayerIndex player;
			if (input.IsNewKeyPress (Microsoft.Xna.Framework.Input.Keys.Space, ControllingPlayer, out player) ||
			    input.IsNewKeyPress (Microsoft.Xna.Framework.Input.Keys.Enter, ControllingPlayer, out player) ||
			    input.MouseGesture.HasFlag(MouseGestureType.LeftClick)||
			    input.IsNewButtonPress (Microsoft.Xna.Framework.Input.Buttons.Start, ControllingPlayer, out player)) {
				// Create a new instance of the gameplay screen
				gameplayScreen = new GameplayScreen ();
				gameplayScreen.ScreenManager = ScreenManager;

				// Start loading the resources in additional thread
#if MACOS
				// create a new thread using BackgroundWorkerThread as method to execute
				thread = new Thread (LoadAssetsWorkerThread as ThreadStart);
#else
				thread = new System.Threading.Thread (new System.Threading.ThreadStart (gameplayScreen.LoadAssets));
#endif
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

		void LoadAssetsWorkerThread ()
		{

#if MACOS || IOS			
			// Create an Autorelease Pool or we will leak objects.
			using (var pool = new NSAutoreleasePool()) {
#else				
				
#endif				
				// Make sure we invoke this on the Main Thread or OpenGL will throw an error
#if MACOS
				MonoMac.AppKit.NSApplication.SharedApplication.BeginInvokeOnMainThread (delegate {
#endif
#if IOS
				var invokeOnMainThredObj = new NSObject();
				invokeOnMainThredObj.InvokeOnMainThread(delegate {
#endif
					gameplayScreen.LoadAssets ();
#if MACOS || IOS						
				});
					
			}				
#endif				

		}
#endregion

#region Render
		public override void Draw (GameTime gameTime)
		{
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

			spriteBatch.Begin ();

			// Draw Background
			spriteBatch.Draw (background, new Vector2 (0, 0), new Color (255, 255, 255, TransitionAlpha));

			// If loading gameplay screen resource in the 
			// background show "Loading..." text
			if (isLoading) {
				string text = "Loading...";
				Vector2 size = font.MeasureString (text);
				Vector2 position = new Vector2 ((ScreenManager.GraphicsDevice.Viewport.Width - size.X) / 2,
				                                (ScreenManager.GraphicsDevice.Viewport.Height - size.Y) / 2);
				spriteBatch.DrawString (font, text, position, Color.Black);
				spriteBatch.DrawString (font, text, position - new Vector2 (-4, 4), new Color (255f, 150f, 0f));
			}

			spriteBatch.End ();
		}
#endregion
	}
}
