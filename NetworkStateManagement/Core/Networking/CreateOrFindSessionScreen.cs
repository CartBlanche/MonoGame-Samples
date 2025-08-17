//-----------------------------------------------------------------------------
// CreateOrFindSessionScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;
using System.Threading.Tasks;


namespace NetworkStateManagement
{
	/// <summary>
	/// This menu screen lets the user choose whether to create a new
	/// network session, or search for an existing session to join.
	/// </summary>
	class CreateOrFindSessionScreen : MenuScreen
	{

		NetworkSessionType sessionType;




		/// <summary>
		/// Constructor fills in the menu contents.
		/// </summary>
		public CreateOrFindSessionScreen(NetworkSessionType sessionType)
		: base(GetMenuTitle(sessionType))
		{
			this.sessionType = sessionType;

			// Create our menu entries.
			MenuEntry createSessionMenuEntry = new MenuEntry(Resources.CreateSession);
			MenuEntry findSessionsMenuEntry = new MenuEntry(Resources.FindSessions);
			MenuEntry backMenuEntry = new MenuEntry(Resources.Back);

			// Hook up menu event handlers.
			createSessionMenuEntry.Selected += CreateSessionMenuEntrySelected;
			findSessionsMenuEntry.Selected += FindSessionsMenuEntrySelected;
			backMenuEntry.Selected += OnCancel;

			// Add entries to the menu.
			MenuEntries.Add(createSessionMenuEntry);
			MenuEntries.Add(findSessionsMenuEntry);
			MenuEntries.Add(backMenuEntry);
		}


		/// <summary>
		/// Helper chooses an appropriate menu title for the specified session type.
		/// </summary>
		static string GetMenuTitle(NetworkSessionType sessionType)
		{
			switch (sessionType)
			{
				case NetworkSessionType.PlayerMatch:
					return Resources.PlayerMatch;

				case NetworkSessionType.SystemLink:
					return Resources.SystemLink;

				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Event handler for when the Create Session menu entry is selected.
		/// </summary>
		void CreateSessionMenuEntrySelected(object sender, PlayerIndexEventArgs e)
		{
			try
			{
				// Which local profiles should we include in this session?
				IEnumerable<SignedInGamer> localGamers = NetworkSessionComponent.ChooseGamers(sessionType, ControllingPlayer.Value);

				// Begin an asynchronous create network session operation.
				var networkSession = NetworkSession.CreateAsync(
						sessionType,
						NetworkSessionComponent.MaxLocalGamers,
						NetworkSessionComponent.MaxGamers,
						1,
						null);

				// Activate the network busy screen, which will display
				// an animation until this operation has completed.
				NetworkBusyScreen<NetworkSession> busyScreen = new NetworkBusyScreen<NetworkSession>(networkSession);

				busyScreen.OperationCompleted += CreateSessionOperationCompleted;

				ScreenManager.AddScreen(busyScreen, ControllingPlayer);
			}
			catch (Exception exception)
			{
				NetworkErrorScreen errorScreen = new NetworkErrorScreen(exception);

				ScreenManager.AddScreen(errorScreen, ControllingPlayer);
			}
		}

		enum SessionProperty { GameMode, SkillLevel, ScoreToWin }

		enum GameMode { Practice, Timed, CaptureTheFlag }

		enum SkillLevel { Beginner, Intermediate, Advanced }

		/// <summary>
		/// Event handler for when the asynchronous create network session
		/// operation has completed.
		/// </summary>
		void CreateSessionOperationCompleted(object sender,
					OperationCompletedEventArgs e)
		{
			try
			{
				// Use the result directly from the event args.
				NetworkSession networkSession = e.Result as NetworkSession;
				if (networkSession == null)
					throw new InvalidOperationException("NetworkSession result was null or invalid.");

				// Create a component that will manage the session we just created.
				NetworkSessionComponent.Create(ScreenManager, networkSession);

				// Go to the lobby screen. We pass null as the controlling player,
				// because the lobby screen accepts input from all local players
				// who are in the session, not just a single controlling player.
				ScreenManager.AddScreen(new LobbyScreen(networkSession), null);
			}
			catch (Exception exception)
			{
				NetworkErrorScreen errorScreen = new NetworkErrorScreen(exception);

				ScreenManager.AddScreen(errorScreen, ControllingPlayer);
			}
		}


		/// <summary>
		/// Event handler for when the Find Sessions menu entry is selected.
		/// </summary>
		void FindSessionsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
		{
			try
			{
				// Which local profiles should we include in this session?
				IEnumerable<SignedInGamer> localGamers = NetworkSessionComponent.ChooseGamers(sessionType, ControllingPlayer.Value);

				// Begin an asynchronous find network sessions operation.
				var availableNetworkSessions = NetworkSession.FindAsync(
					sessionType,
					NetworkSessionComponent.MaxLocalGamers, null);

				// Activate the network busy screen, which will display
				// an animation until this operation has completed.
				NetworkBusyScreen<AvailableNetworkSessionCollection> busyScreen = new NetworkBusyScreen<AvailableNetworkSessionCollection>(availableNetworkSessions);

				busyScreen.OperationCompleted += FindSessionsOperationCompleted;

				ScreenManager.AddScreen(busyScreen, ControllingPlayer);
			}
			catch (Exception exception)
			{
				NetworkErrorScreen errorScreen = new NetworkErrorScreen(exception);

				ScreenManager.AddScreen(errorScreen, ControllingPlayer);
			}
		}


		/// <summary>
		/// Event handler for when the asynchronous find network sessions
		/// operation has completed.
		/// </summary>
		void FindSessionsOperationCompleted(object sender, OperationCompletedEventArgs e)
		{
			GameScreen nextScreen;

			try
			{
				// Use the result directly from the event args.
				AvailableNetworkSessionCollection availableSessions = e.Result as AvailableNetworkSessionCollection;
				if (availableSessions == null)
					throw new InvalidOperationException("AvailableNetworkSessionCollection result was null or invalid.");

				if (availableSessions.Count == 0)
				{
					// If we didn't find any sessions, display an error.
					availableSessions.Dispose();

					nextScreen = new MessageBoxScreen(Resources.NoSessionsFound, false);
				}
				else
				{
					// If we did find some sessions, proceed to the JoinSessionScreen.
					nextScreen = new JoinSessionScreen(availableSessions);
				}
			}
			catch (Exception exception)
			{
				nextScreen = new NetworkErrorScreen(exception);
			}

			ScreenManager.AddScreen(nextScreen, ControllingPlayer);
		}
	}
}