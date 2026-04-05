//-----------------------------------------------------------------------------
// BackgroundScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;

namespace CardsFramework.Core
{
    public class BackgroundScreen : GameScreen
    {
        Texture2D background;
        Rectangle safeArea;

        string _imagePath;
        Rectangle? _sourceRect;

        /// <summary>
        /// Initializes a new instance of the screen.
        /// </summary>
        public BackgroundScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        /// <summary>
        /// Initializes a new instance with a custom image path and source rectangle.
        /// </summary>
        public BackgroundScreen(string imagePath, Rectangle sourceRect) : this()
        {
            _imagePath  = imagePath;
            _sourceRect = sourceRect;
        }

        /// <summary>
        /// Load graphics content for the screen.
        /// </summary>
        public override void LoadContent()
        {
            string path = _imagePath ?? Path.Combine("Images", "titlescreen");
            background = ScreenManager.Game.Content.Load<Texture2D>(path);
            safeArea = new Rectangle(0, 0, ScreenManager.BASE_BUFFER_WIDTH, ScreenManager.BASE_BUFFER_HEIGHT);
            base.LoadContent();
        }

        /// <summary>
        /// Allows the screen to run logic, such as updating the transition position.
        /// Unlike HandleInput, this method is called regardless of whether the screen
        /// is active, hidden, or in the middle of a transition.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="otherScreenHasFocus"></param>
        /// <param name="coveredByOtherScreen"></param>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
            bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);
        }

        /// <summary>
        /// This is called when the screen should draw itself.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

            if (_sourceRect.HasValue)
                ScreenManager.SpriteBatch.Draw(background, safeArea, _sourceRect.Value, Color.White * TransitionAlpha);
            else
                ScreenManager.SpriteBatch.Draw(background, safeArea, Color.White * TransitionAlpha);

            ScreenManager.SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}