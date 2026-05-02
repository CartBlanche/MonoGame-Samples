//-----------------------------------------------------------------------------
// WarlordsMainMenuScreen.cs
//
// Main menu for Warlords
//-----------------------------------------------------------------------------

using CardsFramework.Core;
using Microsoft.Xna.Framework;

namespace Warlords
{
    /// <summary>
    /// Main menu screen for Warlords
    /// </summary>
    class WarlordsMainMenuScreen : MenuScreen
    {
        public WarlordsMainMenuScreen()
            : base("Warlords: Iudicium Exodei Nefas")
        {
            // Create menu entries
            MenuEntry playGameMenuEntry = new MenuEntry("Play Game");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");

            // Hook up menu event handlers
            playGameMenuEntry.Selected += PlayGameMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu
            MenuEntries.Add(playGameMenuEntry);
            MenuEntries.Add(exitMenuEntry);
        }

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void PlayGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Remove all existing screens
            foreach (GameScreen screen in ScreenManager.GetScreens())
                screen.ExitScreen();

            // Add gameplay screen
            ScreenManager.AddScreen(new WarlordsGameplayScreen("Default"), null);
        }

        /// <summary>
        /// When the user cancels the main menu, exit the game
        /// </summary>
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.Game.Exit();
        }
    }
}
