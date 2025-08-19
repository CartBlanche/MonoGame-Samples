//-----------------------------------------------------------------------------
// CatapultGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices; // Not available in MonoGame 3.8
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;

namespace CatapultGame
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class CatapultGame : Game
	{
		GraphicsDeviceManager graphicsDeviceManager;
		ScreenManager screenManager;
		public ScreenManager ScreenManager { get => screenManager; set => screenManager = value; }

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

		public CatapultGame()
		{
			graphicsDeviceManager = new GraphicsDeviceManager(this);
			//graphics.SynchronizeWithVerticalRetrace = false;

			graphicsDeviceManager.PreferredBackBufferWidth = ScreenManager.BASE_BUFFER_WIDTH;
			graphicsDeviceManager.PreferredBackBufferHeight = ScreenManager.BASE_BUFFER_HEIGHT;

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

			Content.RootDirectory = "Content";

			// Frame rate is 30 fps by default for Windows Phone.
			TargetElapsedTime = TimeSpan.FromTicks(333333);

			//Create a new instance of the Screen Manager
			screenManager = new ScreenManager(this);
			Components.Add(screenManager);

			Components.Add(new MessageDisplayComponent(this));
			// Components.Add (new GamerServicesComponent (this)); // Not available in MonoGame 3.8

			//Add two new screens
			screenManager.AddScreen(new BackgroundScreen(), null);
			screenManager.AddScreen(new MainMenuScreen(), null);

			// Listen for invite notification events.
			NetworkSession.InviteAccepted += (sender, e) => NetworkSessionComponent.InviteAccepted(screenManager, e);

			AudioManager.Initialize(this);
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
		}

		protected override void LoadContent()
		{
			AudioManager.LoadSounds();
			base.LoadContent();
		}
	}
}