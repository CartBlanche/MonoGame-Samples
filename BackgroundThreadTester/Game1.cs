using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Windows.Forms;
using System.Threading;

using MonoMac.AppKit;
using MonoMac.Foundation;

namespace BackgroundThreadTester
{
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		public TextManager aTm;
		public InputManager cIm;
		private SpriteFont sfStandard;
		public MouseState mousestatus;
		public Object aObjects;
		TimeSpan tsElapsed = TimeSpan.Zero;
		private String sLoading = "Loading";

		public Game1 ()
		{
			this.IsMouseVisible = true;
			graphics = new GraphicsDeviceManager (this);
			Content.RootDirectory = "Content";

			cIm = new InputManager (this);   
            
			CenterWindow ();    
		}

		protected override void Initialize ()
		{
			// TODO: Add your initialization logic here

			base.Initialize ();
		}

		protected override void LoadContent ()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch (GraphicsDevice);
			Services.AddService (typeof(SpriteBatch), spriteBatch);

			sfStandard = Content.Load<SpriteFont> ("fntStandard");

			aTm = new TextManager (this, sfStandard);
			Components.Add (aTm);
		}

		protected override void UnloadContent ()
		{
			// TODO: Unload any non ContentManager content here
		}

		public void CreateBackgroundThread ()
		{
			System.Console.WriteLine ("before invoke");
			// create a new thread using BackgroundWorkerThread as method to execute
			var thread = new Thread (BackgroundWorkerThread as ThreadStart);
			// start it
			thread.Start ();

			System.Console.WriteLine ("after invoke");
		}//if
        
		void BackgroundWorkerThread ()
		{
			// Create an Autorelease Pool or we will leak objects.
			using (var pool = new NSAutoreleasePool()) {
				// Create a loop that will add 5 new components with
				// a 2 second pause between additions
				Console.WriteLine ("Before component load");
				for (int x = 1; x <= 5; x++) {

					Console.WriteLine ("Before add");

					// Make sure we invoke this on the Main Thread or OpenGL will throw an error
					MonoMac.AppKit.NSApplication.SharedApplication.BeginInvokeOnMainThread (delegate {
						Components.Add (new TestTexture (this));
					});
					Console.WriteLine ("After add");
					// Sleep for 2 seconds between each component addition
					Thread.Sleep (2000);

				}
				Console.WriteLine ("After component load");

			}

		}

		public int GetBackBufferWidth ()
		{
			return graphics.PreferredBackBufferWidth;
		}//GetBackBufferWidth

		public int GetBackBufferHeight ()
		{
			return graphics.PreferredBackBufferHeight;
		}//GetBackBufferWidth
        
		public String GetStyleMask ()
		{
			return this.Window.Window.StyleMask.ToString ();
		}//GetStyleMask


		protected override void Update (GameTime gameTime)
		{
			mousestatus = Mouse.GetState ();

			cIm.InputHandler (mousestatus, gameTime);

			base.Update (gameTime);
		}

		protected override void Draw (GameTime gameTime)
		{
            
			GraphicsDevice.Clear (Color.CornflowerBlue);

			spriteBatch.Begin (SpriteSortMode.Immediate, BlendState.AlphaBlend);
			DrawLoadingAnimation (gameTime);
			base.Draw (gameTime); 
			spriteBatch.End ();

       
		}
        
		void DrawLoadingAnimation (GameTime gameTime)
		{
			tsElapsed += gameTime.ElapsedGameTime;

			// it's time for next char
			if (tsElapsed > TimeSpan.FromMilliseconds (500)) {
				tsElapsed = TimeSpan.Zero;

				sLoading = sLoading.Insert (sLoading.Length, ".");

				if (sLoading.Length == 13) {
					sLoading = "Loading";
				}//if
			}//if


			spriteBatch.DrawString (sfStandard, sLoading, new Vector2 (50, 50), Color.White);
		}//DrawLoadingAnimation
        
		public void CenterWindow ()
		{
        	
        		
			int index;
			int upperBound;
			float fScreenWidth, fScreenHeight, fNewX, fNewY, fWindowWidth, fWindowHeight, fTitleBarHeight;
			Screen[] screens = Screen.AllScreens;
	        
			fScreenWidth = fScreenHeight = 0;
	        
			upperBound = screens.GetUpperBound (0);
			for (index = 0; index <= upperBound; index++) {
				if (screens [index].Primary) {
					fScreenWidth = (float)screens [index].Bounds.Width;
					fScreenHeight = (float)screens [index].Bounds.Height;  
					index = upperBound;
				}//if
			}//for
            
			fWindowWidth = graphics.PreferredBackBufferWidth;
			fWindowHeight = graphics.PreferredBackBufferHeight;
            	
			fNewX = (fScreenWidth - fWindowWidth) / 2;
			fNewY = (fScreenHeight - fWindowHeight) / 2;
            
			fTitleBarHeight = this.Window.Window.Frame.Height - fWindowHeight;
            
			System.Drawing.PointF pfLocation = new System.Drawing.PointF (fNewX, fNewY);
			System.Drawing.PointF pfSize = new System.Drawing.PointF (fWindowWidth, fWindowHeight + fTitleBarHeight);
			System.Drawing.SizeF sfSize = new System.Drawing.SizeF (pfSize);
			System.Drawing.RectangleF rectTemp = new System.Drawing.RectangleF (pfLocation, sfSize);
			this.Window.Window.SetFrame (rectTemp, true);
		}//CenterWindow
	}
}
