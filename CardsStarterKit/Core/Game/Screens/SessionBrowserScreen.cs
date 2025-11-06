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
        MenuEntry refreshMenuEntry;
        MenuEntry backMenuEntry;
        List<MenuEntry> sessionEntries = new List<MenuEntry>();
        List<AvailableNetworkSession> availableSessions = new List<AvailableNetworkSession>();
        
        TimeSpan timeSinceLastSearch = TimeSpan.Zero;
        const float AutoRefreshInterval = 5.0f; // Auto-refresh every 5 seconds
        bool isSearching = false;

        public SessionBrowserScreen()
            : base("Join or Host a Blackjack Game")
        {
        }

        public override void LoadContent()
        {
            hostGameMenuEntry = new MenuEntry("Host New Game");
            refreshMenuEntry = new MenuEntry("Refresh");
            backMenuEntry = new MenuEntry("Back");

            hostGameMenuEntry.Selected += HostGameMenuEntrySelected;
            refreshMenuEntry.Selected += RefreshMenuEntrySelected;
            backMenuEntry.Selected += OnCancel;

            MenuEntries.Add(hostGameMenuEntry);
            MenuEntries.Add(refreshMenuEntry);
            
            // Start async session discovery
            BeginFindSessions();
            
            MenuEntries.Add(backMenuEntry);

            base.LoadContent();
        }
        
        void RefreshMenuEntrySelected(object sender, EventArgs e)
        {
            // Manually trigger a refresh
            if (!isSearching)
            {
                BeginFindSessions();
                timeSinceLastSearch = TimeSpan.Zero;
            }
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

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
            
            // Auto-refresh session list periodically
            if (!isSearching && !coveredByOtherScreen)
            {
                timeSinceLastSearch += gameTime.ElapsedGameTime;
                if (timeSinceLastSearch.TotalSeconds >= AutoRefreshInterval)
                {
                    BeginFindSessions();
                    timeSinceLastSearch = TimeSpan.Zero;
                }
            }
            
            // Update refresh button text to show status
            if (refreshMenuEntry != null)
            {
                refreshMenuEntry.Text = isSearching ? "Searching..." : "Refresh";
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
            
            // Show session count and search status
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;
            Vector2 position = new Vector2(ScreenManager.SafeArea.Left + 50, ScreenManager.SafeArea.Bottom - 100);
            
            spriteBatch.Begin();
            
            string statusText = isSearching 
                ? "Searching for games..." 
                : $"Found {availableSessions.Count} game(s)";
            
            spriteBatch.DrawString(font, statusText, position, Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            
            // Show auto-refresh timer
            if (!isSearching)
            {
                int secondsUntilRefresh = (int)(AutoRefreshInterval - timeSinceLastSearch.TotalSeconds);
                string timerText = $"Auto-refresh in {secondsUntilRefresh}s";
                position.Y += font.LineSpacing;
                spriteBatch.DrawString(font, timerText, position, Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
            
            spriteBatch.End();
        }

        // Helper class for session info
        // Deprecated: AvailableSession replaced by AvailableNetworkSession

        void BeginFindSessions()
        {
            if (isSearching)
                return; // Already searching
                
            isSearching = true;
            
            var asyncResult = NetworkSession.FindAsync(
                NetworkSessionType.SystemLink,
                1, // local gamers
                null);
            var busyScreen = new NetworkBusyScreen<AvailableNetworkSessionCollection>(asyncResult);
            busyScreen.OperationCompleted += (s, evt) =>
            {
                isSearching = false;
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