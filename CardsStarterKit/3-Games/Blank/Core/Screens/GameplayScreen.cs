//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Main gameplay screen
//-----------------------------------------------------------------------------

using System;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blank
{
    /// <summary>
    /// Main gameplay screen.
    /// This is where your game logic and rendering happens.
    /// </summary>
    class GameplayScreen : GameScreen
    {
        BlankCardGame cardGame;
        Rectangle safeArea;

        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            safeArea = ScreenManager.SafeArea;

            // Create the card game instance
            cardGame = new BlankCardGame(safeArea, ScreenManager);

            // Add players (customize for your game)
            var player = new BlankPlayer("Player 1", cardGame);
            cardGame.AddPlayer(player);

            // Load card assets and set up UI components.
            // See BlankCardGame.UIOrchestration.cs to add buttons and enable card rendering.
            cardGame.Initialize();

            // Deal first hand and start game logic.
            cardGame.StartPlaying();

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
        }

        public override void HandleInput(InputState input)
        {
            if (input.IsPauseGame(null))
            {
                ScreenManager.AddScreen(new PauseScreen(), null);
            }

            // Add your input handling here

            base.HandleInput(input);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (!coveredByOtherScreen && !otherScreenHasFocus)
            {
                // Update your game logic here
                cardGame?.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target, Color.DarkGreen, 0, 0);

            // Draw your game here
            cardGame?.Draw(gameTime);

            base.Draw(gameTime);
        }
    }
}
