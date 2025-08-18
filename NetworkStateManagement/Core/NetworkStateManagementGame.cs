//-----------------------------------------------------------------------------
// Game.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;


namespace NetworkStateManagement
{
	/// <summary>
	/// Sample showing how to manage the different game states involved in
	/// implementing a networked game, with menus for creating, searching,
	/// and joining sessions, a lobby screen, and the game itself. This main
	/// game class is extremely simple: all the interesting stuff happens
	/// in the ScreenManager component.
	/// </summary>
	public class NetworkStateManagementGame : Game
	{
		const int screenWidth = 480;
		const int screenHeight = 540;

		GraphicsDeviceManager graphicsDeviceManager;
		ScreenManager screenManager;


		// By preloading any assets used by UI rendering, we avoid framerate glitches
		// when they suddenly need to be loaded in the middle of a menu transition.
		static readonly string[] preloadAssets =
		{
			"gradient",
			"cat",
			"chat_ready",
			"chat_able",
			"chat_talking",
			"chat_mute",
		};

		/// <summary>
		/// The main game constructor.
		/// </summary>		
		public NetworkStateManagementGame()
		{
			Content.RootDirectory = "Content";

			graphicsDeviceManager = new GraphicsDeviceManager(this);
			graphicsDeviceManager.PreferredBackBufferWidth = screenWidth;
			graphicsDeviceManager.PreferredBackBufferHeight = screenHeight;

			if (UIUtility.IsMobile)
			{
				graphicsDeviceManager.IsFullScreen = true;
				IsMouseVisible = false;
			}
			else if (UIUtility.IsDesktop)
			{
				graphicsDeviceManager.IsFullScreen = false;
				IsMouseVisible = true;
			}
			else
			{
				throw new PlatformNotSupportedException();
			}

			// Create components.
			screenManager = new ScreenManager(this);

			Components.Add(screenManager);
			Components.Add(new MessageDisplayComponent(this));
			Components.Add(new GamerServicesComponent(this));

			// Activate the first screens.
			screenManager.AddScreen(new BackgroundScreen(), null);
			screenManager.AddScreen(new MainMenuScreen(), null);

			// Listen for invite notification events.
			NetworkSession.InviteAccepted += (sender, e) => NetworkSessionComponent.InviteAccepted(screenManager, e);

			// To test the trial mode behavior while developing your game,
			// uncomment this line:

			// Guide.SimulateTrialMode = true;
		}

		/// <summary>
		/// Loads graphics content.
		/// </summary>
		protected override void LoadContent()
		{
			foreach (string asset in preloadAssets)
			{
				Content.Load<object>(asset);
			}
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		protected override void Draw(GameTime gameTime)
		{
			graphicsDeviceManager.GraphicsDevice.Clear(Color.Black);

			// The real drawing happens inside the screen manager component.
			base.Draw(gameTime);
		}


	}
}