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
        readonly string backgroundPath;
        readonly Rectangle? sourceRect;

        /// <summary>
        /// Initializes a new instance of the screen.
        /// </summary>
        /// <param name="backgroundImagePath">
        /// Content path of the background texture (without extension).
        /// Defaults to "Images/titlescreen" for backward compatibility.
        /// </param>
        /// <param name="sourceRect">
        /// Optional source rectangle to crop the texture (e.g. one quadrant of a sprite sheet).
        /// Null draws the full texture.
        /// </param>
        public BackgroundScreen(string backgroundImagePath = "Images/titlescreen", Rectangle? sourceRect = null)
        {
            backgroundPath = backgroundImagePath;
            this.sourceRect = sourceRect;
            TransitionOnTime = TimeSpan.FromSeconds(0.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        /// <summary>
        /// Load graphics content for the screen.
        /// </summary>
        public override void LoadContent()
        {
            background = ScreenManager.Game.Content.Load<Texture2D>(backgroundPath);
            safeArea = ScreenManager.SafeArea;
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

            ScreenManager.SpriteBatch.Draw(background, safeArea, sourceRect, Color.White * TransitionAlpha);

            ScreenManager.SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}