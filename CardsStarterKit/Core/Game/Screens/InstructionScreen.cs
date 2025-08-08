//-----------------------------------------------------------------------------
// InstructionScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Microsoft.Xna.Framework;
using GameStateManagement;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.IO;
using CardsFramework;

namespace Blackjack
{
    class InstructionScreen : GameplayScreen
    {
        Texture2D background;
        SpriteFont font;
        GameplayScreen gameplayScreen;
        string theme;
        bool isExit = false;
        bool isExited = false;

        public InstructionScreen(string theme)
            : base("")
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            this.theme = theme;

            EnabledGestures = GestureType.Tap;

        }

        /// <summary>
        /// Load the screen resources
        /// </summary>
        public override void LoadContent()
        {
            background = Load<Texture2D>(Path.Combine("Images", "instructions"));
            font = Load<SpriteFont>(Path.Combine("Fonts", "MenuFont"));

            // Create a new instance of the gameplay screen
            gameplayScreen = new GameplayScreen(theme);
        }

        /// <summary>
        /// Exit the screen after a tap or click
        /// </summary>
        /// <param name="input"></param>
        private void HandleInput()
        {
            if (!isExit)
            {
                if (UIUtilty.IsMobile)
                {
                    if (ScreenManager.InputState.Gestures.Count > 0 &&
                        ScreenManager.InputState.Gestures[0].GestureType == GestureType.Tap)
                    {
                        isExit = true;
                    }
                }
                else
                {
                    PlayerIndex result;
                    if (ScreenManager.InputState.CurrentMouseState.LeftButton == ButtonState.Pressed)
                    {
                        isExit = true;
                    }
                    else if (ScreenManager.InputState.IsNewButtonPress(Buttons.A, null, out result) ||
                             ScreenManager.InputState.IsNewButtonPress(Buttons.Start, null, out result))
                    {
                        isExit = true;
                    }
                }
            }
        }

        /// <summary>
        /// Screen update logic
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="otherScreenHasFocus"></param>
        /// <param name="coveredByOtherScreen"></param>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (isExit && !isExited)
            {
                // Move on to the gameplay screen
                foreach (GameScreen screen in ScreenManager.GetScreens())
                    screen.ExitScreen();

                gameplayScreen.ScreenManager = ScreenManager;
                ScreenManager.AddScreen(gameplayScreen, null);
                isExited = true;
            }

            HandleInput();

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        /// <summary>
        /// Render screen 
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

            // Draw Background
            spriteBatch.Draw(background, ScreenManager.SafeArea, Color.White * TransitionAlpha);

            if (isExit)
            {
                Rectangle safeArea = ScreenManager.SafeArea;
                string text = "Loading...";
                Vector2 measure = font.MeasureString(text);
                Vector2 textPosition = new Vector2(safeArea.Center.X - measure.X / 2,
                    safeArea.Center.Y - measure.Y / 2);
                spriteBatch.DrawString(font, text, textPosition, Color.Black);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}