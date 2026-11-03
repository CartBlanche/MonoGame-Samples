using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace Gorillas
{
	public class GameOver
	{
		GraphicsDevice graphics;
		SpriteBatch spriteBatch;
		ContentManager content;
		
		// font
		SpriteFont gameFont;
		
		// Game over color
		Color gameOverBackground = Color.AntiqueWhite;

		// game over position
		Vector2 gameOverPosition = Vector2.Zero;

		// Game over string
		const string gameOver = "Game Over";

		public GameOver (GameplayScreen game)
		{
			graphics = game.ScreenManager.Game.GraphicsDevice;
			spriteBatch = game.ScreenManager.SpriteBatch;
			content = game.ScreenManager.Game.Content;

			LoadContent ();			
		}
		
		private ContentManager Content 
		{
			get { return content; }
		}
		protected void LoadContent ()
		{
			//gameFont = Game.Content.Load<SpriteFont> ("Fonts/Pericles10Bold");
			gameFont = Content.Load<SpriteFont> ("Fonts/Pericles36");
		}

		public void Update (GameTime gameTime)
		{
			if (!Visible)
				return;
			
			//gameOver = string.Format (scoreFormat, players.ScoreOfPlayer1, players.ScoreOfPlayer2);
			Vector2 sc = gameFont.MeasureString (gameOver);
			gameOverPosition.X = (int)((ScreenManager.BaseWidth / 2) - (sc.X / 2));
			gameOverPosition.Y = 30;//(int)((graphics.PreferredBackBufferHeight / 2) - (sc.Y / 2));

		}
		
		public void Draw (GameTime gameTime)
		{
			
			if (!Visible)
				return;
			
			spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
			spriteBatch.DrawString (gameFont, gameOver, 
				gameOverPosition, gameOverBackground); 
			spriteBatch.End ();
			
		}
		
		public Color GameOverBackground 
		{
			get { return gameOverBackground; }
			set { 
				gameOverBackground = value; 
			}
		}
		
		bool _visible = false;
		public bool Visible 
		{
			get { return _visible; }
			set { _visible = value; }
		}
				
	}
}

