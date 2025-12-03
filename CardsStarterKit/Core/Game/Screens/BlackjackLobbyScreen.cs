//----------------------------------------------------------------------------- 
// BlackjackLobbyScreen.cs
//
// Displays the lobby with player slots and host controls.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    class BlackjackLobbyScreen : MenuScreen
    {
        MenuEntry startGameMenuEntry;
        MenuEntry leaveLobbyMenuEntry;
        List<string> joinedPlayers = new List<string>();
        bool isHost;
        NetworkSession networkSession;

        public BlackjackLobbyScreen(NetworkSession networkSession = null)
            : base(Resources.Lobby)
        {
            this.networkSession = networkSession;

            if (networkSession != null)
            {
                this.isHost = networkSession.IsHost;

                // Subscribe to session events
                networkSession.GamerJoined += OnGamerJoined;
                networkSession.GamerLeft += OnGamerLeft;

                // Initialize player list from current session
                UpdatePlayerList();
            }
            else
            {
                // Local game fallback
                this.isHost = true;
                var defaultPlayerName = Environment.UserName;
                if (string.IsNullOrEmpty(defaultPlayerName))
                    defaultPlayerName = "You";
                joinedPlayers.Add(defaultPlayerName);
            }
        }

        public override void LoadContent()
        {
            startGameMenuEntry = new MenuEntry(Resources.StartGame);
            leaveLobbyMenuEntry = new MenuEntry(Resources.LeaveSession);

            startGameMenuEntry.Selected += StartGameMenuEntrySelected;
            leaveLobbyMenuEntry.Selected += LeaveLobbyMenuEntrySelected;

            // Only add "Start Game" button for the host
            if (isHost)
            {
                MenuEntries.Add(startGameMenuEntry);
            }
            MenuEntries.Add(leaveLobbyMenuEntry);

            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            // Draw solid background to cover any BackgroundScreen logo
            ScreenManager.GraphicsDevice.Clear(new Color(50, 20, 20)); // Dark red background

            base.Draw(gameTime);

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;
            Vector2 position = new Vector2(ScreenManager.SafeArea.Left + 50, ScreenManager.SafeArea.Top + 100);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
            spriteBatch.DrawString(font, Resources.Players, position, Color.White);
            position.Y += font.LineSpacing * 2;

            int slotIndex = 0;
            // Show joined players
            foreach (var playerName in joinedPlayers)
            {
                // Strip GUID suffix for display (e.g., "Player_abc123de" -> "Player")
                string displayName = CardsFramework.UIUtility.StripGuidSuffix(playerName);
                string slotText = string.Format(Resources.Slot, slotIndex + 1, displayName);
                if (slotIndex == 0 && isHost)
                    slotText += $"    [{Resources.Host}]";
                spriteBatch.DrawString(font, slotText, position, Color.Green);
                position.Y += font.LineSpacing;
                slotIndex++;
            }
            // Do NOT show AI players until Start Game is clicked
            // Dealer
            position.Y += font.LineSpacing;
            spriteBatch.DrawString(font, Resources.Dealer, position, Color.Yellow);
            spriteBatch.End();
        }

        void StartGameMenuEntrySelected(object sender, EventArgs e)
        {
            if (networkSession != null && isHost)
            {
                // In network game, host calls StartGame which will trigger state change
                // This will cause Update() to detect Playing state and transition all players
                networkSession.StartGame();
            }
            else if (networkSession == null)
            {
                // TODO Display Message that network session is required to start game
            }
            else
            {
                // If client in network game, do nothing - only host can start
            }
        }

        void LeaveLobbyMenuEntrySelected(object sender, EventArgs e)
        {
            // Unsubscribe from events
            if (networkSession != null)
            {
                networkSession.GamerJoined -= OnGamerJoined;
                networkSession.GamerLeft -= OnGamerLeft;
            }

            // Exit all current screens before returning to main menu
            foreach (GameScreen screen in ScreenManager.GetScreens())
                screen.ExitScreen();

            ScreenManager.AddScreen(new BackgroundScreen(), null);
            ScreenManager.AddScreen(new MainMenuScreen(), null);
        }

        void OnGamerJoined(object sender, GamerJoinedEventArgs e)
        {
            UpdatePlayerList();
        }

        void OnGamerLeft(object sender, GamerLeftEventArgs e)
        {
            UpdatePlayerList();
        }

        void UpdatePlayerList()
        {
            joinedPlayers.Clear();

            if (networkSession != null)
            {
                // CRITICAL: Ensure consistent player ordering across all machines
                // Host must ALWAYS be first, then other players sorted by their network ID
                var orderedGamers = networkSession.AllGamers
                    .OrderByDescending(g => g.IsHost)  // Host first
                    .ThenBy(g => g.Id);                 // Then others sorted by ID

                foreach (NetworkGamer gamer in orderedGamers)
                {
                    joinedPlayers.Add(gamer.Gamertag);
                }
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // In network games, check if host started the game
            if (networkSession != null && !IsExiting)
            {
                if (networkSession.SessionState == NetworkSessionState.Playing)
                {
                    // Host started the game, transition all players
                    // Only pass human player names - GameplayScreen will add AI players on host only
                    var allPlayers = new List<string>(joinedPlayers);

                    // Exit all screens to clear the background and lobby screens
                    foreach (GameScreen screen in ScreenManager.GetScreens())
                        screen.ExitScreen();

                    ScreenManager.AddScreen(new GameplayScreen(MainMenuScreen.Theme, allPlayers, networkSession), null);
                }
            }
        }

        // Helper for session info - DEPRECATED, keeping for backward compatibility
        public class SessionInfo
        {
            public List<string> PlayerNames = new List<string>();
        }
    }
}