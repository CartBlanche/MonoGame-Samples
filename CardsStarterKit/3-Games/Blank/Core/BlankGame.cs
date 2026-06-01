//-----------------------------------------------------------------------------
// BlankGame.cs
//
// Main game class for Blank card game template
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using CardsFramework;
using CardsFramework.Core;


namespace Blank
{
    /// <summary>
    /// This is the main game type.
    /// </summary>
    public class BlankGame : Game
    {
        GraphicsDeviceManager graphicsDeviceManager;
        ScreenManager screenManager;

        /// <summary>
        /// Initializes a new instance of the game.
        /// </summary>
        public BlankGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            if (UIUtility.IsMobile)
            {
                graphicsDeviceManager.IsFullScreen = true;
                IsMouseVisible = false;
            }
            else if (UIUtility.IsDesktop)
            {
                graphicsDeviceManager.IsFullScreen = false;
                graphicsDeviceManager.PreferredBackBufferWidth = ScreenManager.BASE_BUFFER_WIDTH;
                graphicsDeviceManager.PreferredBackBufferHeight = ScreenManager.BASE_BUFFER_HEIGHT;
                IsMouseVisible = true;
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            // Use the language-aware ScreenManager overload so new games can
            // opt into runtime language/font switching without changing bootstrap code.
            screenManager = new ScreenManager(this, () => string.Empty);

            // Add screens - start with main menu
            screenManager.AddScreen(new MainMenuScreen(), null);

            Components.Add(screenManager);

            // Initialize audio plumbing even if this starter has no sounds yet.
            // New games can begin calling AudioManager.LoadSound/LoadSong immediately.
            AudioManager.Initialize(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Starter hook: load game-specific audio here.
            // Example:
            // AudioManager.LoadSound("Click", "Click");
            // AudioManager.LoadSong("track-name", "MainTheme");
            // AudioManager.SetPlaylist("MainTheme");

            base.LoadContent();
        }
    }
}