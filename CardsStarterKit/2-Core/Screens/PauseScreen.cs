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
        readonly Action _onQuit;

        public PauseScreen(string returnText = "Back", string quitText = "Quit", Action onQuit = null, string title = "Pause")
            : base(title)
        {
            this.returnText = returnText;
            this.quitText   = quitText;
            _onQuit         = onQuit;
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
            if (_onQuit != null)
            {
                _onQuit();
                return;
            }

            // Default: exit pause-related screens and return to whatever is beneath
            GameScreen[] screens = ScreenManager.GetScreens();
            foreach (GameScreen screen in screens)
            {
                if (screen is PauseScreen || screen is IPausable)
                {
                    screen.ExitScreen();
                }
                else if (screen is BackgroundScreen)
                {
                    int pauseIndex = System.Array.FindIndex(screens, s => s is PauseScreen);
                    int bgIndex    = System.Array.FindIndex(screens, s => s == screen);
                    if (bgIndex == pauseIndex - 1)
                        screen.ExitScreen();
                }
            }
        }
    }
}