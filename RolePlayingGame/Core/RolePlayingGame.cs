//-----------------------------------------------------------------------------
// RolePlayingGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RolePlaying.Data;

namespace RolePlaying
{
    /// <summary>
    /// The Game object for the Role-Playing Game starter kit.
    /// </summary>
    public class RolePlayingGame : Game
    {
        GraphicsDeviceManager graphicsDeviceManager;
        ScreenManager screenManager;

        /// <summary>
        /// Indicates if the game is running on a mobile platform.
        /// </summary>
        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Indicates if the game is running on a desktop platform.
        /// </summary>
        public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        /// <summary>
        /// Create a new RolePlayingGame object.
        /// </summary>
        public RolePlayingGame()
        {
            // initialize the graphics system
            graphicsDeviceManager = new GraphicsDeviceManager(this);
            graphicsDeviceManager.PreferredBackBufferWidth = Session.BACK_BUFFER_WIDTH;
            graphicsDeviceManager.PreferredBackBufferHeight = Session.BACK_BUFFER_HEIGHT;

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

            // configure the content manager
            Content.RootDirectory = "Content";

            // Configure screen orientations.
            graphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;

            // add a gamer-services component, which is required for the storage APIs
            // TODO: GamerServicesComponent was XNA-specific and not available in MonoGame
            // Components.Add(new GamerServicesComponent(this));

            // add the audio manager
            AudioManager.Initialize(this,
                Path.Combine("Content", "Audio", "RpgAudio.xgs"),
                Path.Combine("Content", "Audio", "Wave Bank.xwb"),
                Path.Combine("Content", "Audio", "Sound Bank.xsb"));

            // add the screen manager
            screenManager = new ScreenManager(this);
            Components.Add(screenManager);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to 
        /// before starting to run.  This is where it can query for any required 
        /// services and load any non-graphic related content.  Calling base.Initialize 
        /// will enumerate through any components and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            InputManager.Initialize();

            base.Initialize();

            TileEngine.Viewport = graphicsDeviceManager.GraphicsDevice.Viewport;

            screenManager.AddScreen(new MainMenuScreen());
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            Fonts.LoadContent(Content);

            base.LoadContent();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            Fonts.UnloadContent();

            base.UnloadContent();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            InputManager.Update();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphicsDeviceManager.GraphicsDevice.Clear(Color.Transparent);

            base.Draw(gameTime);
        }
    }
}