//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    class MainMenuScreen : MenuScreen
    {
        public static string Theme { get; set; } = "Red";

        /// <summary>
        /// Initializes a new instance of the screen.
        /// </summary>
        public MainMenuScreen()
            : base("")
        {
            // Load theme from settings
            Theme = GameSettings.Instance.Theme;
        }

        public override void LoadContent()
        {
            // Create our menu entries.
            MenuEntry startGameMenuEntry = new MenuEntry(Resources.Play);
            MenuEntry settingsMenuEntry = new MenuEntry(Resources.Settings);
            MenuEntry exitMenuEntry = new MenuEntry(Resources.Exit);

            // Hook up menu event handlers.
            startGameMenuEntry.Selected += StartGameMenuEntrySelected;
            settingsMenuEntry.Selected += SettingsMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(startGameMenuEntry);
            MenuEntries.Add(settingsMenuEntry);
            MenuEntries.Add(exitMenuEntry);

            base.LoadContent();
        }

        /// <summary>
        /// Respond to "Play" Item Selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void StartGameMenuEntrySelected(object sender, EventArgs e)
        {
            foreach (GameScreen screen in ScreenManager.GetScreens())
                screen.ExitScreen();

            // Don't add BackgroundScreen - we don't want the logo on the session browser
            ScreenManager.AddScreen(new SessionBrowserScreen(), null);
        }

        /// <summary>
        /// Respond to "Settings" Item Selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SettingsMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new SettingsScreen(), null);
        }

        /// <summary>
        /// Respond to "Exit" Item Selection
        /// </summary>
        /// <param name="playerIndex"></param>
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.Game.Exit();
        }
    }
}