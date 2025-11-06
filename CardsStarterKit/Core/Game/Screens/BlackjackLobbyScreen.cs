//----------------------------------------------------------------------------- 
// BlackjackLobbyScreen.cs
//
// Displays the lobby with player slots and host controls.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
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
            : base("Lobby")
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
            startGameMenuEntry = new MenuEntry("Start Game");
            leaveLobbyMenuEntry = new MenuEntry("Leave Lobby");

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

            spriteBatch.Begin();
            spriteBatch.DrawString(font, "Players:", position, Color.White);
            position.Y += font.LineSpacing * 2;

            int slotIndex = 0;
            // Show joined players
            foreach (var playerName in joinedPlayers)
            {
                string slotText = $"Slot {slotIndex + 1}: {playerName}";
                if (slotIndex == 0 && isHost)
                    slotText += "    [HOST]";
                spriteBatch.DrawString(font, slotText, position, Color.Green);
                position.Y += font.LineSpacing;
                slotIndex++;
            }
            // Do NOT show AI players until Start Game is clicked
            // Dealer
            position.Y += font.LineSpacing;
            spriteBatch.DrawString(font, "Dealer: House", position, Color.Yellow);
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
                foreach (NetworkGamer gamer in networkSession.AllGamers)
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