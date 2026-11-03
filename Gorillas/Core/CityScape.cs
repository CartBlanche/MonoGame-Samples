using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Net;

using Microsoft.Xna.Framework.Input;

namespace Gorillas
{
	public class CityScape //: DrawableGameComponent
	{
		GraphicsDevice graphics;
		SpriteBatch spriteBatch;
		RenderTarget2D renderTarget;
		static Random random = new Random ((int)DateTime.Now.Ticks);
		GameplayScreen engine;

		// city
		internal bool cityIsDirty = true;

		// buildings
		internal Texture2D building;
		internal List<CityBuilding> buildings = new List<CityBuilding> ();
		int lastBuilding = 0;

		// explosion
		Texture2D explosion;		
		// Erases whatever is already in the city render target wherever the explosion sprite is opaque:
		// dest = dest * (1 - srcAlpha). This is what punches the craters out of the buildings.
		static readonly BlendState eraseBlend = new BlendState {
			ColorSourceBlend = Blend.Zero,
			AlphaSourceBlend = Blend.Zero,
			ColorDestinationBlend = Blend.InverseSourceAlpha,
			AlphaDestinationBlend = Blend.InverseSourceAlpha
		};

		List<Vector2> explosions = new List<Vector2> ();		
		Vector2 explosionOrigin;
		Vector2 pointOfImpact = Vector2.Zero;

		// sun
		internal Sun sun;

		// Wind

		// SpriteSheet
		Texture2D spriteSheet;
		ExplosionManager explosionManager;
		CityScapeBackground background;

		public CityScape (GameplayScreen game)
		//: base(game)
		{

			// TODO: Construct any child components here
			graphics = game.ScreenManager.Game.GraphicsDevice;
			spriteBatch = game.ScreenManager.SpriteBatch;   
			engine = game;

			LoadContent ();
		}

		//		public override void Initialize ()
		//		{
		//
		//			// TODO: Add your initialization code here
		//
		//			base.Initialize ();
		//
		//		}
		ContentManager content = null;

		private ContentManager Content {
			get {
				if (content == null)
					content = new ContentManager (engine.ScreenManager.Game.Services, "Content");				
				return content;
			}
		}

		Color[] cityscapeData;

		internal Color[] CityScapeData {
			get {
				return cityscapeData;
			}
		}

		protected void LoadContent ()
		{
			building = new Texture2D (GraphicsDevice,1,1);
			building.SetData<Color> (new Color[] { Color.White });

			sun = new Sun (Content);
			sun.WorldLocation = new Vector2 (ScreenManager.BaseWidth / 2 - sun.Width / 2,10);

			explosion = Content.Load<Texture2D> ("Textures/Explosion");
			explosionMax = ScreenManager.BaseHeight / 50;
			explosionMax = 1.0f;
			explosionOrigin = new Vector2 (explosion.Width / 2, explosion.Height / 2);

			spriteSheet = Content.Load<Texture2D> ("Textures/ExplosionSpriteSheet");

			explosionManager = new ExplosionManager (spriteSheet,
								new Rectangle (0, 0, 50, 50),
								3,
								new Rectangle (0, 450, 2, 2));

			background = new CityScapeBackground (engine);

			//base.LoadContent ();
		}

		//float explosionScale = 0.5f;
		float explosionMax = 0;
		float explosionStep = 0.2f;
		float time = 0;

		public void Update (GameTime gameTime)
		{

			sun.Update (gameTime);
			explosionManager.Update (gameTime);
			background.Update (gameTime);
			// TODO: Add your update code here
			//base.Update (gameTime);
			//			MouseState mouse = Mouse.GetState();
			//			if (cityscapeData != null) {
			//				var pos = (mouse.Y * ScreenManager.BaseWidth) + mouse.X;
			//				Console.WriteLine(mouse.X + " / " + mouse.Y + " = " + cityscapeData[pos] );
			//			}

		}

		private GraphicsDevice GraphicsDevice {
			get {
				return engine.ScreenManager.GraphicsDevice;
			}
		}
		
		int numExplosions = 0;
		
