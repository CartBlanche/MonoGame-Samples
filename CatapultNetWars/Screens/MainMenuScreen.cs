#region File Description
//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
#endregion

namespace CatapultGame
{
    class MainMenuScreen : MenuScreen
    {
        #region Initialization
        public MainMenuScreen()
            : base(String.Empty)
        {
            IsPopup = true;

            // Create our menu entries.
            MenuEntry startGameMenuEntry = new MenuEntry("Play");
            MenuEntry startMultiPlayerGameMenuEntry = new MenuEntry("Multi-Player");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");

            // Hook up menu event handlers.
            startGameMenuEntry.Selected += StartGameMenuEntrySelected;
            startMultiPlayerGameMenuEntry.Selected += StartMultiPlayerGameMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(startGameMenuEntry);
            MenuEntries.Add(startMultiPlayerGameMenuEntry);
            MenuEntries.Add(exitMenuEntry);
        }

        #endregion

        #region Overrides
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
        #endregion

        #region Event Handlers for Menu Items
        /// <summary>
        /// Handles "Play" menu item selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void StartGameMenuEntrySelected(object sender, EventArgs e)
        {
		// Lets make sure we get rid of our network session
		// so we can start up clean
		ScreenManager.Game.Services.RemoveService(typeof(NetworkSession));
            ScreenManager.AddScreen(new InstructionsScreen(), null);
        }

        /// <summary>
        /// Handles "Exit" menu item selection
        /// </summary>
        /// 
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.Game.Exit();
        }

        void StartMultiPlayerGameMenuEntrySelected (object sender, PlayerIndexEventArgs e)
        {
		CreateOrFindSession (NetworkSessionType.SystemLink, e.PlayerIndex);
        }

		/// <summary>
		/// Helper method shared by the Live and System Link menu event handlers.
		/// </summary>
		void CreateOrFindSession (NetworkSessionType sessionType,
				PlayerIndex playerIndex)
		{

			// First, we need to make sure a suitable gamer profile is signed in.
			ProfileSignInScreen profileSignIn = new ProfileSignInScreen (sessionType);

			// Hook up an event so once the ProfileSignInScreen is happy,
			// it will activate the CreateOrFindSessionScreen.
			profileSignIn.ProfileSignedIn += delegate
				{
					GameScreen createOrFind = new CreateOrFindSessionScreen (sessionType);

					ScreenManager.AddScreen (createOrFind, playerIndex);
				};

			// Activate the ProfileSignInScreen.
			ScreenManager.AddScreen (profileSignIn, playerIndex);
		}


        #endregion
    }
}
