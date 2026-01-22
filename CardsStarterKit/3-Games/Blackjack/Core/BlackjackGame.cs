//-----------------------------------------------------------------------------
// BlackjackGame.cs
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

            // Show splash screen on startup (it will add BackgroundScreen and MainMenuScreen after 3 seconds)
            screenManager.AddScreen(new SplashScreen(), null);

            Components.Add(screenManager);

            // Initialize sound system
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
            AudioManager.LoadSounds();
            AudioManager.LoadMusic();

            base.LoadContent();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (UIUtility.IsDesktop)
            {
                KeyboardState keyboardState = Keyboard.GetState();

                // Check if Alt+Enter is pressed
                if ((keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt))
                    && keyboardState.IsKeyDown(Keys.Enter))
                {
                    // Use a static flag to prevent multiple toggles on held keys
                    if (!wasFullscreenTogglePressed)
                    {
                        graphicsDeviceManager.ToggleFullScreen();

                        wasFullscreenTogglePressed = true;
                    }
                }
                else
                {
                    wasFullscreenTogglePressed = false;
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Clear the screen to prevent rendering artifacts when switching between windowed and fullscreen
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
        }

        // Flag to prevent multiple fullscreen toggles when keys are held
        private bool wasFullscreenTogglePressed = false;
    }
}