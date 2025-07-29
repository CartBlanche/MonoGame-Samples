using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Microsoft.Xna.Samples.BouncingBox
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;		
		Texture2D texture;
		Vector2 position;
		Vector2 speed;
		Random randomizer;
		Color backColor;

		public Game1()
		{
			randomizer = new Random(DateTime.Now.TimeOfDay.Milliseconds);
			speed = new Vector2(5 + randomizer.Next(10), 5 + randomizer.Next(10));
			position = new Vector2(250, 400);
			GetNewColor();

			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			
#if __MOBILE__
			graphics.IsFullScreen = true;	
#endif
		}

		private void GetNewColor ()
		{
			backColor = new Color (randomizer.Next (255), randomizer.Next (255), randomizer.Next (255), 255);
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
			spriteBatch = new SpriteBatch (GraphicsDevice);

			// Load the texture from the file in the Content directory
			try 
			{
				texture = Content.Load<Texture2D> ("monogameicon");
			}
			catch
			{
				// If that fails, create a simple colored rectangle texture
				texture = new Texture2D(GraphicsDevice, 64, 64);
				Color[] data = new Color[64 * 64];
				for (int i = 0; i < data.Length; i++)
				{
					data[i] = Color.White;
				}
				texture.SetData(data);
			}
		}
		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update (GameTime gameTime)
		{
			KeyboardState keyState = Keyboard.GetState();
			
            // Allows the game to exit
            if (keyState.IsKeyDown(Keys.Escape)
            || GamePad.GetState(PlayerIndex.One).Buttons.Back ==
                ButtonState.Pressed)
#if !__IOS__
				Exit();
#endif

			if (texture != null) {
				//  Keep inside the screen
				//  right
				if (position.X + texture.Width + speed.X > Window.ClientBounds.Width) {
					GetNewColor ();
					speed.X = -speed.X;
				}
				//  bottom
				if (position.Y + texture.Height + speed.Y > Window.ClientBounds.Height) {
					GetNewColor ();
					speed.Y = -speed.Y;
				}
				//  left
				if (position.X + speed.X < 0) {
					GetNewColor ();
					speed.X = -speed.X;
				}
				//  top
				if (position.Y + speed.Y < 0) {
					GetNewColor ();
					speed.Y = -speed.Y;
				}
				//  update position
				position += speed;

			}
			base.Update (gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw (GameTime gameTime)
		{
			graphics.GraphicsDevice.Clear (backColor);

			spriteBatch.Begin ();
			if (texture != null)
				spriteBatch.Draw (texture, position, Color.White);			
			spriteBatch.End ();

			base.Draw (gameTime);
		}
	}
}
