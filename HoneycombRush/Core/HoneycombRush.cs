//-----------------------------------------------------------------------------
// HoneycombRush.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------



using System;
using Microsoft.Xna.Framework;
using HoneycombRush.GameDebugTools;



namespace HoneycombRush
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class HoneycombRushGame : Game
    {


        GraphicsDeviceManager graphics;
        ScreenManager screenManager;

        public static string GameName = "Honeycomb Rush";

        DebugSystem debugSystem;





        public HoneycombRushGame()
        {
            // Initialize sound system
            AudioManager.Initialize(this);

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferHeight = 480;
            graphics.PreferredBackBufferWidth = 800;

            IsMouseVisible = true;

            // GamerServicesComponent is no longer needed in MonoGame 3.8.4
            // Components.Add(new GamerServicesComponent(this));

            Vector2 scaleVector = new Vector2(graphics.PreferredBackBufferWidth / 1280f,
                graphics.PreferredBackBufferHeight / 720f);

            UIConstants.SetScale(scaleVector);

            // Create a new instance of the Screen Manager. Have all drawing scaled from 720p to the PC's resolution
            screenManager = new ScreenManager(this, scaleVector);

            screenManager.AddScreen(new BackgroundScreen("titleScreen"), null);
            screenManager.AddScreen(new MainMenuScreen(), PlayerIndex.One);
            Components.Add(screenManager);
        }

        protected override void Initialize()
        {
            // Initialize the debug system with the game and the name of the font 
            // we want to use for the debugging
            debugSystem = DebugSystem.Initialize(this, @"Fonts\GameScreenFont16px");

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            // Tell the TimeRuler that we're starting a new frame. you always want
            // to call this at the start of Update
            debugSystem.TimeRuler.StartFrame();

            // Start measuring time for "Update".
            debugSystem.TimeRuler.BeginMark("Update", Color.Blue);

            base.Update(gameTime);

            // Stop measuring time for "Update".
            debugSystem.TimeRuler.EndMark("Update");
        }

        protected override void Draw(GameTime gameTime)
        {
            // Start measuring time for "Draw".
            debugSystem.TimeRuler.BeginMark("Draw", Color.Yellow);

            base.Draw(gameTime);

            // Stop measuring time for "Draw".
            debugSystem.TimeRuler.EndMark("Draw");
        }
    }
}