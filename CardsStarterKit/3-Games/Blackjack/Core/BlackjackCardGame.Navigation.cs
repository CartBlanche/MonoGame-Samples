//-----------------------------------------------------------------------------
// BlackjackCardGame.Navigation.cs
//
// Partial class containing navigation and gameplay-exit helpers.
//-----------------------------------------------------------------------------

using System;
using CardsFramework.Core;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        /// <summary>
        /// Removes all gameplay components (buttons, cards, chips, etc.) from Game.Components,
        /// leaving only the ScreenManager. Called when exiting gameplay for any reason.
        /// </summary>
        public void RemoveAllGameplayComponents()
        {
            for (int componentIndex = 0; componentIndex < Game.Components.Count; componentIndex++)
            {
                if (!(Game.Components[componentIndex] is ScreenManager))
                {
                    Game.Components.RemoveAt(componentIndex);
                    componentIndex--;
                }
            }
        }

        /// <summary>
        /// Legacy back-button handler that exits gameplay screens.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void backButton_Click(object sender, EventArgs e)
        {
            RemoveAllGameplayComponents();

            foreach (GameScreen screen in screenManager.GetScreens())
                screen.ExitScreen();

            screenManager.AddScreen(new BackgroundScreen(), null);
            screenManager.AddScreen(new MainMenuScreen(), null);
        }
    }
}
