#region File Description
//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;
#endregion

namespace Gorillas
{
	/// <summary>
	/// The main menu screen is the first thing displayed when the game starts up.
	/// </summary>
	class MainMenuScreen : MenuScreen
	{
	#region Initialization


		/// <summary>
		/// Constructor fills in the menu contents.
		/// </summary>
		public MainMenuScreen ()
		: base("Main Menu")
			{
			// Create our menu entries.
			MenuEntry playGameMenuEntry = new MenuEntry ("Play");
			//MenuEntry systemLinkMenuEntry = new MenuEntry ("SystemLink");			
			MenuEntry optionsMenuEntry = new MenuEntry ("Options");
			MenuEntry exitMenuEntry = new MenuEntry ("Exit");

			// Hook up menu event handlers.
			playGameMenuEntry.Selected += PlayGameMenuEntrySelected;
			optionsMenuEntry.Selected += OptionsMenuEntrySelected;
			exitMenuEntry.Selected += OnCancel;
			//systemLinkMenuEntry.Selected += SystemLinkMenuEntrySelected;

			// Add entries to the menu.
			MenuEntries.Add (playGameMenuEntry);
			//MenuEntries.Add (systemLinkMenuEntry);
			MenuEntries.Add (optionsMenuEntry);
			MenuEntries.Add (exitMenuEntry);
		}


	#endregion

	#region Handle Input


		/// <summary>
		/// Event handler for when the Play Game menu entry is selected.
		/// </summary>
		void PlayGameMenuEntrySelected (object sender, PlayerIndexEventArgs e)
		{
			if (OptionsMenuScreen.currentPlayerMode < 2) {
//				LoadingScreen.Load (ScreenManager, true, e.PlayerIndex, 
//					new GameplayScreen (null));
				SigninScreen(NetworkSessionType.Local, e.PlayerIndex);
			}
			else {
				CreateOrFindSession (NetworkSessionType.SystemLink, e.PlayerIndex);
			}
		}
		
		void SigninScreen(NetworkSessionType sessionType, PlayerIndex playerIndex) {
			// First, we need to make sure a suitable gamer profile is signed in.
			ProfileSignInScreen profileSignIn = new ProfileSignInScreen (sessionType);

			// Hook up an event so once the ProfileSignInScreen is happy,
			// it will activate the CreateOrFindSessionScreen.
			profileSignIn.ProfileSignedIn += delegate {
				if (OptionsMenuScreen.currentPlayerMode != 1) {
					LoadingScreen.Load (ScreenManager, true, playerIndex, 
						new GameplayScreen (null));
				}
				else {
					if (Gamer.SignedInGamers.Count < 2) {
						// Activate the ProfileSignInScreen.
						
						ScreenManager.AddScreen (profileSignIn, PlayerIndex.Two);	
					}
					else {
						LoadingScreen.Load (ScreenManager, true, playerIndex, 
							new GameplayScreen (null));						
					}
				}
			};

			// Activate the ProfileSignInScreen.
			ScreenManager.AddScreen (profileSignIn, playerIndex);			
		}

		/// <summary>
		/// Event handler for when the Options menu entry is selected.
		/// </summary>
		void OptionsMenuEntrySelected (object sender, PlayerIndexEventArgs e)
		{
			ScreenManager.AddScreen (new OptionsMenuScreen (), e.PlayerIndex);
		}

		/// <summary>
		/// Event handler for when the System Link menu entry is selected.
		/// </summary>
		void SystemLinkMenuEntrySelected (object sender, PlayerIndexEventArgs e)
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
			profileSignIn.ProfileSignedIn += delegate {
				GameScreen createOrFind = new CreateOrFindSessionScreen (sessionType);
		
				ScreenManager.AddScreen (createOrFind, playerIndex);
			};

			// Activate the ProfileSignInScreen.
			ScreenManager.AddScreen (profileSignIn, playerIndex);
		}
		
		/// <summary>
		/// When the user cancels the main menu, ask if they want to exit the sample.
		/// </summary>
		protected override void OnCancel (PlayerIndex playerIndex)
		{
		const
			string message = "Are you sure you want to exit this sample?" ; 

			MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen (message);

			confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;

			ScreenManager.AddScreen (confirmExitMessageBox, playerIndex);
		}


		/// <summary>
		/// Event handler for when the user selects ok on the "are you sure
		/// you want to exit" message box.
		/// </summary>
		void ConfirmExitMessageBoxAccepted (object sender, PlayerIndexEventArgs e)
		{
			ScreenManager.Game.Exit ();
		}


	#endregion
	}
}