		public void Draw (GameTime gameTime)
		{


			if (cityIsDirty) {
				
				// Create a rendertarget that matches the back buffer's dimensions, does not generate mipmaps automatically
				// (the Reach profile requires power of 2 sizing in order to do that), uses an RGBA color format, and
				// has no depth buffer or stencil buffer.
				//				renderTarget = new RenderTarget2D (GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth,
				//					GraphicsDevice.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Unknown);
				renderTarget = new RenderTarget2D (GraphicsDevice, ScreenManager.BaseWidth,
					ScreenManager.BaseHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

				// Set renderTarget as the surface to draw to instead of the back buffer
				GraphicsDevice.SetRenderTarget (renderTarget);

				// Clear the renderTarget. By default it's all a bright purple color. I like to use Color.Transparent to
				// enable easy alpha blending.
				GraphicsDevice.Clear (Color.Transparent);

				foreach (CityBuilding buildingRect in buildings) {

					spriteBatch.Begin ();
					buildingRect.Draw (spriteBatch);
					spriteBatch.End ();
				}

				cityIsDirty = false;

				// (The wind is shown by DrawWindIndicator, under the sun.)

				// Switch back to drawing onto the back buffer
				GraphicsDevice.SetRenderTarget (null);

				cityscapeData = new Color[renderTarget.Width * renderTarget.Height];
				renderTarget.GetData<Color> (cityscapeData);
				
				numExplosions = 0;
			}

			background.Draw (gameTime);			

			if (numExplosions != explosions.Count) {
			
				// Set renderTarget as the surface to draw to instead of the back buffer
				GraphicsDevice.SetRenderTarget (renderTarget);
				// Draw all explosions
				foreach (Vector2 explos in explosions) {
	
					// Begin the sprite batch, using our custom effect.
					spriteBatch.Begin (SpriteSortMode.Deferred, eraseBlend);
//					spriteBatch.Draw (explosion, explos, null, CityScapeBackground.backGroundColor, 0, explosionOrigin, 
//						explosionMax, SpriteEffects.None, 0);
					spriteBatch.Draw (explosion, explos, null, Color.White, 0, explosionOrigin, 
						explosionMax, SpriteEffects.None, 0);
					spriteBatch.End ();
				}
	
				// Switch back to drawing onto the back buffer
				GraphicsDevice.SetRenderTarget (null);
				
				//cityscapeData = new Color[renderTarget.Width * renderTarget.Height];
				renderTarget.GetData<Color> (cityscapeData);
				numExplosions = explosions.Count;
			}
			
			spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
			spriteBatch.Draw (renderTarget, new Vector2 (0,0), Color.White);
			sun.Draw (spriteBatch);
			DrawWindIndicator ();
			spriteBatch.End ();

			spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
			explosionManager.Draw (spriteBatch);
			spriteBatch.End ();			

			//base.Draw (gameTime);
		}

		// Wind indicator, centred under the sun: an arrow pointing the way the wind blows whose length
		// grows with its strength, and a label with the strength.
		const float windArrowPerUnit = 4f;   // pixels of arrow per unit of wind (max wind is about 15)
		const float windIndicatorY = 52f;

		void DrawWindIndicator ()
		{
			int wind = WindManager.Wind;
			SpriteFont font = engine.ScreenManager.TextFont;
			float centreX = ScreenManager.BaseWidth / 2f;

			string label = wind == 0 ? "No wind" : "Wind " + Math.Abs (wind);
			float labelScale = 0.75f;
			Vector2 labelSize = font.MeasureString (label) * labelScale;
			float labelY = windIndicatorY;

			if (wind != 0) {
				// Positive wind blows to the right (see Banana.ThrowMe).
				float length = Math.Abs (wind) * windArrowPerUnit + 14f;
				float angle = wind > 0 ? 0f : MathHelper.Pi;
				Vector2 tail = new Vector2 (centreX - Math.Sign (wind) * length / 2f, windIndicatorY);
				RetroArrow.Draw (spriteBatch, building, tail, angle, length, Color.Cyan, 0.8f);
				labelY += 10;
			}

			spriteBatch.DrawString (font, label, new Vector2 (centreX - labelSize.X / 2f, labelY),
				Color.Cyan, 0, Vector2.Zero, labelScale, SpriteEffects.None, 0);
		}

		private void CreateCityScape ()
		{
			//			cityGen = true;

			buildings.Clear ();
			explosions.Clear ();

			int slope = random.Next (1, 7);
			int newHeight = 0;
			int bottomLine = 335;
			int heightIncrement = 10;
			int defaultBuildingWidth = 37;
			int randomHeight = 120;

			switch (slope) {
			case 1:
				newHeight = 15;  // upward slope
				break;
			case 2:
				newHeight = 130;  // downward slope
				break;
			case 3:
			case 4:
			case 5:
				newHeight = 15;  // "V" slope - most common
				break;
			case 6:
				newHeight = 130;  // Inverted "V" slope
				break;

			}

			int currentBuilding = 1;
			int x = 2;
			int screenWidth = ScreenManager.BaseWidth;

			//Console.WriteLine ("slope:" + slope);
			do {
				switch (slope) {
				case 1:
					newHeight += heightIncrement;
					break;
				case 2:
					newHeight -= heightIncrement;
					break;
				case 3:
				case 4:
				case 5:
					if (x > screenWidth / 2)
						newHeight -= 2 * heightIncrement;
					else
						newHeight += 2 * heightIncrement;
					break;
				case 6:
					if (x > screenWidth / 2)
						newHeight += 2 * heightIncrement;
					else
						newHeight -= 2 * heightIncrement;
					break;
				}	

				// Set width of building and check to see if it would go off the screen
				int bWidth = random.Next (5, defaultBuildingWidth) + defaultBuildingWidth;
				if (x + bWidth > screenWidth)
					bWidth = screenWidth - x - 2;

				// set height of building and check to see if it goes below the screen
				int bHeight = random.Next (randomHeight) + newHeight;
				if (bHeight < heightIncrement)
					bHeight = heightIncrement;

				int maxHeight = 0;
				int gHeight = 25;

				// check to see if building is to high
				if (bottomLine - bHeight < maxHeight + gHeight)
					bHeight = maxHeight + gHeight - 5;

				// Add a building
				Rectangle bRect = new Rectangle (x,bottomLine - bHeight,bWidth,bHeight);
				Color bColor = new Color ((float)random.NextDouble (),(float)random.NextDouble (),(float)random.NextDouble (),1.0f);
				CityBuilding cityBuilding = new CityBuilding (building, bRect, bColor);
				buildings.Add (cityBuilding);

				// draw windows
				x = x + bWidth + 2;
				currentBuilding += 1;

			} while ( x <= screenWidth - heightIncrement);

			lastBuilding = currentBuilding - 1;

			WindManager.CalculateWindSpeed ();

		}

		public bool CityIsDirty {
			get { return cityIsDirty; }
			set { 
				CreateCityScape ();
				cityIsDirty = value; 
			}
		}

		public void RedrawCityScape ()
		{
			cityIsDirty = true;
		}

		public void PlaceGorilla (Gorilla gorilla)
		{
			int buildingNum = 0;
			switch (gorilla.PlayerClass) {
			case Gorilla.GorillaPlayerClass.Player1:
				buildingNum = random.Next (1, 3) + 1;
				break;
			case Gorilla.GorillaPlayerClass.Player2:
				buildingNum = lastBuilding - random.Next (1, 3);
				break;
			}

			Rectangle rec = buildings [buildingNum - 1].BuildingRect;
			rec.X += (rec.Width / 2) - gorilla.Width / 2;
			rec.Y -= gorilla.Height;
			gorilla.WorldLocation = new Vector2 (rec.X, rec.Y);

		}


		/// <summary>
		/// Determines if there is overlap of the non-transparent pixels
		/// between two sprites.
		/// </summary>
		/// <param name="rectangleA">Bounding rectangle of the first sprite</param>
		/// <param name="dataA">Pixel data of the first sprite</param>
		/// <param name="rectangleB">Bouding rectangle of the second sprite</param>
		/// <param name="dataB">Pixel data of the second sprite</param>
		/// <returns>True if non-transparent pixels overlap; false otherwise</returns>
		static bool IntersectPixels (Rectangle rectangleA, Color[] dataA, 
				Rectangle rectangleB, Color[] dataB)
		{
			// Find the bounds of the rectangle intersection
			int top = Math.Max (rectangleA.Top, rectangleB.Top);
			int bottom = Math.Min (rectangleA.Bottom, rectangleB.Bottom);
			int left = Math.Max (rectangleA.Left, rectangleB.Left);
			int right = Math.Min (rectangleA.Right, rectangleB.Right);

			// Check every point within the intersection bounds
			for (int y = top; y < bottom; y++) {
				for (int x = left; x < right; x++) {
					// Get the color of both pixels at this point
					Color colorA = dataA [(x - rectangleA.Left) + 
					(y - rectangleA.Top) * rectangleA.Width];
					Color colorB = dataB [(x - rectangleB.Left) + 
					(y - rectangleB.Top) * rectangleB.Width];

					// If both pixels are not completely transparent,
					if (colorA.A != 0 && colorB.A != 0) {
						// then an intersection has been found
						return true;
					}
				}
			}

			// No intersection found
			return false;
		}
		
		/// <summary>
		/// Determines if there is overlap of the non-transparent pixels
		/// between two sprites.
		/// </summary>
		/// <param name="rectangleA">Bounding rectangle of the first sprite</param>
		/// <param name="dataA">Pixel data of the first sprite</param>
		/// <param name="rectangleB">Bouding rectangle of the second sprite</param>
		/// <param name="dataB">Pixel data of the second sprite</param>
		/// <returns>True if non-transparent pixels overlap; false otherwise</returns>
		static bool IntersectPixels (Rectangle rectangleA, Color[] dataA, 
				Vector2 pixelPoint)
		{
			// Find the bounds of the rectangle intersection
//			int top = Math.Max (rectangleA.Top, rectangleB.Top);
//			int bottom = Math.Min (rectangleA.Bottom, rectangleB.Bottom);
//			int left = Math.Max (rectangleA.Left, rectangleB.Left);
//			int right = Math.Min (rectangleA.Right, rectangleB.Right);
//
//			// Check every point within the intersection bounds
//			for (int y = top; y < bottom; y++) {
//				for (int x = left; x < right; x++) {
//					// Get the color of both pixels at this point
//					Color colorA = dataA [(x - rectangleA.Left) + 
//					(y - rectangleA.Top) * rectangleA.Width];
//					Color colorB = dataB [(x - rectangleB.Left) + 
//					(y - rectangleB.Top) * rectangleB.Width];
//
//					// If both pixels are not completely transparent,
//					if (colorA.A != 0 && colorB.A != 0) {
//						// then an intersection has been found
//						return true;
//					}
//				}
//			}
			
			// No intersection found
			return false;
		}
		
		public ObjectCollisionEnum CheckBuildingCollision (Banana banana)
		{
			ObjectCollisionEnum what = ObjectCollisionEnum.Nothing;

			if (!banana.Visible)
				return what;

			// This is here for testing only
			// The AI will be using rectangles but normal collision is pixel perfect which has
			// will show some innaccuracies on the AI side.

			//			if (engine.gorilla1.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
			//				Console.WriteLine ("Gorilla 1 hit @ " + banana.WorldLocation);
			//				return ObjectCollisionEnum.Gorilla1;
			//			}
			//			else {
			//				if (engine.gorilla2.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
			//					Console.WriteLine ("Gorilla 2 hit @ " + banana.WorldLocation);
			//					return ObjectCollisionEnum.Gorilla1;
			//				}
			//			}

			Color posColor = banana.PositionColor;
			//var newY = ScreenManager.BaseHeight - (int)banana.WorldLocation.Y;
			var newY = (int)banana.WorldLocation.Y;
			var pos = (int)((newY * renderTarget.Width) + banana.WorldLocation.X);
			//var posRect = new Rectangle(banana.wor)
			var posRect = new Rectangle((int)banana.WorldCenter.X, (int)banana.WorldCenter.Y,1,1);
			var banaData = new Color[] {Color.Yellow};
			
			if (pos < 0 || pos > cityscapeData.Length)
				pos = -1;

			if (engine.gorilla1.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
				
				bool hit = IntersectPixels(engine.gorilla1.WorldRectangle, Gorilla.gorillaData,
					posRect, banaData);
				
				if (hit) {
					Console.WriteLine ("Gorilla 1 hit @ " + banana.WorldLocation);
					return ObjectCollisionEnum.Gorilla1;
				}
			}

			if (engine.gorilla2.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
				
				bool hit = IntersectPixels(engine.gorilla2.WorldRectangle, Gorilla.gorillaData,
					posRect, banaData);
				
				if (hit) {
					Console.WriteLine ("Gorilla 2 hit @ " + banana.WorldLocation);
					return ObjectCollisionEnum.Gorilla2;
				}
			}
			
			if (sun.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
				sun.ReactToCollision ();
				return ObjectCollisionEnum.Sun;
			}

			if (pos > 0 && cityscapeData [pos].A != 0) {
				Console.WriteLine ("Bana Hit non transparent color " + cityscapeData [pos]);

				//if (posColor != Color.TransparentBlack && posColor != CityScapeBackground.backGroundColor) {


				if (sun.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
					sun.ReactToCollision ();
					what = ObjectCollisionEnum.Sun;
				} else {
					if (engine.gorilla1.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
						Console.WriteLine ("Gorilla 1 hit @ " + banana.WorldLocation);
						what = ObjectCollisionEnum.Gorilla1;
					} else {

						if (engine.gorilla2.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
							Console.WriteLine ("Gorilla 2 hit @ " + banana.WorldLocation);
							what = ObjectCollisionEnum.Gorilla2;
						} else {
							//						if (posColor.R != CityScapeBackground.backGroundColor.R && 
							//							posColor.G != CityScapeBackground.backGroundColor.G && 
							//							posColor.B != CityScapeBackground.backGroundColor.B) {

							//						if (engine.gorilla1.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
							//							Console.WriteLine ("Gorilla 1 hit");
							//							what = ObjectCollisionEnum.Gorilla1;
							//						}
							//						if (engine.gorilla2.CollisionRectangle.Intersects (banana.CollisionRectangle)) {
							//							Console.WriteLine ("Gorilla 2 hit");
							//							what = ObjectCollisionEnum.Gorilla2;
							//						}

							if (what != ObjectCollisionEnum.Gorilla1 && 
								what != ObjectCollisionEnum.Gorilla2) {
								Console.WriteLine ("hit" + posColor + " - " + CityScapeBackground.backGroundColor + " @ " + banana.WorldLocation);
								pointOfImpact = banana.WorldLocation;
								time = 0;
								what = ObjectCollisionEnum.Building;
								explosionManager.AddExplosion (pointOfImpact, 
								new Vector2 (WindManager.Wind * 20,random.Next (0, 101) - 50) / 10);
								explosions.Add (pointOfImpact);
								engine.banana.Visible = false;

							}
							//						}
						}
					}
				}

			}

			if (what != ObjectCollisionEnum.Nothing && what != ObjectCollisionEnum.Sun)
				sun.Reset ();

			return what;
		}

		public void ResetScenary ()
		{
			sun.PlayAnimation ("idle");
		}

		internal void WriteScene (PacketWriter packetWriter)
		{
			packetWriter.Write (buildings.Count);
			foreach (var building in buildings) {
				building.WriteBuilding (packetWriter);
			}

			packetWriter.Write (WindManager.Wind);			
		}

		internal void ReadCityScape (PacketReader packetReader)
		{
			buildings.Clear ();
			explosions.Clear ();
			int buildingCount = packetReader.ReadInt32 ();
			for (int x = 0; x < buildingCount; x++) {

				CityBuilding cityBuilding = new CityBuilding (building);
				cityBuilding.ReadBuilding (packetReader);
				buildings.Add (cityBuilding);

			}

			int wind = packetReader.ReadInt32 ();
			Console.WriteLine ("Wind: " + wind);

			WindManager.Wind = wind;
			cityIsDirty = true;
		}
	}
}

