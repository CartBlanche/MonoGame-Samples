//-----------------------------------------------------------------------------
// PauseScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace CatapultGame
{
    class PauseScreen : MenuScreen
    {
        GameScreen backgroundScreen;
        Player human;
        Player computer;
        bool prevHumanIsActive;
        bool prevCompuerIsActive;

        public PauseScreen(GameScreen backgroundScreen, Player human, Player computer)
            : base(String.Empty)
        {
            IsPopup = true;

            this.backgroundScreen = backgroundScreen;

            // Create our menu entries.
            MenuEntry startGameMenuEntry = new MenuEntry("Return");
            MenuEntry exitMenuEntry = new MenuEntry("Quit Game");

            // Hook up menu event handlers.
            startGameMenuEntry.Selected += StartGameMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(startGameMenuEntry);
            MenuEntries.Add(exitMenuEntry);

            this.human = human;
            this.computer = computer;

            // Preserve the old state of the game
            prevHumanIsActive = this.human.Catapult.IsActive;
            prevCompuerIsActive = this.computer.Catapult.IsActive;

            // Pause the game logic progress
            this.human.Catapult.IsActive = false;
            this.computer.Catapult.IsActive = false;

            AudioManager.PauseResumeSounds(false);
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
        /// Handles "Return" menu item selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void StartGameMenuEntrySelected(object sender, EventArgs e)
        {
            human.Catapult.IsActive = prevHumanIsActive;
            computer.Catapult.IsActive = prevCompuerIsActive;

            if (!(human as Human).isDragging)
                AudioManager.PauseResumeSounds(true);
            else
            {
                (human as Human).ResetDragState();
                AudioManager.StopSounds();
            }

            backgroundScreen.ExitScreen();
            ExitScreen();
        }

        /// <summary>
        /// Handles "Exit" menu item selection
        /// </summary>
        /// 
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            // Tear down our network session
            NetworkSession session = ScreenManager.Game.Services.GetService(typeof(NetworkSession)) as NetworkSession;
            if (session != null)
            {
                if (session.AllGamers.Count == 1)
                {
                    session.EndGame();
                }
                session.Dispose();
                ScreenManager.Game.Services.RemoveService(typeof(NetworkSession));
            }
            AudioManager.StopSounds();
            ScreenManager.AddScreen(new MainMenuScreen(), null);
            ExitScreen();
        }
    }
}