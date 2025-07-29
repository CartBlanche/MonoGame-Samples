using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace SoundSample
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class SoundGame : Game
	{
		GraphicsDeviceManager graphics;	
		KeyboardState oldSate;
		SoundEffect sound;
		SpriteBatch spriteBatch;
		SoundEffectInstance soundInstance;
		SpriteFont font;
		
		public SoundGame ()  
		{
			// Initialize fields to null to satisfy nullable checks
			sound = null!;
			spriteBatch = null!;
			soundInstance = null!;
			font = null!;
			graphics = new GraphicsDeviceManager (this);
			Content.RootDirectory = "Content";
			graphics.PreferMultiSampling = true;
			graphics.IsFullScreen = false;  
			Window.AllowUserResizing = false;
			graphics.SupportedOrientations = DisplayOrientation.Portrait | DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight; // PortraitUpsideDown not supported on all platforms
		}
		
		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize ()
		{
			// TODO: Add your initialization logic here
			
			base.Initialize ();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent ()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			font = Content.Load<SpriteFont>("Font");

			sound = Content.Load<SoundEffect>("Explosion1");
			soundInstance = sound.CreateInstance();
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update (GameTime gameTime)
		{
			KeyboardState ks = Keyboard.GetState();
			
#if ANDROID
			if (soundInstance.State != SoundState.Playing)
			{
				soundInstance.Volume = 1f;
				soundInstance.IsLooped = true;
				soundInstance.Play();
			}
#endif
			
			if (ks[Keys.Escape] == KeyState.Down)
#if !ANDROID && !IOS
				base.Exit();
#endif
			
			if (ks.IsKeyDown(Keys.A) && oldSate.IsKeyUp(Keys.A))
				soundInstance.Play();
			
			if (ks.IsKeyDown(Keys.B) && oldSate.IsKeyUp(Keys.B))
				soundInstance.Stop();
			
			if (ks.IsKeyDown(Keys.C) && oldSate.IsKeyUp(Keys.C))
				soundInstance.Pause();
			
			if (ks.IsKeyDown(Keys.D) && oldSate.IsKeyUp(Keys.D))
				soundInstance.IsLooped = !soundInstance.IsLooped;
			
			if (ks.IsKeyDown(Keys.E) && oldSate.IsKeyUp(Keys.E))
				soundInstance.Stop(true);			
			
			if (ks.IsKeyDown(Keys.X))
				soundInstance.Volume = MathHelper.Clamp(soundInstance.Volume + 0.01f, 0f, 1f);
			else if (ks.IsKeyDown(Keys.Z))
				soundInstance.Volume = MathHelper.Clamp(soundInstance.Volume - 0.01f, 0f, 1f);;
			
			oldSate = ks;

			base.Update (gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw (GameTime gameTime)
		{
			graphics.GraphicsDevice.Clear (Color.CornflowerBlue);

			spriteBatch.Begin();

			spriteBatch.DrawString(font, "A: play\nB: stop\nC: pause\nD: toggle looping\nE: immediate stop\nX/Z volume\nStatus: " +
								   soundInstance.State.ToString() + "\nLooping: " +
								   soundInstance.IsLooped.ToString() + "\nVolume: " +
								   soundInstance.Volume.ToString()
								   , Vector2.Zero, Color.White);
			
			base.Draw(gameTime);
			
			spriteBatch.End();
		}
	}
}
