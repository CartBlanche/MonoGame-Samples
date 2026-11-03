using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Tile_Engine;


namespace Gorillas
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Engine : Microsoft.Xna.Framework.Game
	{
		// Graphics
		GraphicsDeviceManager graphics;

		// SpriteBatch instance used to render all the effects.
		SpriteBatch spriteBatch;
		
		ScreenManager screenManager;		
		StreamWriter writer;

		// By preloading any assets used by UI rendering, we avoid framerate glitches
		// when they suddenly need to be loaded in the middle of a menu transition.
		static readonly string[] preloadAssets = 
		{
			"gradient",
			"Network/cat",
			"Network/chat_ready",
			"Network/chat_able",
			"Network/chat_talking",
			"Network/chat_mute",
		};

		public Engine ()
		{
			graphics = new GraphicsDeviceManager (this);
			Content.RootDirectory = "Content";
			
			// Create the screen manager component.
			screenManager = new ScreenManager (this);

			Components.Add (screenManager);
			Components.Add (new MessageDisplayComponent (this));
			Components.Add (new GamerServicesComponent(this));

			// Activate the first screens.
			screenManager.AddScreen (new BackgroundScreen (), null);
			//screenManager.AddScreen (new MainMenuScreen (), null);
			screenManager.AddScreen (new IntroScreen (), null);
			
//			//
//			FileStream ostrm;
//			//StreamWriter writer;
//			TextWriter oldOut = Console.Out;
//			try
//			{
//			ostrm = new FileStream (Path.Combine(NSBundle.MainBundle.ResourcePath, "../../../../log.txt"), FileMode.OpenOrCreate, FileAccess.Write);
//				
//			writer = new StreamWriter (ostrm);
//			}
//			catch (Exception e)
//			{
//			Console.WriteLine ("Cannot open Redirect.txt for writing");
//			Console.WriteLine (e.Message);
//			return;
//			}
//			Console.SetOut (writer);
//			Console.WriteLine ("This is a line of text");
//			Console.WriteLine ("Everything written to Console.Write() or");
//			Console.WriteLine ("Console.WriteLine() will be written to a file");
			//ostrm.Flush();
			//Console.SetOut (oldOut);
			//writer.Close();
			
			//ostrm.Close();
			
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize ()
		{
			Window.Title = "Gorillas";

			if (OperatingSystem.IsAndroid () || OperatingSystem.IsIOS ()) {
				// Full screen at the device's resolution; ScreenManager scales the 640x350 game to fit.
				var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
				graphics.PreferredBackBufferWidth = mode.Width;
				graphics.PreferredBackBufferHeight = mode.Height;
				graphics.IsFullScreen = true;
				graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
				this.IsMouseVisible = false;
			} else {
				// Desktop: start at 2x the virtual size and let the window be resized freely.
				graphics.PreferredBackBufferWidth = ScreenManager.BaseWidth * 2;
				graphics.PreferredBackBufferHeight = ScreenManager.BaseHeight * 2;
				Window.AllowUserResizing = true;
				this.IsMouseVisible = true;
			}
			graphics.ApplyChanges ();

			base.Initialize ();
		}


		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent ()
		{

			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch (GraphicsDevice);
			
			// Add the SpriteBatch service
			Services.AddService (typeof(SpriteBatch), spriteBatch);

			foreach (string asset in preloadAssets) {
				Content.Load<object> (asset);
			}

		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update (GameTime gameTime)
		{
			//writer.Flush();
			base.Update (gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw (GameTime gameTime)
		{
			GraphicsDevice.Clear (Color.Black);

			base.Draw (gameTime);
		}

	}
}