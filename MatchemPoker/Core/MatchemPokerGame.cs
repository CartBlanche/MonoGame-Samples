using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;


namespace MatchemPoker
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class MatchemPokerGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        public TileGameRenderer renderer;
        public ITileGame game;
        private InputState inputState;

        // Fixed game resolution — all gameplay logic is authored for this size.
        // The render target is always this size; it is then letterboxed to the
        // actual screen so both rendering and input work correctly on every device.
        private const int GameWidth  = 480;
        private const int GameHeight = 800;

        private RenderTarget2D _gameRenderTarget;
        private Rectangle      _gameDestRect;   // letterbox dest on the real screen
        private SpriteBatch    _scaleBatch;     // used only for the letterbox blit
        
        // Debug display
        private SpriteFont debugFont;
        private SpriteBatch debugSpriteBatch;
        
        // Custom cursor (hand images)
        private Texture2D handOpenTexture;
        private Texture2D handClosedTexture;
        private SpriteBatch cursorSpriteBatch;

        public MatchemPokerGame()
        {
            graphics = new GraphicsDeviceManager(this);

            // On desktop, fix the window to the game's canonical size.
            // On mobile MonoGame uses the native screen resolution regardless,
            // and the render-target letterbox handles the difference.
            if (IsDesktopPlatform())
            {
                graphics.PreferredBackBufferWidth  = GameWidth;
                graphics.PreferredBackBufferHeight = GameHeight;
            }

            graphics.SupportedOrientations = DisplayOrientation.Portrait;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            renderer = new TileGameRenderer();
            game = new TileNpc(renderer, 0, 0, 1.0f, 800.0f / 480.0f);
            inputState = new InputState();
            
            base.Initialize();
        }

        /// <summary>
        /// Detects if the game is running on iOS.
        /// </summary>
        /// <returns>True if running on iOS; False otherwise</returns>
        private bool IsIOSPlatform()
        {
            return OperatingSystem.IsIOS();
        }

        /// <summary>
        /// Detects if the game is running on a desktop platform.
        /// </summary>
        /// <returns>True if running on Windows, macOS, or Linux; False for mobile platforms (Android, iOS)</returns>
        private bool IsDesktopPlatform()
        {
            return OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();
        }

        /// <summary>
        /// Handle ESC/Back key press - decides whether to exit or handle in-game
        /// </summary>
        private void HandleEscapeKey()
        {
            // Check if game is in menu state and we should exit
            if (game.GetCurrentGameState() == TileGameState.eTILEGAMESTATE_MENU)
            {
                // On Desktop and Android: exit the game
                // On iOS: don't exit (iOS doesn't have a back button)
                if (!IsIOSPlatform())
                {
                    this.Exit();
                }
            }
            else
            {
                // In-game: handle escape key (pause/resume/menu transitions)
                game.HandleEscapeKey();
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Tell the renderer to always use the canonical game dimensions,
            // not whatever the live viewport happens to be.
            renderer.Init(Content, GraphicsDevice, GameWidth, GameHeight);
            game.Init(Content);

            // Render-to-texture: game always draws into this 480×800 surface.
            _gameRenderTarget = new RenderTarget2D(GraphicsDevice, GameWidth, GameHeight);
            _scaleBatch = new SpriteBatch(GraphicsDevice);
            UpdateGameDestRect();
            
            // Load custom cursor textures for desktop
            if (IsDesktopPlatform())
            {
                try
                {
                    handOpenTexture = Content.Load<Texture2D>("images/hand_open");
                    handClosedTexture = Content.Load<Texture2D>("images/hand_closed");
                    cursorSpriteBatch = new SpriteBatch(GraphicsDevice);
                }
                catch
                {
                    // If hand textures not available, no custom cursor will be drawn
                    handOpenTexture = null;
                    handClosedTexture = null;
                }
            }
            
#if DEBUG
            // Load debug font for coordinate display
            try
            {
                debugFont = Content.Load<SpriteFont>("debugFont");
                debugSpriteBatch = new SpriteBatch(GraphicsDevice);
            }
            catch
            {
                // If debug font is not available, debug display will be skipped
                debugFont = null;
            }
#endif
        }

        protected override void Dispose(bool disposing)
        {
            // Save the state before exiting
            game.Save();
            _gameRenderTarget?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Computes the letterbox destination rectangle so the 480×800 render
        /// target fills as much of the real screen as possible while preserving
        /// aspect ratio.  Also informs InputState of the mapping.
        /// </summary>
        private void UpdateGameDestRect()
        {
            int screenW = GraphicsDevice.Viewport.Width;
            int screenH = GraphicsDevice.Viewport.Height;

            float scale  = Math.Min((float)screenW / GameWidth, (float)screenH / GameHeight);
            int   destW  = (int)(GameWidth  * scale);
            int   destH  = (int)(GameHeight * scale);
            int   destX  = (screenW - destW) / 2;
            int   destY  = (screenH - destH) / 2;

            _gameDestRect = new Rectangle(destX, destY, destW, destH);
            inputState.SetGameViewport(_gameDestRect, GameWidth, GameHeight);
        }

        /// <summary>
        /// Normalizes a game-space position to 0..1 coordinates for game logic.
        /// Both axes divide by GameWidth because the renderer uses vpWidth for both axes.
        /// </summary>
        private Vector2 NormalizeGamePos(Vector2 gamePos)
        {
            return new Vector2(gamePos.X / GameWidth, gamePos.Y / GameWidth);
        }

        /// <summary>Converts a game-space position to screen pixels (for cursor rendering).</summary>
        private Vector2 GameToScreen(Vector2 gamePos)
        {
            return new Vector2(
                _gameDestRect.X + gamePos.X / GameWidth  * _gameDestRect.Width,
                _gameDestRect.Y + gamePos.Y / GameHeight * _gameDestRect.Height);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            inputState.Update(gameTime,
                              GraphicsDevice.Viewport.Width,
                              GraphicsDevice.Viewport.Height);

            Vector2 norm = NormalizeGamePos(inputState.CursorPosition);

            // Dispatch unified press/drag/release events to game logic
            if (inputState.IsNewPress)
                game.Click(norm.X, norm.Y, MouseEventType.eMOUSEEVENT_BUTTONDOWN);
            else if (inputState.IsNewRelease)
                game.Click(norm.X, norm.Y, MouseEventType.eMOUSEEVENT_BUTTONUP);
            else if (inputState.IsDragging)
                game.Click(norm.X, norm.Y, MouseEventType.eMOUSEEVENT_MOUSEDRAG);

            // Keyboard confirm (Space/Enter) — fires at screen centre
            if (inputState.IsConfirmTriggered)
                game.Click(0.5f, 0.5f, MouseEventType.eMOUSEEVENT_BUTTONDOWN);
            else if (inputState.IsConfirmReleased)
                game.Click(0.5f, 0.5f, MouseEventType.eMOUSEEVENT_BUTTONUP);

            // ESC / gamepad Back
            if (inputState.IsEscapeTriggered)
                HandleEscapeKey();

            game.Run((float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // ── Pass 1: render game into the fixed 480×800 render target ────────
            GraphicsDevice.SetRenderTarget(_gameRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);
            renderer.StartFrame();
            game.Draw();
            renderer.EndFrame();

#if DEBUG
            // Draw debug coordinate display
            if (debugFont != null && debugSpriteBatch != null)
            {
                debugSpriteBatch.Begin();

                // Current cursor position
                debugSpriteBatch.DrawString(debugFont,
                    $"Cursor: X={inputState.CursorPosition.X:F0} Y={inputState.CursorPosition.Y:F0}",
                    new Vector2(10, 10), Color.Black);
                    
                // Normalized coordinates (for game logic)
                Vector2 norm = NormalizeGamePos(inputState.CursorPosition);
                debugSpriteBatch.DrawString(debugFont, 
                    $"Normalized: X={norm.X:F3} Y={norm.Y:F3}", 
                    new Vector2(10, 40), Color.Black);
                
                // Press-down position
                debugSpriteBatch.DrawString(debugFont, 
                    $"Down: X={inputState.PressDownPosition.X:F0} Y={inputState.PressDownPosition.Y:F0}", 
                    new Vector2(10, 70), Color.Black);
                
                // Press-up position
                debugSpriteBatch.DrawString(debugFont, 
                    $"Up: X={inputState.PressUpPosition.X:F0} Y={inputState.PressUpPosition.Y:F0}", 
                    new Vector2(10, 100), Color.Black);
                
                debugSpriteBatch.End();
            }
#endif

            // ── Pass 2: letterbox-blit the render target to the real screen ─────
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);   // letterbox bars are black
            _scaleBatch.Begin();
            _scaleBatch.Draw(_gameRenderTarget, _gameDestRect, Color.White);
            _scaleBatch.End();

            // ── Pass 3: hand cursor overlay in screen-space (desktop only) ──────
            if (IsDesktopPlatform() && handOpenTexture != null && handClosedTexture != null && cursorSpriteBatch != null)
            {
                cursorSpriteBatch.Begin();
                Texture2D handTexture = inputState.IsPressed ? handClosedTexture : handOpenTexture;
                Vector2 handSize     = new Vector2(handTexture.Width, handTexture.Height);
                Vector2 handPosition = GameToScreen(inputState.CursorPosition) - (handSize / 2);
                cursorSpriteBatch.Draw(handTexture, handPosition, Color.White);
                cursorSpriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
