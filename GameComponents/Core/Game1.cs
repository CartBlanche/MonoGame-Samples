using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameComponents
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;		
		Texture2D texture;
		Random randomizer;
		SpriteFont font;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
			
			randomizer = new Random(DateTime.Now.TimeOfDay.Milliseconds);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            texture = Content.Load<Texture2D>("logo");
			font = Content.Load<SpriteFont>("Font");
						
			for (int i=0;i<50;i++)
				AddSprite();
			
			Components.Add(new FPSCounterComponent(this,spriteBatch,font));
        }

		private void AddSprite()
		{
			Vector2 speed = new Vector2(5+randomizer.Next(10),5+randomizer.Next(10));
			Vector2 position = new Vector2(randomizer.Next(260),randomizer.Next(400));
			Components.Add(new Sprite(this,texture,position, speed, spriteBatch));
		}
	

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // TODO: Add your update logic here
            
            KeyboardState currentKeyboardState = Keyboard.GetState();
			GamePadState currentGamePadState = GamePad.GetState (PlayerIndex.One);

			// Check for exit.
			if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
				currentGamePadState.Buttons.Back == ButtonState.Pressed)
			{
#if !___IOS___
				Exit();
#endif
			}

            var mouseState = Mouse.GetState();
			
			if (mouseState.LeftButton == ButtonState.Pressed ||
                currentGamePadState.Buttons.A == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.Space))
            {
                AddSprite();
                //Mouse.SetPosition(0,0);
            }
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
           	graphics.GraphicsDevice.Clear(Color.MonoGameOrange);
			
			spriteBatch.Begin();
						
			base.Draw(gameTime);
			
			spriteBatch.DrawString(font,"Tap/Click/Space/A button to add a new sprite",new Vector2(0,25),Color.White);
			spriteBatch.DrawString(font,"Sprite count: " + (Components.Count-1).ToString(),new Vector2(150,0),Color.White);


			spriteBatch.End();  			
        }
    }
}
