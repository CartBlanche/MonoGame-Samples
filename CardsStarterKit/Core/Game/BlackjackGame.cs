//-----------------------------------------------------------------------------
// PauseScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using CardsFramework;
using System.Globalization;
using GameStateManagement;

namespace Blackjack
{
    /// <summary>
    /// This is the main game type.
    /// </summary>
    public class BlackjackGame : Game
    {
        GraphicsDeviceManager graphicsDeviceManager;
        ScreenManager screenManager;

        public static float HeightScale = 1.0f;
        public static float WidthScale = 1.0f;

        const int BUFFER_WIDTH = 800;
        const int BUFFER_HEIGHT = 480;

        /// <summary>
        /// Indicates if the game is running on a mobile platform.
        /// </summary>
        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Indicates if the game is running on a desktop platform.
        /// </summary>
        public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        /// <summary>
        /// Initializes a new instance of the game.
        /// </summary>
        public BlackjackGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            if (IsMobile)
            {
                graphicsDeviceManager.IsFullScreen = true;
                IsMouseVisible = false;
            }
            else if (IsDesktop)
            {
                graphicsDeviceManager.IsFullScreen = false;
                IsMouseVisible = true;
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            screenManager = new ScreenManager(this);

            screenManager.AddScreen(new BackgroundScreen(), null);
            screenManager.AddScreen(new MainMenuScreen(), null);

            Components.Add(screenManager);

            // Initialize sound system
            AudioManager.Initialize(this);
        }

        protected override void Initialize()
        {
            base.Initialize();

            graphicsDeviceManager.PreferredBackBufferWidth = BUFFER_WIDTH;
            graphicsDeviceManager.PreferredBackBufferHeight = BUFFER_HEIGHT;
            graphicsDeviceManager.ApplyChanges();

            Rectangle bounds = graphicsDeviceManager.GraphicsDevice.Viewport.TitleSafeArea;
            HeightScale = bounds.Height / BUFFER_HEIGHT;
            WidthScale = bounds.Width / BUFFER_WIDTH;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            AudioManager.LoadSounds();

            base.LoadContent();
        }
    }
}
