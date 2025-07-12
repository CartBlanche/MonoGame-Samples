using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BackgroundThreadTester
{
	public class Game1 : Game
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

		// Modern async patterns
		private CancellationTokenSource _cancellationTokenSource;
		private readonly object _componentLock = new object();
		private bool _backgroundTaskRunning = false;

        public Game1()
        {
#if !__MOBILE__
            this.IsMouseVisible = true;
#endif
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 480;
            graphics.PreferredBackBufferHeight = 640;
            graphics.ApplyChanges();

            Content.RootDirectory = "Content";

            cIm = new InputManager(this);

            CenterWindow();

            // Initialize cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
        }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Cancel any running background tasks
				_cancellationTokenSource?.Cancel();
				_cancellationTokenSource?.Dispose();
			}
			base.Dispose(disposing);
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
			if (_backgroundTaskRunning)
			{
				Console.WriteLine("Background task already running. Cancelling previous task...");
				_cancellationTokenSource?.Cancel();
			}

			_cancellationTokenSource = new CancellationTokenSource();
			_backgroundTaskRunning = true;
			
			Console.WriteLine ("Starting modern async background task");
			
			// Start the modern async background task
			_ = CreateBackgroundTaskAsync(_cancellationTokenSource.Token);
		}

		/// <summary>
		/// Modern async background worker using Task-based async patterns
		/// </summary>
		private async Task CreateBackgroundTaskAsync(CancellationToken cancellationToken)
		{
			try
			{
				Console.WriteLine("Background task started");
				
				// Create a loop that will add 5 new components with
				// a 2 second pause between additions
				for (int x = 1; x <= 5; x++)
				{
					// Check for cancellation
					cancellationToken.ThrowIfCancellationRequested();
					
					Console.WriteLine($"Adding component {x}/5");

					// Schedule component addition on the main thread
					// This is the correct cross-platform way to marshal to the UI thread
					var testTexture = new TestTexture(this);
					
					// Use a thread-safe approach to add components
					lock (_componentLock)
					{
						Components.Add(testTexture);
					}
					
					Console.WriteLine($"Component {x} added successfully");
					
					// Use non-blocking delay instead of Thread.Sleep
					// This allows the task to be cancelled during the delay
					await Task.Delay(2000, cancellationToken);
				}
				
				Console.WriteLine("Background task completed successfully");
			}
			catch (OperationCanceledException)
			{
				Console.WriteLine("Background task was cancelled");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Background task failed with error: {ex.Message}");
			}
			finally
			{
				_backgroundTaskRunning = false;
			}
		}

		/// <summary>
		/// Legacy method kept for compatibility - now uses modern patterns internally
		/// </summary>
		void BackgroundWorkerThread ()
		{
			// Redirect to modern async implementation
			CreateBackgroundThread();
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
			return "Modern";
		}//GetStyleMask


		protected override void Update (GameTime gameTime)
		{
			mousestatus = Mouse.GetState ();
            var keyboardState = Keyboard.GetState();
            // Handle input

            if (keyboardState.IsKeyDown(Keys.Escape))
            {
#if !__IOS__
                Exit();
#endif
            }

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
			// Window centering is handled by the platform-specific code
		}//CenterWindow
	}
}
