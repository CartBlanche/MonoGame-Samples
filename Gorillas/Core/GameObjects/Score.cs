using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace Gorillas
{
	public class Score //: DrawableGameComponent
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		
		// font
		SpriteFont gameFont;
		
		// score color
		Color scoreBoardBackground = Color.DarkBlue;

		// score position
		Vector2 scoreBoardPosition = Vector2.Zero;


		// players
		PlayerManager players;
		
		// Score Background
		Texture2D onePixel;
		Rectangle scoreBackground = Rectangle.Empty;
		
		// Score string
		const string scoreFormat = "{0}>Score<{1}";
		string score = string.Empty;

		GameplayScreen game; 
		
		public Score (GameplayScreen game,PlayerManager players)
			//: base(game)
		{
			graphics = (GraphicsDeviceManager)game.ScreenManager.Game.Services.GetService (typeof(IGraphicsDeviceManager));
			//spriteBatch = (SpriteBatch)game.Services.GetService (typeof(SpriteBatch));
			spriteBatch = game.ScreenManager.SpriteBatch;
			this.players = players;
			//content = new ContentManager (game.ScreenManager.Game.Services, "Content");
			this.game = game;
			LoadContent ();			
		}
		
		ContentManager content = null;
		
		private ContentManager Content 
		{
			get {
				if (content == null)
					content = new ContentManager (game.ScreenManager.Game.Services, "Content");				
				return content;
			}
		}
		
		
		private GraphicsDevice GraphicsDevice
		{
			get {
				return game.ScreenManager.GraphicsDevice;
			}
		}
		
		protected void LoadContent ()
		{
			gameFont = Content.Load<SpriteFont> ("Fonts/Pericles10Bold");
			onePixel = Content.Load<Texture2D> ("Textures/OnePixel");
			//gameFont = Game.Content.Load<SpriteFont> ("Fonts/Pericles36");
		}
		
		bool _visible = false;
		public bool Visible 
		{
			get { return _visible; }
			set { _visible = value; }
		}
		
		public void Update (GameTime gameTime)
		{
			if (!Visible)
				return;
			
			score = string.Format (scoreFormat, players.ScoreOfPlayer1, players.ScoreOfPlayer2);
			Vector2 sc = gameFont.MeasureString (score);
			scoreBoardPosition.X = 5; //(int)((ScreenManager.BaseWidth / 2) - (sc.X / 2));
			scoreBoardPosition.X = (int)((ScreenManager.BaseWidth / 2) - (sc.X / 2));
			scoreBoardPosition.Y = (int)(ScreenManager.BaseHeight - sc.Y) - 25;
			scoreBackground.X = (int)scoreBoardPosition.X - 2;
			scoreBackground.Y = (int)scoreBoardPosition.Y + 2;
			scoreBackground.Width = (int)sc.X + 6;
			scoreBackground.Height = (int)sc.Y + 2;
			//scoreBackground.Inflate(3,3);
			
		}
		
		public void Draw (GameTime gameTime)
		{
			
			if (!Visible)
				return;
			
			spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
			spriteBatch.Draw(onePixel,scoreBackground, scoreBoardBackground);
			spriteBatch.DrawString (gameFont, score, scoreBoardPosition, Color.White); 
			spriteBatch.End ();
			
			//base.Draw (gameTime);
		}
		
		public Color ScoreBoardBackground 
		{
			get { return scoreBoardBackground; }
			set { 
				scoreBoardBackground = value; 
			}
		}
	}
}

