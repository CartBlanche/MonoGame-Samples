using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace Gorillas
{
	public class CityScapeBackground
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		
		// players
		PlayerManager players;
		
		GameplayScreen game; 

		internal static Color backGroundColor = Color.DarkBlue;
		
		public CityScapeBackground (GameplayScreen game)
			//: base(game)
		{
			graphics = (GraphicsDeviceManager)game.ScreenManager.Game.Services.GetService (typeof(IGraphicsDeviceManager));
			spriteBatch = game.ScreenManager.SpriteBatch;
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
//			gameFont = Content.Load<SpriteFont> ("Fonts/Pericles10Bold");
//			onePixel = Content.Load<Texture2D> ("Textures/OnePixel");
//			//gameFont = Game.Content.Load<SpriteFont> ("Fonts/Pericles36");
		}
		
		bool _visible = true;
		public bool Visible 
		{
			get { return _visible; }
			set { _visible = value; }
		}
		
		public void Update (GameTime gameTime)
		{
			if (!Visible)
				return;
		}
		
		public void Draw (GameTime gameTime)
		{
			
					
			if (!Visible)
				return;
			GraphicsDevice.Clear (backGroundColor);				
			
		}
	}
}

