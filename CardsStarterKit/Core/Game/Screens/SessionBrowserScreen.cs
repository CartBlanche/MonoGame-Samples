//----------------------------------------------------------------------------- 
// SessionBrowserScreen.cs
//
// Shows available sessions and allows hosting a new game.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Graphics;
using GameStateManagement;

namespace Blackjack
{
    class SessionBrowserScreen : MenuScreen
    {
        MenuEntry hostGameMenuEntry;
        MenuEntry backMenuEntry;
        List<MenuEntry> sessionEntries = new List<MenuEntry>();
        List<AvailableNetworkSession> availableSessions = new List<AvailableNetworkSession>();

        public SessionBrowserScreen()
            : base("Join or Host a Blackjack Game")
        {
        }

        public override void LoadContent()
        {
            hostGameMenuEntry = new MenuEntry("Host New Game");
            backMenuEntry = new MenuEntry("Back");

            hostGameMenuEntry.Selected += HostGameMenuEntrySelected;
            backMenuEntry.Selected += OnCancel;

            MenuEntries.Add(hostGameMenuEntry);
            // Start async session discovery
            BeginFindSessions();
            MenuEntries.Add(backMenuEntry);

            base.LoadContent();
        }

        void HostGameMenuEntrySelected(object sender, EventArgs e)
        {
            // Host a new session and go to lobby
            // Use SystemLink for local network testing (PlayerMatch requires online services)
            var asyncResult = NetworkSession.CreateAsync(
                NetworkSessionType.SystemLink,
                BlackjackConstants.MinPlayers, // local gamers
                BlackjackConstants.MaxPlayers, // max gamers
                0, // private slots
                null);
            var busyScreen = new NetworkBusyScreen<NetworkSession>(asyncResult);
            busyScreen.OperationCompleted += (s, evt) =>
            {
                var networkSession = evt.Result as NetworkSession;
                if (networkSession != null)
                {
                    NetworkSessionComponent.Create(ScreenManager, networkSession);
                    ScreenManager.AddScreen(new BlackjackLobbyScreen(networkSession), null);
                }
                else
                {
                    ScreenManager.AddScreen(new MessageBoxScreen("Failed to create session."), null);
                }
            };
            ScreenManager.AddScreen(busyScreen, null);
        }

        void JoinSessionMenuEntrySelected(object sender, EventArgs e)
        {
            var entry = sender as AvailableSessionMenuEntry;
            var availableSession = entry?.AvailableSession;
            if (availableSession != null)
            {
                var asyncResult = NetworkSession.JoinAsync(availableSession);
                var busyScreen = new NetworkBusyScreen<NetworkSession>(asyncResult);
                busyScreen.OperationCompleted += (s, evt) =>
                {
                    var networkSession = evt.Result as NetworkSession;
                    if (networkSession != null)
                    {
                        NetworkSessionComponent.Create(ScreenManager, networkSession);
                        ScreenManager.AddScreen(new BlackjackLobbyScreen(networkSession), null);
                    }
                    else
                    {
                        ScreenManager.AddScreen(new MessageBoxScreen("Failed to join session."), null);
                    }
                };
                ScreenManager.AddScreen(busyScreen, null);
            }
        }

        void RefreshSessionList()
        {
            sessionEntries.Clear();
            foreach (var session in availableSessions)
            {
                var entry = new AvailableSessionMenuEntry(session);
                entry.Selected += JoinSessionMenuEntrySelected;
                sessionEntries.Add(entry);
                // Insert after hostGameMenuEntry, before backMenuEntry
                MenuEntries.Insert(MenuEntries.Count - 1, entry);
            }
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.AddScreen(new BackgroundScreen(), null);
            ScreenManager.AddScreen(new MainMenuScreen(), null);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        // Helper class for session info
        // Deprecated: AvailableSession replaced by AvailableNetworkSession

        void BeginFindSessions()
        {
            var asyncResult = NetworkSession.FindAsync(
                NetworkSessionType.SystemLink,
                1, // local gamers
                null);
            var busyScreen = new NetworkBusyScreen<AvailableNetworkSessionCollection>(asyncResult);
            busyScreen.OperationCompleted += (s, evt) =>
            {
                var foundSessions = evt.Result as AvailableNetworkSessionCollection;
                availableSessions.Clear();
                if (foundSessions != null)
                {
                    foreach (var session in foundSessions)
                        availableSessions.Add(session);
                }
                RefreshSessionList();
            };
            ScreenManager.AddScreen(busyScreen, null);
        }
    }
}