//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using CardsFramework;
using Microsoft.Xna.Framework.Input.Touch;
using System.Globalization;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    class GameplayScreen : GameScreen
    {
        BlackjackCardGame blackJackGame;

        InputHelper inputHelper;

        string theme;
        List<DrawableGameComponent> pauseEnabledComponents = new List<DrawableGameComponent>();
        List<DrawableGameComponent> pauseVisibleComponents = new List<DrawableGameComponent>();
        Rectangle safeArea;

        Vector2[] playerCardOffset;

        NetworkSession networkSession;

        /// <summary>
        /// Initializes a new instance of the screen.
        /// </summary>
        public GameplayScreen(string theme, List<string> joinedPlayers = null, NetworkSession networkSession = null)
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            EnabledGestures = GestureType.Tap;

            this.theme = theme;
            this.joinedPlayers = joinedPlayers;
            this.networkSession = networkSession;
        }

        private List<string> joinedPlayers;

        /// <summary>
        /// Load content and initializes the actual game.
        /// </summary>
        public override void LoadContent()
        {
            safeArea = ScreenManager.SafeArea;

            // Calculate proportional player positions for 7 players evenly spaced across the table
            // Spread them more evenly from left to right in a gentle arc
            float bottomY = safeArea.Height * 0.30f;  // ~216px - bottom positions
            float topY = safeArea.Height * 0.25f;     // ~180px - top positions (alternating arc)

            playerCardOffset = new Vector2[]
            {
                // Evenly distribute 7 positions across the width from ~10% to ~85%
                new Vector2(safeArea.Width * 0.10f, bottomY),   // Position 0 - far left
                new Vector2(safeArea.Width * 0.225f, topY),     // Position 1 - left-center (higher)
                new Vector2(safeArea.Width * 0.35f, bottomY),   // Position 2 - left-mid
                new Vector2(safeArea.Width * 0.475f, topY),     // Position 3 - center (higher)
                new Vector2(safeArea.Width * 0.60f, bottomY),   // Position 4 - right-mid
                new Vector2(safeArea.Width * 0.725f, topY),     // Position 5 - right-center (higher)
                new Vector2(safeArea.Width * 0.85f, bottomY)    // Position 6 - far right
            };

            // Initialize virtual cursor
            inputHelper = new InputHelper(ScreenManager);
            inputHelper.DrawOrder = 1000;
            ScreenManager.Game.Components.Add(inputHelper);
            // Ignore the curser when not run in Xbox
#if !XBOX
            inputHelper.Visible = false;
            inputHelper.Enabled = false;
#endif

            blackJackGame = new BlackjackCardGame(safeArea, new Vector2(safeArea.Left + safeArea.Width / 2 - 50, safeArea.Top + 20),
                GetPlayerCardPosition, ScreenManager, theme);

            // Wire up network session if in multiplayer mode
            if (networkSession != null)
            {
                blackJackGame.NetworkSession = networkSession;
                blackJackGame.IsNetworkGame = true;
                blackJackGame.IsHost = networkSession.IsHost;
            }

            InitializeGame();

            base.LoadContent();
        }

        /// <summary>
        /// Unload content loaded by the screen.
        /// </summary>
        public override void UnloadContent()
        {
            ScreenManager.Game.Components.Remove(inputHelper);

            base.UnloadContent();
        }

        /// <summary>
        /// Handle user input.
        /// </summary>
        /// <param name="input">User input information.</param>
        public override void HandleInput(InputState input)
        {
            if (input.IsPauseGame(null))
            {
                PauseCurrentGame();
            }

            base.HandleInput(input);
        }

        /// <summary>
        /// Perform the screen's update logic.
        /// </summary>
        /// <param name="gameTime">The time that has passed since the last call to 
        /// this method.</param>
        /// <param name="otherScreenHasFocus">Whether or not another screen has
        /// the focus.</param>
        /// <param name="coveredByOtherScreen">Whether or not another screen covers
        /// this one.</param>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {

            /* TODO: For consoles
            if (Guide.IsVisible)
            {
                PauseCurrentGame();
            } */

            if (blackJackGame != null && !coveredByOtherScreen)
            {
                blackJackGame.Update(gameTime);
            }

            // Centralized network packet dispatcher
            ProcessNetworkPackets();

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        // Centralized network packet dispatcher
        private void ProcessNetworkPackets()
        {
            if (networkSession == null || networkSession.LocalGamers.Count == 0)
                return;

            var localGamer = networkSession.LocalGamers[0];
            var packetReader = new PacketReader();
            while (localGamer.IsDataAvailable)
            {
                NetworkGamer sender;
                localGamer.ReceiveData(packetReader, out sender);
                // Read packet type (assume PacketType is a byte)
                var packetType = (Blackjack.Networking.PacketType)packetReader.ReadByte();
                switch (packetType)
                {
                    case Blackjack.Networking.PacketType.CardDealt:
                        HandleCardDealtPacket(sender, packetReader);
                        break;
                    case Blackjack.Networking.PacketType.BetPlaced:
                        HandleBetPlacedPacket(sender, packetReader);
                        break;
                    case Blackjack.Networking.PacketType.PlayerAction:
                        HandlePlayerActionPacket(sender, packetReader);
                        break;
                    case Blackjack.Networking.PacketType.ShuffleSeed:
                        HandleShuffleSeedPacket(sender, packetReader);
                        break;
                    // Add more cases for other packet types as needed
                    default:
                        // Unknown or unhandled packet type
                        break;
                }
            }
        }

        // Example packet handlers (implement actual logic as needed)
        private void HandleCardDealtPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.CardDealtPacket.Deserialize(reader);
            
            // Only clients should process this - host already dealt the card locally
            if (networkSession != null && !networkSession.IsHost)
            {
                // Forward to the game to handle the card dealing
                blackJackGame.HandleReceivedCardDealt(packet.Card, packet.PlayerIndex, packet.FaceDown, packet.HandType);
            }
        }

        private void HandleBetPlacedPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.BetPlacedPacket.Deserialize(reader);
            
            // Only clients should process this - host already placed the bet locally
            if (networkSession != null && !networkSession.IsHost)
            {
                // Forward to the game to handle the bet
                blackJackGame.HandleReceivedBetPlaced(packet.PlayerIndex, packet.BetAmount);
            }
        }

        private void HandlePlayerActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.PlayerActionPacket.Deserialize(reader);
            // Host receives action from client and processes it
            if (networkSession != null && networkSession.IsHost)
            {
                switch (packet.Action)
                {
                    case Blackjack.Networking.BlackjackAction.Hit:
                        blackJackGame.Hit();
                        break;
                    case Blackjack.Networking.BlackjackAction.Stand:
                        blackJackGame.Stand();
                        break;
                    case Blackjack.Networking.BlackjackAction.Double:
                        blackJackGame.Double();
                        break;
                    case Blackjack.Networking.BlackjackAction.Split:
                        blackJackGame.Split();
                        break;
                }
            }
        }

        private void HandleShuffleSeedPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.ShuffleSeedPacket.Deserialize(reader);
            // Client receives shuffle seed from host
            if (networkSession != null && !networkSession.IsHost)
            {
                blackJackGame.ReceiveShuffleSeed(packet.Seed);
            }
        }

        /// <summary>
        /// Draw the screen
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (blackJackGame != null)
            {
                blackJackGame.Draw(gameTime);
            }

        }

        /// <summary>
        /// Initializes the game component.
        /// </summary>
        private void InitializeGame()
        {
            blackJackGame.Initialize();

            TextInfo myTI = new CultureInfo("en-GB", false).TextInfo;

            // Add players from lobby
            if (joinedPlayers != null && joinedPlayers.Count > 0)
            {
                // Determine how many are human players (from network session)
                int humanPlayerCount = joinedPlayers.Count;
                if (networkSession != null)
                {
                    // In network games, only count actual network gamers as human players
                    humanPlayerCount = networkSession.AllGamers.Count;
                }

                // Add human players
                for (int i = 0; i < humanPlayerCount; i++)
                {
                    blackJackGame.AddPlayer(new BlackjackPlayer(myTI.ToTitleCase(joinedPlayers[i]), blackJackGame));
                }

                // Only the host creates AI players in network games
                // In local games, always create AI players
                if (networkSession == null || networkSession.IsHost)
                {
                    // Fill remaining slots with AI
                    int aiSlotsNeeded = BlackjackConstants.MaxPlayers - humanPlayerCount;
                    for (int i = 0; i < aiSlotsNeeded && i < BlackjackConstants.DefaultAINames.Length; i++)
                    {
                        BlackjackAIPlayer player = new BlackjackAIPlayer(BlackjackConstants.DefaultAINames[i], blackJackGame);
                        blackJackGame.AddPlayer(player);
                        player.Hit += player_Hit;
                        player.Stand += player_Stand;
                    }
                }
            }
            else
            {
                // Fallback: single player + 6 AI (local game only)
                var defaultPlayerName = Environment.UserName;
                if (string.IsNullOrEmpty(defaultPlayerName))
                {
                    defaultPlayerName = "You";
                }

                blackJackGame.AddPlayer(new BlackjackPlayer(myTI.ToTitleCase(defaultPlayerName), blackJackGame));
                for (int i = 0; i < BlackjackConstants.DefaultAINames.Length; i++)
                {
                    BlackjackAIPlayer player = new BlackjackAIPlayer(BlackjackConstants.DefaultAINames[i], blackJackGame);
                    blackJackGame.AddPlayer(player);
                    player.Hit += player_Hit;
                    player.Stand += player_Stand;
                }
            }

            // Load UI assets
            string[] assets = { "blackjack", "bust", "lose", "push", "win", "pass", "Shuffle_" + theme };

            for (int chipIndex = 0; chipIndex < assets.Length; chipIndex++)
            {
                blackJackGame.LoadUITexture("UI", assets[chipIndex]);
            }

            blackJackGame.StartRound();
        }

        /// <summary>
        /// Gets the player hand positions according to the player index.
        /// </summary>
        /// <param name="player">The player's index.</param>
        /// <returns>The position for the player's hand on the game table.</returns>
        private Vector2 GetPlayerCardPosition(int player)
        {
            // Support up to 7 players (indices 0-6)
            if (player >= 0 && player < playerCardOffset.Length)
            {
                return playerCardOffset[player];
            }
            else
            {
                throw new ArgumentException(
                    $"Player index should be between 0 and {playerCardOffset.Length - 1}", "player");
            }
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        private void PauseCurrentGame()
        {
            // Move to the pause screen
            ScreenManager.AddScreen(new BackgroundScreen(), null);
            ScreenManager.AddScreen(new PauseScreen(), null);

            // Hide and disable all components which are related to the gameplay screen
            pauseEnabledComponents.Clear();
            pauseVisibleComponents.Clear();
            foreach (IGameComponent component in ScreenManager.Game.Components)
            {
                if (component is BetGameComponent ||
                    component is AnimatedGameComponent ||
                    component is GameTable ||
                    component is InputHelper)
                {
                    DrawableGameComponent pauseComponent = (DrawableGameComponent)component;
                    if (pauseComponent.Enabled)
                    {
                        pauseEnabledComponents.Add(pauseComponent);
                        pauseComponent.Enabled = false;
                    }
                    if (pauseComponent.Visible)
                    {
                        pauseVisibleComponents.Add(pauseComponent);
                        pauseComponent.Visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Returns from pause.
        /// </summary>
        public void ReturnFromPause()
        {
            // Reveal and enable all previously hidden components
            foreach (DrawableGameComponent component in pauseEnabledComponents)
            {
                component.Enabled = true;
            }
            foreach (DrawableGameComponent component in pauseVisibleComponents)
            {
                component.Visible = true;
            }
        }

        /// <summary>
        /// Responds to the event sent when AI player's choose to "Stand".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_Stand(object sender, EventArgs e)
        {
            blackJackGame.Stand();
        }

        /// <summary>
        /// Responds to the event sent when AI player's choose to "Split".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_Split(object sender, EventArgs e)
        {
            blackJackGame.Split();
        }

        /// <summary>
        /// Responds to the event sent when AI player's choose to "Hit".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_Hit(object sender, EventArgs e)
        {
            blackJackGame.Hit();
        }

        /// <summary>
        /// Responds to the event sent when AI player's choose to "Double".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_Double(object sender, EventArgs e)
        {
            blackJackGame.Double();
        }
    }
}