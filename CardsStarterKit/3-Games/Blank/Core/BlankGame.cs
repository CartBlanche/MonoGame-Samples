//-----------------------------------------------------------------------------
// BlankGame.cs
//
// Main game class for Blank card game template
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using CardsFramework;
using GameStateManagement;

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

            screenManager = new ScreenManager(this);

            // Add screens - start with main menu
            screenManager.AddScreen(new MainMenuScreen(), null);

            Components.Add(screenManager);
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
            base.LoadContent();
        }
    }
}