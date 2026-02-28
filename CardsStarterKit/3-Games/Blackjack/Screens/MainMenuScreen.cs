//-----------------------------------------------------------------------------
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameStateManagement;

namespace Blackjack
{
    class MainMenuScreen : MenuScreen
    {
        // Store theme as invariant English value to avoid asset loading issues
        public static string Theme { get; set; } = "Red";
        private bool needsRefresh = false;

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
            BuildMenuEntries();
            base.LoadContent();

            AudioManager.PlayMusic("CasinoAmbiance", volumeMultiplier: 0.15f);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            // If we were covered and now we're not, refresh the menu entries
            // to pick up any language changes from the settings screen
            if (!coveredByOtherScreen && needsRefresh)
            {
                BuildMenuEntries();
                needsRefresh = false;
            }
            else if (coveredByOtherScreen)
            {
                needsRefresh = true;
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        private void BuildMenuEntries()
        {
            // Clear existing entries
            MenuEntries.Clear();

            // Create our menu entries.
            MenuEntry startGameMenuEntry = new MenuEntry(Resources.Play);
            MenuEntry settingsMenuEntry = new MenuEntry(Resources.Settings);
            MenuEntry aboutMenuEntry = new MenuEntry(Resources.About);
            MenuEntry exitMenuEntry = new MenuEntry(Resources.Exit);

            // Hook up menu event handlers.
            startGameMenuEntry.Selected += StartGameMenuEntrySelected;
            settingsMenuEntry.Selected += SettingsMenuEntrySelected;
            aboutMenuEntry.Selected += AboutMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(startGameMenuEntry);
            MenuEntries.Add(settingsMenuEntry);
            MenuEntries.Add(aboutMenuEntry);
            MenuEntries.Add(exitMenuEntry);
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
        /// Respond to "About" Item Selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AboutMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new AboutScreen(), null);
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