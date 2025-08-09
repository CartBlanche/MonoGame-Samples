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

        /// <summary>
        /// Initializes a new instance of the game.
        /// </summary>
        public BlackjackGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            if (UIUtilty.IsMobile)
            {
                graphicsDeviceManager.IsFullScreen = true;
                IsMouseVisible = false;
            }
            else if (UIUtilty.IsDesktop)
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

            graphicsDeviceManager.PreferredBackBufferWidth = ScreenManager.BASE_BUFFER_WIDTH;
            graphicsDeviceManager.PreferredBackBufferHeight = ScreenManager.BASE_BUFFER_HEIGHT;
            graphicsDeviceManager.ApplyChanges();
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