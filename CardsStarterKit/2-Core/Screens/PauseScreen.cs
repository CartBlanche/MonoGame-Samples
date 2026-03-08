//-----------------------------------------------------------------------------
// PauseScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

namespace CardsFramework.Core
{
    public class PauseScreen : MenuScreen
    {
        /// <summary>
        /// Initializes a new instance of the screen.
        /// </summary>
        readonly string returnText;
        readonly string quitText;

        public PauseScreen(string returnText = "Back", string quitText = "Quit")
            : base("Pause")
        {
            this.returnText = returnText;
            this.quitText = quitText;
            IsPopup = true;
        }

        public override void LoadContent()
        {
            // Create our menu entries.
            MenuEntry returnGameMenuEntry = new MenuEntry(returnText);
            MenuEntry exitMenuEntry = new MenuEntry(quitText);

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
            IPausable pausable = null;
            List<GameScreen> res = new List<GameScreen>();

            for (int screenIndex = 0; screenIndex < screens.Length; screenIndex++)
            {
                if (screens[screenIndex] is IPausable p)
                {
                    pausable = p;
                }
                else
                {
                    res.Add(screens[screenIndex]);
                }
            }

            foreach (GameScreen screen in res)
                screen.ExitScreen();

            pausable?.ReturnFromPause();
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
                if (screen is PauseScreen || screen is IPausable)
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