using System;
using System.Collections.Generic;

#if ANDROID
using Android.App;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Samples.Draw2D
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;		
		Texture2D texture;
		SpriteFont font;
		float size, rotation;
		float clippingSize = 0.0f;
		Color alphaColor = Color.White;
		FPSCounterComponent fps;
		
        public Game1 ()  
		{
			graphics = new GraphicsDeviceManager (this);
			
			Content.RootDirectory = "Content";
			
			graphics.PreferMultiSampling = true;
#if ANDROID || IPHONE || PSS
			graphics.IsFullScreen = true;
#else
			graphics.IsFullScreen = false;
#endif
			
			graphics.PreferredBackBufferHeight = 480;
			graphics.PreferredBackBufferWidth = 320;

			graphics.SupportedOrientations = DisplayOrientation.Portrait | DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight | DisplayOrientation.Portrait;			
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

			// TODO: use this.Content to load your game content here
			texture = Content.Load<Texture2D> ("logo");
			font = Content.Load<SpriteFont> ("Font");

			fps = new FPSCounterComponent (this,spriteBatch,font);
			Components.Add(fps);
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update (GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
			{
				Exit();
			}
			// TODO: Add your update logic here
			size += 0.5f;
			if (size > 150) {
				size = 0;
			}

			rotation += 0.1f;
			if (rotation > MathHelper.TwoPi) {
				rotation = 0.0f;
			}

			clippingSize += 0.5f;
			if (clippingSize > graphics.GraphicsDevice.Viewport.Width) {
				clippingSize = 0.0f;
			}

			alphaColor.A ++;

			base.Update (gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw (GameTime gameTime)
		{
			graphics.GraphicsDevice.Clear (Color.CornflowerBlue);
            
			// Draw without blend
			spriteBatch.Begin (SpriteSortMode.Deferred, BlendState.Opaque);			
			spriteBatch.Draw (texture, new Vector2 (250,20), Color.White);	
			spriteBatch.End ();

			// Draw with additive blend
			spriteBatch.Begin (SpriteSortMode.Deferred, BlendState.Additive);
			spriteBatch.Draw (texture, new Vector2 (250,110), Color.White);
			spriteBatch.Draw (texture, new Vector2 (260,120), Color.White);
			spriteBatch.End ();
            
			spriteBatch.Begin ();

			// Normal draw
			// TODO spriteBatch.Draw (ball, new Vector2 (200,300), Color.White);	
			// TODO spriteBatch.Draw (ball, new Vector2 (200,300), null, Color.Yellow, 0.0f, new Vector2 (5,5), 1.0f, SpriteEffects.None, 0);	
            
			// Normal draw
			spriteBatch.Draw (texture, new Vector2 (10,390), Color.White);		
			// Draw stretching
			spriteBatch.Draw (texture, new Rectangle (0,0,(int)size,(int)size), Color.White);	
			// Draw with Filter Color
			spriteBatch.Draw (texture, new Vector2 (120,120), Color.Red);
			// Draw rotated
			spriteBatch.Draw (texture, new Rectangle (100,300,texture.Width,texture.Height), null, Color.White, rotation, new Vector2 (texture.Width / 2,texture.Height / 2), SpriteEffects.None, 0);	
			// Draw texture section
			spriteBatch.Draw (texture, new Vector2 (200,200), new Rectangle (20,20,40,40), Color.White);		
			// Draw texture section and scale
			spriteBatch.Draw (texture, new Rectangle (10,120,(int)size,(int)size), new Rectangle (20,20,40,40), Color.White);		
			// Alpha blending
			spriteBatch.Draw (texture, new Vector2 (140,0), alphaColor);	
			// Flip horizontaly
			spriteBatch.Draw (texture, new Rectangle (80,390,texture.Width,texture.Height), null, Color.White, 0.0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0);	
			// Flip verticaly
			spriteBatch.Draw (texture, new Rectangle (150,390,texture.Width,texture.Height), null, Color.White, 0.0f, Vector2.Zero, SpriteEffects.FlipVertically, 0);	
			// Flip horizontaly and verticaly
			spriteBatch.Draw (texture, new Rectangle (220,390,texture.Width,texture.Height), null, Color.White, 0.0f, Vector2.Zero, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);	
			
			// Scale
			spriteBatch.Draw (texture, new Vector2 (290,390), null, Color.White, 0.0f, Vector2.Zero, new Vector2(0.5f, 1.6f), SpriteEffects.None,0.0f);
			
            
			base.Draw (gameTime);

			spriteBatch.End ();
            
			// Now let's try some scissoring
			//Disabled as this scissoring code is /wrong/
			/*spriteBatch.Begin ();

			spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle (50, 40, (int)clippingSize, (int)clippingSize);
			spriteBatch.GraphicsDevice.RasterizerState.ScissorTestEnable = true;

			spriteBatch.Draw (texture, new Rectangle (50, 40, 320, 40), Color.White);
			spriteBatch.DrawString (font, "Scissor Clipping Test", new Vector2 (50, 40), Color.Red);
			
			spriteBatch.End ();

			spriteBatch.GraphicsDevice.RasterizerState.ScissorTestEnable = false;*/
            

		}
	}
}
