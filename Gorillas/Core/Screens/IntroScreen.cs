#region File Description
//-----------------------------------------------------------------------------
// BackgroundScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace Gorillas
{
	/// <summary>
	/// The background screen sits behind all the other menu screens.
	/// It draws a background image that remains fixed in place regardless
	/// of whatever transitions the screens on top of it may be doing.
	/// </summary>
	class IntroScreen : GameScreen
	{
	#region Fields

		ContentManager content;
		Texture2D backgroundTexture;
		Color textColor = Color.FloralWhite;
		Color sparkleColor = Color.Red;
		string astrisks = "*    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    ";
		
		int a = 1;
		//int b = 1;
		
	#endregion

	#region Initialization


		/// <summary>
		/// Constructor.
		/// </summary>
		public IntroScreen ()
		{
			TransitionOnTime = TimeSpan.FromSeconds (0.5);
			TransitionOffTime = TimeSpan.FromSeconds (0.5);
		}


		/// <summary>
		/// Loads graphics content for this screen. The background texture is quite
		/// big, so we use our own local ContentManager to load it. This allows us
		/// to unload before going from the menus into the game itself, wheras if we
		/// used the shared ContentManager provided by the Game class, the content
		/// would remain loaded forever.
		/// </summary>
		public override void LoadContent ()
		{
			if (content == null)
				content = new ContentManager (ScreenManager.Game.Services, "Content");

			//backgroundTexture = content.Load<Texture2D> ("background");
			backgroundTexture = content.Load<Texture2D> ("gradient");
		}


		/// <summary>
		/// Unloads graphics content for this screen.
		/// </summary>
		public override void UnloadContent ()
		{
			content.Unload ();
		}

		public override void HandleInput (InputState input)
		{
			PlayerIndex playerIndex;
			
			if (input.IsNewKeyPress(Keys.Space, ControllingPlayer, out playerIndex) ||
				input.IsMenuSelect(ControllingPlayer, out playerIndex) ||
				input.PointerPressed) {
				playerIndex = PlayerIndex.One;
				LoadingScreen.Load (ScreenManager, false, playerIndex, 
							new MainMenuScreen ());	
			}
		}
	#endregion

	#region Update and Draw

		float time = 0;
		/// <summary>
		/// Updates the background screen. Unlike most screens, this should not
		/// transition off even if it has been covered by another screen: it is
		/// supposed to be covered, after all! This overload forces the
		/// coveredByOtherScreen parameter to false in order to stop the base
		/// Update method wanting to transition off.
		/// </summary>
		public override void Update (GameTime gameTime, bool otherScreenHasFocus, 
							bool coveredByOtherScreen)
		{
			
			base.Update (gameTime, otherScreenHasFocus, false);

			float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
			
			time += elapsed;
			//Console.WriteLine(time);
			if (time > 0.06) {
				a++;
				if (a > 5)
					a = 1;
				
				//b++;
				
				//if (b >= 15)
				//	b= 1;
				
				time = 0;
			}			
			
		}

		
		/// <summary>
		/// Draws the background screen.
		/// </summary>
		public override void Draw (GameTime gameTime)
		{
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			Viewport viewport = ScreenManager.BaseViewport;
			Rectangle fullscreen = new Rectangle (0, 0, viewport.Width, viewport.Height);

			spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

			Vector2 startPosition = new Vector2(0,50);
			var lineText = "Your mission is to hit your opponent with the exploding";
			startPosition.X = (viewport.Width / 2) - (ScreenManager.TextFont.MeasureString(lineText).X / 2);
			spriteBatch.DrawString(ScreenManager.TextFont, lineText,
				startPosition, textColor);
			
			lineText = "banana by varying the angle and power of your throw, taking";
			startPosition.Y += ScreenManager.TextFont.LineSpacing;
			startPosition.X = (viewport.Width / 2) - (ScreenManager.TextFont.MeasureString(lineText).X / 2);
			spriteBatch.DrawString(ScreenManager.TextFont,lineText,
				startPosition, textColor);
			
			lineText = "into account wind speed, gravity, and the city skyline.";
			startPosition.Y += ScreenManager.TextFont.LineSpacing;
			startPosition.X = (viewport.Width / 2) - (ScreenManager.TextFont.MeasureString(lineText).X / 2);
			spriteBatch.DrawString(ScreenManager.TextFont,lineText,
				startPosition, textColor);
			
			lineText = "The wind speed is shown by a directional arrow at the bottom";
			startPosition.Y += ScreenManager.TextFont.LineSpacing;
			startPosition.X = (viewport.Width / 2) - (ScreenManager.TextFont.MeasureString(lineText).X / 2);
			spriteBatch.DrawString(ScreenManager.TextFont,lineText,
				startPosition, textColor);

			lineText = "of the playing field, its length relative to its strength.";
			startPosition.Y += ScreenManager.TextFont.LineSpacing;
			startPosition.X = (viewport.Width / 2) - (ScreenManager.TextFont.MeasureString(lineText).X / 2);
			spriteBatch.DrawString(ScreenManager.TextFont,lineText,
				startPosition, textColor);

			lineText = "Tap, click or press A to start";
			startPosition.Y += ScreenManager.TextFont.LineSpacing * 3;
			startPosition.X = (viewport.Width / 2) - (ScreenManager.TextFont.MeasureString(lineText).X / 2);
			spriteBatch.DrawString(ScreenManager.TextFont,lineText,
				startPosition, textColor);
			
			lineText = "*    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *";
			
			float endLinePosition = ScreenManager.TextFont.MeasureString(lineText).X;
			float startLinePosition = (viewport.Width / 2) - (endLinePosition / 2);
			int lineSpacing = ScreenManager.TextFont.LineSpacing;

			startPosition.Y = lineSpacing;
			startPosition.X = startLinePosition;
			endLinePosition += startLinePosition;
			spriteBatch.DrawString(ScreenManager.TextFont,lineText.Substring(a),
				startPosition, sparkleColor);
			
			//int c = (a + b) % 5;
//FOR b = 2 TO 21                         'Print Vertical sparkles
//
//        c = (a + b) MOD 5
//
//        IF c = 1 THEN
//
//          LOCATE b, 80
//
//          PRINT "*";
//
//          LOCATE 23 - b, 1
//
//          PRINT "*";
//
//        ELSE
//
//          LOCATE b, 80
//
//          PRINT " ";
//
//          LOCATE 23 - b, 1
//
//          PRINT " ";
//
//        END IF
//
//      NEXT b
			
			//Console.WriteLine(c);
			var sparkleChar = string.Empty;
			int numVerticles = 44;
			int verticleSpacing = lineSpacing / 3;
			int verticleStart = lineSpacing ;
			for (int b = 2; b < numVerticles; b++) {
				int c = (a + b) % 5;
				if (c == 1) {
					sparkleChar = "*";
				}
				else {
					sparkleChar = " ";
				}
				
				// Right Side Verticles
				startPosition.Y = verticleSpacing + verticleSpacing * b;
				startPosition.X = endLinePosition;
				spriteBatch.DrawString(ScreenManager.TextFont,sparkleChar,
					startPosition, sparkleColor);
				
				startPosition.Y = verticleStart + verticleSpacing * (numVerticles - b);
				startPosition.X = startLinePosition;
				spriteBatch.DrawString(ScreenManager.TextFont,sparkleChar,
					startPosition, sparkleColor);
				
			}
			
			startPosition.Y = ScreenManager.TextFont.LineSpacing * 15;
			startPosition.X = startLinePosition;			
			spriteBatch.DrawString(ScreenManager.TextFont,lineText.Substring(6 - a),
				startPosition, sparkleColor);
			

//
//			for (int x = 1; x < 15; x++) {
//				lineText = "*";
//				startPosition.Y += ScreenManager.TextFont.LineSpacing;
//				spriteBatch.DrawString(ScreenManager.TextFont,lineText,
//					startPosition, sparkleColor);
//				
//			}
//			
//			lineText = "*    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *";
//
//			//startPosition.Y = 
//			spriteBatch.DrawString(ScreenManager.TextFont,lineText,
//				startPosition, sparkleColor);
			
			spriteBatch.End ();
		}


	#endregion
	}
}
