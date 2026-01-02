//-----------------------------------------------------------------------------
// PauseScreen.cs
//
// Minimal pause screen for Blank game
//-----------------------------------------------------------------------------

using GameStateManagement;
using Microsoft.Xna.Framework;

namespace Blank
{
    /// <summary>
    /// Simple pause screen.
    /// </summary>
    class PauseScreen : MenuScreen
    {
        public PauseScreen()
            : base("Paused")
        {
            IsPopup = true;
        }

        public override void LoadContent()
        {
            MenuEntry resumeMenuEntry = new MenuEntry("Resume");
            MenuEntry quitMenuEntry = new MenuEntry("Quit to Main Menu");

            resumeMenuEntry.Selected += OnCancel;
            quitMenuEntry.Selected += QuitMenuEntrySelected;

            MenuEntries.Add(resumeMenuEntry);
            MenuEntries.Add(quitMenuEntry);

            base.LoadContent();
        }

        void QuitMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Remove all screens and go back to main menu
            foreach (GameScreen screen in ScreenManager.GetScreens())
            {
                screen.ExitScreen();
            }

            ScreenManager.AddScreen(new MainMenuScreen(), null);
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ExitScreen();
        }
    }
}
