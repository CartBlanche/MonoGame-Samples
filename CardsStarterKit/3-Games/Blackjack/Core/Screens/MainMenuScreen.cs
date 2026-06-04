//-----------------------------------------------------------------------------
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using CardsFramework.Core;

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
            MenuEntry achievementsMenuEntry = new MenuEntry(AchievementLocalization.GetAchievementsText());
            MenuEntry settingsMenuEntry = new MenuEntry(Resources.Settings);
            MenuEntry aboutMenuEntry = new MenuEntry(Resources.About);
            MenuEntry exitMenuEntry = new MenuEntry(Resources.Exit);

            // Hook up menu event handlers.
            startGameMenuEntry.Selected += StartGameMenuEntrySelected;
            achievementsMenuEntry.Selected += AchievementsMenuEntrySelected;
            settingsMenuEntry.Selected += SettingsMenuEntrySelected;
            aboutMenuEntry.Selected += AboutMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(startGameMenuEntry);
            MenuEntries.Add(achievementsMenuEntry);
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

        void AchievementsMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new BlackjackAchievementsScreen(), null);
        }

        /// <summary>
        /// Respond to "About" Item Selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AboutMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new BlackjackAboutScreen(), null);
        }

        /// <summary>
        /// Respond to "Exit" Item Selection
        /// </summary>
        /// <param name="playerIndex"></param>
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.Game.Exit();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            string status = LeaderboardPlatformStatus.GetLeaderboardStatusMessage();
            if (string.IsNullOrWhiteSpace(status))
            {
                return;
            }

            var spriteBatch = ScreenManager.SpriteBatch;
            var bounds = ScreenManager.SafeArea;
            const float statusScale = 0.90f;

            Vector2 statusSize = ScreenManager.RegularFont.MeasureString(status);
            float statusX = bounds.Left + (bounds.Width - statusSize.X * statusScale) / 2f;
            float statusY = bounds.Bottom - statusSize.Y * statusScale - 12f;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
            spriteBatch.DrawString(
                ScreenManager.RegularFont,
                status,
                new Vector2(statusX, statusY),
                new Color(126, 99, 62) * 0.82f * TransitionAlpha,
                0f,
                Vector2.Zero,
                statusScale,
                SpriteEffects.None,
                0f);
            spriteBatch.End();
        }

        public override void UpdateMenuEntryDestination()
        {
            Rectangle bounds = ScreenManager.SafeArea;
            Rectangle textureSize = ScreenManager.ButtonBackground.Bounds;

            if (MenuEntries.Count == 0)
                return;

            int maxWidth = 0;
            for (int i = 0; i < MenuEntries.Count; i++)
            {
                int width = MenuEntries[i].GetWidth(this);
                if (width > maxWidth)
                    maxWidth = width;
            }

            maxWidth += 20;

            const int verticalGap = 8;
            const int rightMargin = 24;
            const int bottomMargin = 24;
            int stackHeight = MenuEntries.Count * 50 + (MenuEntries.Count - 1) * verticalGap;
            int x = bounds.Right - rightMargin - maxWidth;
            int y = bounds.Bottom - bottomMargin - stackHeight;

            // If we run out of room vertically, fall back to base layout.
            if (y < bounds.Top + 12)
            {
                base.UpdateMenuEntryDestination();
                return;
            }

            for (int i = 0; i < MenuEntries.Count; i++)
            {
                MenuEntries[i].Destination = new Rectangle(x, y, maxWidth, 50);
                y += 50 + verticalGap;
            }
        }
    }
}