//-----------------------------------------------------------------------------
// PauseScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GameStateManagement;
using Microsoft.Xna.Framework;

namespace Blackjack
{
    class PauseScreen : MenuScreen
    {
        /// <summary>
        /// Initializes a new instance of the screen.
        /// </summary>
        public PauseScreen()
            : base("Pause")
        {
            IsPopup = true;
        }

        public override void LoadContent()
        {
            // Create our menu entries.
            MenuEntry returnGameMenuEntry = new MenuEntry(Resources.Back);
            MenuEntry exitMenuEntry = new MenuEntry(Resources.Quit);

            // Hook up menu event handlers.
            returnGameMenuEntry.Selected += ReturnGameMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(returnGameMenuEntry);
            MenuEntries.Add(exitMenuEntry);

            base.LoadContent();
        }

        /// <summary>
        /// Respond to "Return" Item Selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ReturnGameMenuEntrySelected(object sender, EventArgs e)
        {
            GameScreen[] screens = ScreenManager.GetScreens();
            GameplayScreen gameplayScreen = null;
            List<GameScreen> res = new List<GameScreen>();

            for (int screenIndex = 0; screenIndex < screens.Length; screenIndex++)
            {
                if (screens[screenIndex] is GameplayScreen)
                {
                    gameplayScreen = (GameplayScreen)screens[screenIndex];
                }
                else
                {
                    res.Add(screens[screenIndex]);
                }
            }

            foreach (GameScreen screen in res)
                screen.ExitScreen();

            gameplayScreen.ReturnFromPause();
        }

        /// <summary>
        /// Respond to "Quit Game" Item Selection
        /// </summary>
        /// <param name="playerIndex"></param>
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            // Exit only the pause-related screens: PauseScreen, BackgroundScreen (pause overlay), and GameplayScreen
            // This will return to the LobbyScreen beneath them
            GameScreen[] screens = ScreenManager.GetScreens();

            foreach (GameScreen screen in screens)
            {
                // Exit only if it's one of the pause screens or gameplay
                if (screen is PauseScreen || screen is GameplayScreen)
                {
                    screen.ExitScreen();
                }
                // Also exit the BackgroundScreen that was added for the pause overlay
                // It should be the one right before PauseScreen
                else if (screen is BackgroundScreen)
                {
                    // Check if this is the pause BackgroundScreen (there might be others in the stack)
                    int pauseIndex = System.Array.FindIndex(screens, s => s is PauseScreen);
                    int bgIndex = System.Array.FindIndex(screens, s => s == screen);
                    if (bgIndex == pauseIndex - 1)
                    {
                        screen.ExitScreen();
                    }
                }
            }
        }
    }
}