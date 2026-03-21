//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Main menu for the blank card game
//-----------------------------------------------------------------------------


using Microsoft.Xna.Framework;
using CardsFramework.Core;

namespace Blank
{
    /// <summary>
    /// Main menu screen for the game.
    /// </summary>
    class MainMenuScreen : MenuScreen
    {
        public MainMenuScreen()
            : base("Blank Card Game")
        {
        }

        public override void LoadContent()
        {
            // Add menu entries
            MenuEntry playGameMenuEntry = new MenuEntry("Play Game");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");

            playGameMenuEntry.Selected += PlayGameMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            MenuEntries.Add(playGameMenuEntry);
            MenuEntries.Add(exitMenuEntry);

            base.LoadContent();
        }

        void PlayGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new GameplayScreen(), e.PlayerIndex);
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.Game.Exit();
        }
    }
}
