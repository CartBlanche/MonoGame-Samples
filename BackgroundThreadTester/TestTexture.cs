using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BackgroundThreadTester
{
	public class TestTexture : DrawableGameComponent
	{
		Texture2D texture;
		int x, y;
		static Random random = new Random();
		Game1 game;
		SpriteBatch spriteBatch;
		Vector2 position;

		public TestTexture (Game1 game) : base (game)
		{
			this.game = game;
		}

		public override void Initialize ()
		{

			base.Initialize ();
		}

		protected override void LoadContent ()
		{

			texture = game.Content.Load<Texture2D>("beehive");

			// Create a random position
			x = random.Next(0, this.game.GetBackBufferWidth () - texture.Width);
			y = random.Next(0, this.game.GetBackBufferHeight () - texture.Height);
			position = new Vector2(x,y);

			// Create a new spritebatch
			spriteBatch = new SpriteBatch(this.Game.GraphicsDevice);
		}

		public override void Draw (GameTime gameTime)
		{
			spriteBatch.Begin();
			spriteBatch.Draw(texture, position, Color.White);
			spriteBatch.End();
		}
	}
}

