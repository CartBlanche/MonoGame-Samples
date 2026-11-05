//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
#if !SINGLE_PLAYER
using Microsoft.Xna.Framework.Net;
#endif

namespace CatapultGame
{
    class MainMenuScreen : MenuScreen
    {
        public MainMenuScreen()
            : base(String.Empty)
        {
            IsPopup = true;

            // Create our menu entries.
            MenuEntry playMenuEntry = new MenuEntry("Play");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");
            MenuEntry themeMenuEntry = new MenuEntry(ThemeEntryText());

            // Hook up menu event handlers.
            playMenuEntry.Selected += PlayMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;
            themeMenuEntry.Selected += delegate
            {
                Theme.SeasonalEnabled = !Theme.SeasonalEnabled;
                themeMenuEntry.Text = ThemeEntryText();
            };

            // Add entries to the menu.
            MenuEntries.Add(playMenuEntry);

            // Only offer the switch while a seasonal theme is on offer.
            if (Theme.SeasonAvailable)
                MenuEntries.Add(themeMenuEntry);

#if !SINGLE_PLAYER
            MenuEntries.Add(exitMenuEntry);
#endif
        }

        static string ThemeEntryText()
        {
            return "Seasonal Theme: " + (Theme.SeasonalEnabled ? "On" : "Off");
        }


        protected override void UpdateMenuEntryLocations()
        {
            base.UpdateMenuEntryLocations();

            foreach (var entry in MenuEntries)
            {
                Vector2 position = entry.Position;

                position.Y += 60;

                entry.Position = position;
            }
        }

        /// <summary>
        /// Handles "Play" menu item selection
        /// </summary>
        void PlayMenuEntrySelected(object sender, EventArgs e)
        {
#if SINGLE_PLAYER
            // The browser build is single player only, so there is nothing to choose between
            MatchSettings.WinScore = MatchSettings.DefaultWinScore;
            ScreenManager.AddScreen(new InstructionsScreen(), null);
#else
            ScreenManager.AddScreen(new PlayMenuScreen(), null);
#endif
        }

        /// <summary>
        /// Handles "Exit" menu item selection
        /// </summary>
        /// 
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.Game.Exit();
        }

        /// <summary>
        /// There is nowhere to go back to from here, and Escape should not close the game by accident.
        /// Choosing Exit still does.
        /// </summary>
        protected override void OnBack(PlayerIndex playerIndex)
        {
        }
    }
}