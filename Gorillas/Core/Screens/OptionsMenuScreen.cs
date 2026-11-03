#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;

#endregion

namespace Gorillas
{
	/// <summary>
	/// The options screen is brought up over the top of the main menu
	/// screen, and gives the user a chance to configure the game
	/// in various hopefully useful ways.
	/// </summary>
	internal class OptionsMenuScreen : MenuScreen
	{
	#region Fields

		MenuEntry playerModeMenuEntry;
		MenuEntry entryModeMenuEntry;
		MenuEntry livesMenuEntry;
		MenuEntry skillLevelMenuEntry;
		
		internal enum EntryMode
		{
			KeyBoard,
			Mouse,
		}
		
		static string[] playerMode = {"Single Player - (Against AI)","Multi-Player (Local)","Multi-Player (Network)"};
		internal static EntryMode currentEntryMode = EntryMode.Mouse;
		internal static SkillLevel currentSkillLevel = SkillLevel.Beginner;
		internal static int numberOfLives = 3;
		internal static int currentPlayerMode = 0;

	#endregion

	#region Initialization


		/// <summary>
		/// Constructor.
		/// </summary>
		public OptionsMenuScreen ()
		: base("Options")
			{
			// Create our menu entries.
			playerModeMenuEntry = new MenuEntry (string.Empty);
			entryModeMenuEntry = new MenuEntry (string.Empty);
			livesMenuEntry = new MenuEntry (string.Empty);
			skillLevelMenuEntry = new MenuEntry (string.Empty);
			
			SetMenuEntryText ();

			MenuEntry back = new MenuEntry ("Back");

			// Hook up menu event handlers.
			
			playerModeMenuEntry.Selected += PlayerModeMenuEntrySelected;
			playerModeMenuEntry.Incremented += PlayerModeMenuEntryIncremented;
			playerModeMenuEntry.Decremented += PlayerModeMenuEntryDecremented;
			
			entryModeMenuEntry.Selected += EntryModeMenuEntrySelected;
			entryModeMenuEntry.Incremented += ModeMenuEntryIncremented;
			entryModeMenuEntry.Decremented += EntryModeMenuEntryDecremented;
			
			livesMenuEntry.Selected += LivesMenuEntrySelected;
			livesMenuEntry.Incremented += LivesMenuEntryIncremented;
			livesMenuEntry.Decremented += LivesMenuEntryDecremented;
			
			skillLevelMenuEntry.Selected += SkillLevelMenuEntrySelected;
			skillLevelMenuEntry.Incremented += SkillLevelMenuEntryIncremented;
			skillLevelMenuEntry.Decremented += SkillLevelMenuEntryDecremented;
			
			back.Selected += OnCancel;

			// Add entries to the menu.
			MenuEntries.Add (playerModeMenuEntry);
			MenuEntries.Add (entryModeMenuEntry);
			MenuEntries.Add (livesMenuEntry);
			MenuEntries.Add (skillLevelMenuEntry);
			MenuEntries.Add (back);
		}

		/// <summary>
		/// Fills in the latest values for the options screen menu text.
		/// </summary>
		void SetMenuEntryText ()
		{
			playerModeMenuEntry.Text = "Player Mode: " + playerMode[currentPlayerMode];
			entryModeMenuEntry.Text = "Entry Mode: " + (currentEntryMode == EntryMode.Mouse ? "Touch / Mouse" : "Keyboard");
			livesMenuEntry.Text = "Lives: " + numberOfLives;
			skillLevelMenuEntry.Text = "Skill Level: " + currentSkillLevel;
		}


	#endregion

	#region Handle Input

		void LivesMenuEntrySelected (object sender, PlayerIndexEventArgs e)
		{
			numberOfLives++;
			
			SetMenuEntryText ();
		}

		void LivesMenuEntryDecremented (object sender, PlayerIndexEventArgs e)
		{
			numberOfLives--;
			if (numberOfLives < 1)
				numberOfLives = 1;
			
			SetMenuEntryText ();			
		}

		void LivesMenuEntryIncremented (object sender, PlayerIndexEventArgs e)
		{
			numberOfLives++;
			
			SetMenuEntryText ();			
		}
		
		void EntryModeMenuEntrySelected (object sender, PlayerIndexEventArgs e)
		{
			currentEntryMode++;

			if (currentEntryMode > EntryMode.Mouse)
				currentEntryMode = 0;

			SetMenuEntryText ();			
		}

		void EntryModeMenuEntryDecremented (object sender, PlayerIndexEventArgs e)
		{
			currentEntryMode--;

			if (currentEntryMode < EntryMode.KeyBoard)
				currentEntryMode = EntryMode.Mouse;

			SetMenuEntryText ();			
		}

		void ModeMenuEntryIncremented (object sender, PlayerIndexEventArgs e)
		{
			currentEntryMode++;

			if (currentEntryMode > EntryMode.Mouse)
				currentEntryMode = EntryMode.KeyBoard;

			SetMenuEntryText ();			
		}

		
		void PlayerModeMenuEntrySelected (object sender, PlayerIndexEventArgs e)
		{
			currentPlayerMode = (currentPlayerMode + 1) % playerMode.Length;
			
			SetMenuEntryText ();			
		}

		void PlayerModeMenuEntryDecremented (object sender, PlayerIndexEventArgs e)
		{
			currentPlayerMode = (currentPlayerMode - 1) % playerMode.Length;
			if (currentPlayerMode < 0)
				currentPlayerMode = playerMode.Length - 1;
			
			SetMenuEntryText ();			
			
		}

		void PlayerModeMenuEntryIncremented (object sender, PlayerIndexEventArgs e)
		{
			currentPlayerMode = (currentPlayerMode + 1) % playerMode.Length;
			
			SetMenuEntryText ();			
			
		}

		void SkillLevelMenuEntrySelected (object sender, PlayerIndexEventArgs e)
		{
			currentSkillLevel++;

			if (currentSkillLevel > SkillLevel.Advanced)
				currentSkillLevel = SkillLevel.Beginner;

			SetMenuEntryText ();			
			
		}

		void SkillLevelMenuEntryDecremented (object sender, PlayerIndexEventArgs e)
		{
			currentSkillLevel--;

			if (currentSkillLevel < SkillLevel.Beginner)
				currentSkillLevel = SkillLevel.Advanced;

			SetMenuEntryText ();			
			
		}

		void SkillLevelMenuEntryIncremented (object sender, PlayerIndexEventArgs e)
		{
			currentSkillLevel++;

			if (currentSkillLevel > SkillLevel.Advanced)
				currentSkillLevel = SkillLevel.Beginner;

			SetMenuEntryText ();			
			
		}




	#endregion
	}
}
