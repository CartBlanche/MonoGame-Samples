//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework.Input.Touch;
using System.Globalization;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;
using System.IO;

namespace Blackjack
{
    class GameplayScreen : GameScreen, CardsFramework.Core.ILanguageAware, CardsFramework.Core.IPausable
    {
        BlackjackCardGame blackJackGame;

        string theme;
        List<DrawableGameComponent> pauseEnabledComponents = new List<DrawableGameComponent>();
        List<DrawableGameComponent> pauseVisibleComponents = new List<DrawableGameComponent>();
        Rectangle safeArea;

        Vector2[] playerCardOffset;

        NetworkSession networkSession;

        // Hint system
        private Texture2D gradientTexture;
        private bool showHints = false;
        private int currentHintIndex = -1;
        private TimeSpan timeSinceLastHint;
        private GameSettings settings;
        private HashSet<int> shownHints = new HashSet<int>(); // Track which hints have been shown
        private BlackjackGameState lastGameState = BlackjackGameState.Betting;

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

            // Player positions will be calculated dynamically after we know how many players there are
            blackJackGame = new BlackjackCardGame(safeArea, new Vector2(safeArea.Left + safeArea.Width / 2 - 50, safeArea.Top + 20),
                GetPlayerCardPosition, ScreenManager, theme);

            // Wire up network session if in multiplayer mode
            // Only treat as network game if there are actually multiple human players
            if (networkSession != null && networkSession.AllGamers.Count > 1)
            {
                blackJackGame.NetworkSession = networkSession;
                blackJackGame.IsNetworkGame = true;
                blackJackGame.IsHost = networkSession.IsHost;
                Debug.WriteLine($"[LoadContent] Network game detected with {networkSession.AllGamers.Count} gamers, IsNetworkGame=true");
            }
            else
            {
                Debug.WriteLine($"[LoadContent] Single-player game, IsNetworkGame={blackJackGame.IsNetworkGame}, networkSession={(networkSession == null ? "null" : $"exists with {networkSession.AllGamers.Count} gamers")}");
            }

            InitializeGame();

            // Update button text/fonts to match current language
            UpdateButtonText();

            // Load gradient texture for hint boxes
            gradientTexture = ScreenManager.Game.Content.Load<Texture2D>(Path.Combine("Images", "UI", "gradient"));

            // Initialize hint system
            settings = GameSettings.Instance;
            showHints = settings.ShowHints;

            // Start gameplay background ambience
            AudioManager.PlayPlaylist(volumeMultiplier: 0.10f);

            base.LoadContent();
        }

        /// <summary>
        /// Unload content loaded by the screen.
        /// </summary>
        public override void UnloadContent()
        {
            // Stop the background music when exiting gameplay
            AudioManager.StopMusic();

            // Remove all gameplay components (buttons, cards, chips, etc.) so they
            // don't remain visible or interactive on whatever screen comes next.
            blackJackGame?.RemoveAllGameplayComponents();

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
            if (Guide.IsVisible)
            {
                PauseCurrentGame();
            }

            if (blackJackGame != null && !coveredByOtherScreen)
            {
                blackJackGame.Update(gameTime);
            }

            // Update hint system
            if (showHints && !coveredByOtherScreen)
            {
                UpdateHints(gameTime);
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

                try
                {
                    // Read packet type (assume PacketType is a byte)
                    var packetType = (Blackjack.Networking.PacketType)packetReader.ReadByte();
                    Debug.WriteLine($"[PACKET] Received {packetType} from {sender.Gamertag}");

                    switch (packetType)
                    {
                        case Blackjack.Networking.PacketType.PlayerListSync:
                            HandlePlayerListSyncPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.CardDealt:
                            HandleCardDealtPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.BetPlaced:
                            HandleBetPlacedPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.ChipAdded:
                            HandleChipAddedPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.PlayerAction:
                            HandlePlayerActionPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.ShuffleSeed:
                            HandleShuffleSeedPacket(sender, packetReader);
                            break;
                        // Phase 5: Gameplay action packets
                        case Blackjack.Networking.PacketType.HitAction:
                            HandleHitActionPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.StandAction:
                            HandleStandActionPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.DoubleAction:
                            HandleDoubleActionPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.SplitAction:
                            HandleSplitActionPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.InsuranceAction:
                            HandleInsuranceActionPacket(sender, packetReader);
                            break;
                        case Blackjack.Networking.PacketType.TurnChanged:
                            HandleTurnChangedPacket(sender, packetReader);
                            break;
                        // Add more cases for other packet types as needed
                        default:
                            Debug.WriteLine($"[PACKET] Unknown packet type: {(byte)packetType}");
                            break;
                    }
                }
                catch (System.IO.EndOfStreamException ex)
                {
                    Debug.WriteLine($"[PACKET ERROR] EndOfStreamException while processing packet from {sender.Gamertag}: {ex.Message}");
                    Debug.WriteLine($"[PACKET ERROR] Stack trace: {ex.StackTrace}");
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"[PACKET ERROR] Exception while processing packet from {sender.Gamertag}: {ex.GetType().Name} - {ex.Message}");
                }
            }
        }

        // Packet handlers
        private void HandlePlayerListSyncPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.PlayerListSyncPacket.Deserialize(reader);

            // Only clients should process this
            if (networkSession != null && !networkSession.IsHost)
            {
                System.Globalization.TextInfo myTI = new System.Globalization.CultureInfo("en-GB", false).TextInfo;

                // If client hasn't created any players yet (because we're waiting for this packet)
                // OR if we need to rebuild the player list to match the host's order exactly
                int currentPlayerCount = blackJackGame.Players.Count;
                
                if (currentPlayerCount == 0)
                {
                    // Client has no players yet - build the complete list from the packet
                    for (int i = 0; i < packet.Players.Count; i++)
                    {
                        var playerInfo = packet.Players[i];
                        if (playerInfo.IsNPC)
                        {
                            // Add NPC player
                            BlackjackNPCPlayer NPCPlayer = new BlackjackNPCPlayer(playerInfo.Name, blackJackGame);
                            blackJackGame.AddPlayer(NPCPlayer);
                        }
                        else
                        {
                            // Add human player
                            blackJackGame.AddPlayer(new BlackjackPlayer(myTI.ToTitleCase(playerInfo.Name), blackJackGame));
                        }
                    }
                }
                else
                {
                    // Client already has some players - just add any missing NPC Players
                    for (int i = currentPlayerCount; i < packet.Players.Count; i++)
                    {
                        var playerInfo = packet.Players[i];
                        if (playerInfo.IsNPC)
                        {
                            // Add NPC player (but don't wire up events - host controls NPC)
                            BlackjackNPCPlayer NPCPlayer = new BlackjackNPCPlayer(playerInfo.Name, blackJackGame);
                            blackJackGame.AddPlayer(NPCPlayer);
                        }
                        else
                        {
                            // Add human player (shouldn't normally happen, but handle it)
                            blackJackGame.AddPlayer(new BlackjackPlayer(myTI.ToTitleCase(playerInfo.Name), blackJackGame));
                        }
                    }
                }

                // CRITICAL: Recalculate player positions now that we have the full player list
                int totalPlayers = blackJackGame.Players.Count;
                CalculatePlayerPositions(totalPlayers);

                // Update the table to show the correct number of player spots
                blackJackGame.GameTable.SetPlaces(totalPlayers);

                // CRITICAL: Calculate LocalPlayerIndex based on the final, authoritative player list from host
                // This must match the host's player indices exactly
                if (networkSession.LocalGamers.Count > 0)
                {
                    string localGamerTag = networkSession.LocalGamers[0].Gamertag;
                    bool foundLocalPlayer = false;
                    
                    for (int i = 0; i < blackJackGame.Players.Count; i++)
                    {
                        if (blackJackGame.Players[i].Name.Equals(localGamerTag, StringComparison.OrdinalIgnoreCase))
                        {
                            // Update LocalPlayerIndex in both components
                            var betComponent = blackJackGame.Game.Components.OfType<BetGameComponent>().FirstOrDefault();
                            if (betComponent != null)
                            {
                                betComponent.LocalPlayerIndex = i;
                            }
                            blackJackGame.LocalPlayerIndex = i;
                            Debug.WriteLine($"[PlayerListSync] Client set LocalPlayerIndex to {i} (player: {localGamerTag})");
                            foundLocalPlayer = true;
                            break;
                        }
                    }
                    
                    if (!foundLocalPlayer)
                    {
                        Debug.WriteLine($"[PlayerListSync] WARNING: Could not find local player '{localGamerTag}' in the synced player list!");
                        Debug.WriteLine($"[PlayerListSync] Available players: {string.Join(", ", blackJackGame.Players.Select(p => p.Name))}");
                    }
                }

                // Now that we have the complete player list, start the round
                // This ensures DisplayPlayingHands() creates animatedHands for all players including NPCs
                Debug.WriteLine($"[PlayerListSync] Client received {packet.Players.Count} players from host, starting round now");
                blackJackGame.StartRound();
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

            if (networkSession != null)
            {
                if (networkSession.IsHost)
                {
                    // Host receives bet from a client
                    // Don't process if this is from the local host machine (to avoid processing our own broadcast)
                    if (!sender.IsLocal)
                    {
                        // Apply the bet locally on the host
                        blackJackGame.HandleReceivedBetPlaced(packet.PlayerIndex, packet.BetAmount);

                        // Broadcast to all other clients (so all clients stay in sync)
                        blackJackGame.BroadcastBetPlaced(packet.PlayerIndex, packet.BetAmount);
                    }
                }
                else
                {
                    // Client receives bet broadcast from host
                    blackJackGame.HandleReceivedBetPlaced(packet.PlayerIndex, packet.BetAmount);
                }
            }
        }

        private void HandleChipAddedPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.ChipAddedPacket.Deserialize(reader);

            if (networkSession != null)
            {
                if (networkSession.IsHost)
                {
                    // Host receives chip addition from a client
                    // Don't process if this is from the local host machine (to avoid processing our own broadcast)
                    if (!sender.IsLocal)
                    {
                        // Apply the chip locally on the host (this will trigger the animation and update bet)
                        blackJackGame.HandleReceivedChipAdded(packet.PlayerIndex, packet.ChipValue);

                        // Broadcast to all other clients (so all clients stay in sync)
                        blackJackGame.BroadcastChipAdded(packet.PlayerIndex, packet.ChipValue);
                    }
                }
                else
                {
                    // Client receives chip addition broadcast from host
                    blackJackGame.HandleReceivedChipAdded(packet.PlayerIndex, packet.ChipValue);
                }
            }
        }

        private void HandlePlayerActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.PlayerActionPacket.Deserialize(reader);
            // Host receives action from client and processes it
            if (networkSession != null && networkSession.IsHost)
            {
                // Execute the action for the specific player
                switch (packet.Action)
                {
                    case Blackjack.Networking.BlackjackAction.Hit:
                        blackJackGame.HitForPlayer(packet.PlayerIndex);
                        break;
                    case Blackjack.Networking.BlackjackAction.Stand:
                        blackJackGame.StandForPlayer(packet.PlayerIndex);
                        break;
                    case Blackjack.Networking.BlackjackAction.Double:
                        blackJackGame.DoubleForPlayer(packet.PlayerIndex);
                        break;
                    case Blackjack.Networking.BlackjackAction.Split:
                        blackJackGame.SplitForPlayer(packet.PlayerIndex);
                        break;
                    case Blackjack.Networking.BlackjackAction.Insurance:
                        blackJackGame.InsuranceForPlayer(packet.PlayerIndex);
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

            // Draw hints on top of game
            if (showHints && currentHintIndex >= 0)
            {
                var spriteBatch = ScreenManager.SpriteBatch;
                spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
                DrawHints(spriteBatch);
                spriteBatch.End();
            }

        }

        /// <summary>
        /// Initializes the game component.
        /// </summary>
        private void InitializeGame()
        {
            blackJackGame.Initialize();

            blackJackGame.UpdateButtonText();

            TextInfo myTI = new CultureInfo("en-GB", false).TextInfo;

            Debug.WriteLine($"[InitializeGame] joinedPlayers={(joinedPlayers == null ? "null" : joinedPlayers.Count.ToString())}, networkSession={(networkSession == null ? "null" : "not null")}, IsHost={(networkSession?.IsHost ?? false)}");

            // Add players from lobby
            if (joinedPlayers != null && joinedPlayers.Count > 0)
            {
                // NETWORK GAME SYNCHRONIZATION:
                // Host creates full player list immediately and broadcasts it
                // Clients wait to receive the host's player list for consistency
                if (networkSession != null && !networkSession.IsHost)
                {
                    // Client: Don't create players yet, wait for PlayerListSync from host
                    // The host will send the player list soon
                    Debug.WriteLine("[InitializeGame] Client waiting for PlayerListSync from host...");
                    // Don't add any players yet
                }
                else
                {
                    // Host or local game: Create player list now
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
                        var player = new BlackjackPlayer(myTI.ToTitleCase(joinedPlayers[i]), blackJackGame);

                        // Load saved balance if persistent winnings is enabled (single-player only, first player)
                        if (i == 0 && !blackJackGame.IsNetworkGame && GameSettings.Instance.PersistWinnings)
                        {
                            // Reset to default if saved balance is 0 or negative
                            if (GameSettings.Instance.SavedPlayerBalance <= 0)
                            {
                                GameSettings.Instance.SavedPlayerBalance = 500f;
                                GameSettings.Save();
                                Debug.WriteLine($"[PersistWinnings] (Path 1) Reset negative/zero balance to default: 500");
                            }
                            player.Balance = GameSettings.Instance.SavedPlayerBalance;
                            Debug.WriteLine($"[PersistWinnings] (Path 1) Loaded balance: {player.Balance}");
                        }
                        else
                        {
                            Debug.WriteLine($"[PersistWinnings] (Path 1) Using default balance: {player.Balance} (i={i}, IsNetworkGame={blackJackGame.IsNetworkGame}, PersistWinnings={GameSettings.Instance.PersistWinnings})");
                        }

                        blackJackGame.AddPlayer(player);
                    }

                    // Only the host creates NPC Players in network games
                    // In local games, always create NPC Players
                    if (networkSession == null || networkSession.IsHost)
                    {
                        // Fill remaining slots with NPC based on settings
                        int maxNPC = GameSettings.Instance.MaxNPCPlayers;
                        int npcSlotsToFill = GameSettings.Instance.FillEmptySlotsWithNPC
                            ? Math.Min(BlackjackConstants.MaxPlayers - humanPlayerCount, maxNPC)
                            : Math.Min(maxNPC, BlackjackConstants.MaxPlayers - humanPlayerCount);

                        for (int i = 0; i < npcSlotsToFill && i < BlackjackConstants.DefaultAINames.Length; i++)
                        {
                            BlackjackNPCPlayer player = new BlackjackNPCPlayer(BlackjackConstants.DefaultAINames[i], blackJackGame);
                            blackJackGame.AddPlayer(player);
                            player.Hit += player_Hit;
                            player.Stand += player_Stand;
                        }
                    }
                }
            }
            else
            {
                // Fallback: single player + 6 NPC (local game only)
                var defaultPlayerName = Environment.UserName;
                if (string.IsNullOrEmpty(defaultPlayerName))
                {
                    defaultPlayerName = "You";
                }

                var humanPlayer = new BlackjackPlayer(myTI.ToTitleCase(defaultPlayerName), blackJackGame);

                // Load saved balance if persistent winnings is enabled (single-player only)
                if (GameSettings.Instance.PersistWinnings)
                {
                    // Reset to default if saved balance is 0 or negative
                    if (GameSettings.Instance.SavedPlayerBalance <= 0)
                    {
                        GameSettings.Instance.SavedPlayerBalance = 500f;
                        GameSettings.Save();
                        Debug.WriteLine($"[PersistWinnings] (Path 2 - Fallback) Reset negative/zero balance to default: 500");
                    }
                    humanPlayer.Balance = GameSettings.Instance.SavedPlayerBalance;
                    Debug.WriteLine($"[PersistWinnings] (Path 2 - Fallback) Loaded balance: {humanPlayer.Balance}");
                }
                else
                {
                    Debug.WriteLine($"[PersistWinnings] (Path 2 - Fallback) Using default balance: {humanPlayer.Balance}");
                }

                blackJackGame.AddPlayer(humanPlayer);

                // Add NPC Players based on settings
                int maxNPC = GameSettings.Instance.MaxNPCPlayers;
                for (int i = 0; i < maxNPC && i < BlackjackConstants.DefaultAINames.Length; i++)
                {
                    BlackjackNPCPlayer player = new BlackjackNPCPlayer(BlackjackConstants.DefaultAINames[i], blackJackGame);
                    blackJackGame.AddPlayer(player);
                    player.Hit += player_Hit;
                    player.Stand += player_Stand;
                }
            }

            // Calculate player positions now that we know the actual number of players
            int totalPlayers = blackJackGame.Players.Count;
            CalculatePlayerPositions(totalPlayers);

            // Update the table to show only the actual number of player spots
            blackJackGame.GameTable.SetPlaces(totalPlayers);

            // Load UI assets
            string[] assets = { "blackjack", "bust", "lose", "push", "win", "pass", "Shuffle_" + theme };

            for (int chipIndex = 0; chipIndex < assets.Length; chipIndex++)
            {
                blackJackGame.LoadUITexture("UI", assets[chipIndex]);
            }

            // Host broadcasts the full player list to clients so they know about NPC Players
            if (networkSession != null && networkSession.IsHost)
            {
                blackJackGame.BroadcastPlayerList();
            }

            // In network games, determine which player index belongs to the local user
            if (networkSession != null && networkSession.LocalGamers.Count > 0)
            {
                string localGamerTag = networkSession.LocalGamers[0].Gamertag;

                // Find which player in the game matches the local gamer's tag
                for (int i = 0; i < blackJackGame.Players.Count; i++)
                {
                    if (blackJackGame.Players[i].Name.Equals(localGamerTag, StringComparison.OrdinalIgnoreCase))
                    {
                        // Found the local player - tell BetGameComponent and BlackjackCardGame
                        var betComponent = blackJackGame.Game.Components.OfType<BetGameComponent>().FirstOrDefault();
                        if (betComponent != null)
                        {
                            betComponent.LocalPlayerIndex = i;
                        }
                        blackJackGame.LocalPlayerIndex = i;
                        break;
                    }
                }
            }

            // Only start the round immediately if we're the host or in a local game
            // Clients need to wait for the PlayerListSync packet first
            if (networkSession == null || networkSession.IsHost)
            {
                blackJackGame.StartRound();
            }
            // Note: Clients will call StartRound() after receiving PlayerListSync packet
        }

        /// <summary>
        /// Calculates player positions dynamically based on the actual number of players.
        /// Centers and spreads them evenly across the table.
        /// </summary>
        /// <param name="playerCount">The actual number of players in the game.</param>
        private void CalculatePlayerPositions(int playerCount)
        {
            if (playerCount <= 0)
            {
                playerCardOffset = new Vector2[0];
                return;
            }

            playerCardOffset = new Vector2[playerCount];

            float bottomY = safeArea.Height * 0.41f;  // ~302px - bottom positions (moved down more)
            float topY = safeArea.Height * 0.36f;     // ~266px - top positions (alternating arc, moved down more)

            // Calculate spacing based on number of players
            // More players = tighter spacing, fewer players = more spread out
            float leftMargin = safeArea.Width * 0.10f;
            float rightMargin = safeArea.Width * 0.15f;
            float usableWidth = safeArea.Width - leftMargin - rightMargin;

            // Distribute players evenly across the usable width
            for (int i = 0; i < playerCount; i++)
            {
                float xPosition;
                if (playerCount == 1)
                {
                    // Single player: center
                    xPosition = safeArea.Width * 0.5f;
                }
                else
                {
                    // Multiple players: spread evenly
                    float spacing = usableWidth / (playerCount - 1);
                    xPosition = leftMargin + (i * spacing);
                }

                // Alternate between bottom and top Y positions for visual variety
                float yPosition = (i % 2 == 0) ? bottomY : topY;

                playerCardOffset[i] = new Vector2(xPosition, yPosition);
            }
        }

        /// <summary>
        /// Gets the player hand positions according to the player index.
        /// </summary>
        /// <param name="player">The player's index.</param>
        /// <returns>The position for the player's hand on the game table.</returns>
        private Vector2 GetPlayerCardPosition(int player)
        {
            // Support dynamic number of players
            if (playerCardOffset != null && player >= 0 && player < playerCardOffset.Length)
            {
                return playerCardOffset[player];
            }
            else
            {
                // Fallback to center if positions haven't been calculated yet
                return new Vector2(safeArea.Width * 0.5f, safeArea.Height * 0.30f);
            }
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public void PauseCurrentGame()
        {
            // Pause the background music
            AudioManager.PauseMusic();

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
                    component is GameTable)
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
            // Resume the background music
            AudioManager.ResumeMusic();

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
        /// Responds to the event sent when NPC player's choose to "Stand".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_Stand(object sender, EventArgs e)
        {
            blackJackGame.Stand();
        }

        /// <summary>
        /// Responds to the event sent when NPC player's choose to "Split".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_Split(object sender, EventArgs e)
        {
            blackJackGame.Split();
        }

        /// <summary>
        /// Responds to the event sent when NPC player's choose to "Hit".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_Hit(object sender, EventArgs e)
        {
            blackJackGame.Hit();
        }

        /// <summary>
        /// Responds to the event sent when NPC player's choose to "Double".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void player_Double(object sender, EventArgs e)
        {
            blackJackGame.Double();
        }

        // Phase 5: Gameplay Action Packet Handlers
        private void HandleHitActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.HitActionPacket.Deserialize(reader);

            // Only clients should process this - host already executed the action locally
            if (networkSession != null && !networkSession.IsHost)
            {
                blackJackGame.HandleReceivedHitAction(packet.PlayerIndex);
            }
        }

        private void HandleStandActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.StandActionPacket.Deserialize(reader);

            // Only clients should process this - host already executed the action locally
            if (networkSession != null && !networkSession.IsHost)
            {
                blackJackGame.HandleReceivedStandAction(packet.PlayerIndex);
            }
        }

        private void HandleDoubleActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.DoubleActionPacket.Deserialize(reader);

            // Only clients should process this - host already executed the action locally
            if (networkSession != null && !networkSession.IsHost)
            {
                blackJackGame.HandleReceivedDoubleAction(packet.PlayerIndex);
            }
        }

        private void HandleSplitActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.SplitActionPacket.Deserialize(reader);

            // Only clients should process this - host already executed the action locally
            if (networkSession != null && !networkSession.IsHost)
            {
                blackJackGame.HandleReceivedSplitAction(packet.PlayerIndex);
            }
        }

        private void HandleInsuranceActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.InsuranceActionPacket.Deserialize(reader);

            // Only clients should process this - host already executed the action locally
            if (networkSession != null && !networkSession.IsHost)
            {
                blackJackGame.HandleReceivedInsuranceAction(packet.PlayerIndex);
            }
        }

        private void HandleTurnChangedPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.TurnChangedPacket.Deserialize(reader);
            Debug.WriteLine($"[PACKET] Turn changed from {sender.Gamertag}, current player index: {packet.CurrentPlayerIndex}");

            blackJackGame.HandleReceivedTurnChanged(packet.CurrentPlayerIndex);
        }

        public void OnLanguageChanged() => UpdateButtonText();

        /// <summary>
        /// Updates button text after language change
        /// </summary>
        public void UpdateButtonText()
        {
            if (blackJackGame != null)
            {
                blackJackGame.UpdateButtonText();
                var betComponent = blackJackGame.Game.Components.OfType<BetGameComponent>().FirstOrDefault();
                betComponent?.UpdateButtonText();
            }
        }

        /// <summary>
        /// Updates the hint system, showing context-aware hints based on game state
        /// </summary>
        private void UpdateHints(GameTime gameTime)
        {
            if (blackJackGame == null) return;

            var currentState = blackJackGame.State;
            var betComponent = blackJackGame.Game.Components.OfType<BetGameComponent>().FirstOrDefault();
            if (betComponent == null) return;

            // Check if player has made a bet (first player is typically the human player)
            var player = blackJackGame.Players.FirstOrDefault() as BlackjackPlayer;
            bool hasBet = player?.BetAmount > 0;

            // Determine which hint to show based on context
            int desiredHint = -1;

            if (currentState == BlackjackGameState.Betting)
            {
                if (!hasBet && !shownHints.Contains(0))
                {
                    // Hint 0: Place a bet
                    desiredHint = 0;
                }
                else if (hasBet && !shownHints.Contains(1))
                {
                    // Hint 1: Remove bet
                    desiredHint = 1;
                }
            }
            else if ((currentState == BlackjackGameState.Playing || currentState == BlackjackGameState.Dealing)
                     && !shownHints.Contains(2))
            {
                // Hint 2: Goal of blackjack
                desiredHint = 2;
            }

            // If we have a new hint to show and enough time has passed (or it's immediate state change)
            bool stateChanged = currentState != lastGameState;
            bool enoughTimePassed = currentHintIndex == -1 ||
                                   gameTime.TotalGameTime - timeSinceLastHint > TimeSpan.FromSeconds(5);

            if (desiredHint != -1 && desiredHint != currentHintIndex)
            {
                // Show new hint immediately on state change or after enough time has passed
                if (stateChanged || enoughTimePassed)
                {
                    currentHintIndex = desiredHint;
                    timeSinceLastHint = gameTime.TotalGameTime;
                    shownHints.Add(desiredHint);
                }
            }
            // Special case: if hint 2 needs to show and we're in Playing/Dealing state, show it immediately
            else if (desiredHint == 2 && currentHintIndex != 2 &&
                    (currentState == BlackjackGameState.Playing || currentState == BlackjackGameState.Dealing))
            {
                currentHintIndex = desiredHint;
                timeSinceLastHint = gameTime.TotalGameTime;
                shownHints.Add(desiredHint);
            }
            // Hide hint after 5 seconds only if no new hint is waiting to show
            else if (currentHintIndex != -1 && desiredHint == -1 &&
                    gameTime.TotalGameTime - timeSinceLastHint > TimeSpan.FromSeconds(5))
            {
                currentHintIndex = -1;

                // If all hints have been shown, disable hints permanently for this session
                // Only check this AFTER hiding the current hint
                if (shownHints.Count >= 3)
                {
                    showHints = false;
                }
            }

            lastGameState = currentState;
        }

        /// <summary>
        /// Draws hint boxes with text overlays, positioned near relevant UI elements
        /// </summary>
        private void DrawHints(SpriteBatch spriteBatch)
        {
            if (currentHintIndex < 0) return;

            SpriteFont font = ScreenManager.RegularFont;

            // Padding for hint boxes
            const int hPad = 32;
            const int vPad = 16;

            Rectangle backgroundRectangle = Rectangle.Empty;
            string message = string.Empty;

            // Get bet component to position hints near betting UI
            var betComponent = blackJackGame?.Game.Components.OfType<BetGameComponent>().FirstOrDefault();
            if (betComponent == null) return;

            switch (currentHintIndex)
            {
                case 0:
                    // Hint: Place a bet and hit Deal to join game
                    // Position to the right of Deal/Clear buttons and chips
                    message = Resources.HintPlaceBet;
                    Vector2 textSize0 = font.MeasureString(message);
                    Rectangle dealBounds = betComponent.DealButtonBounds;

                    // Position hint to the right of the buttons with some spacing
                    backgroundRectangle = new Rectangle(
                        dealBounds.Right + 120,
                        dealBounds.Y - 20,
                        (int)(textSize0.X + hPad * 1.5),
                        (int)(textSize0.Y + vPad * 1.5));
                    break;

                case 1:
                    // Hint: Click Clear or chip stack to remove bets
                    // Position above the chip stack
                    message = Resources.HintRemoveBet;
                    Vector2 textSize1 = font.MeasureString(message);
                    Vector2 chipStackPos = betComponent.GetHumanPlayerChipStackPosition();

                    // Position hint above the chip stack (avoid overlapping with chip text)
                    backgroundRectangle = new Rectangle(
                        (int)chipStackPos.X - (int)(textSize1.X / 2) - hPad + 180,
                        (int)chipStackPos.Y - (int)textSize1.Y - vPad * 2 - 80, // Above the stack
                        (int)(textSize1.X + hPad * 1.5),
                        (int)(textSize1.Y + vPad * 1.5));
                    break;

                case 2:
                    // Hint: Goal of blackjack
                    // Position in center-top area
                    message = Resources.HintGameGoal;
                    Vector2 textSize2 = font.MeasureString(message);
                    backgroundRectangle = new Rectangle(
                        safeArea.Center.X - (int)(textSize2.X / 2) - hPad,
                        safeArea.Top + 140,
                        (int)(textSize2.X + hPad * 1.5),
                        (int)(textSize2.Y + vPad * 1.5));
                    break;
            }

            Vector2 textPosition = new Vector2(backgroundRectangle.X + hPad, backgroundRectangle.Y + vPad - 7);

            // Draw the background rectangle with transparency
            spriteBatch.Draw(gradientTexture, backgroundRectangle, Color.Black * 0.7f);

            // Draw green border (3 pixels thick)
            int borderThickness = 3;
            Color borderColor = Color.LimeGreen;

            // Top border
            spriteBatch.Draw(gradientTexture, new Rectangle(backgroundRectangle.X, backgroundRectangle.Y, backgroundRectangle.Width, borderThickness), borderColor);
            // Bottom border
            spriteBatch.Draw(gradientTexture, new Rectangle(backgroundRectangle.X, backgroundRectangle.Bottom - borderThickness, backgroundRectangle.Width, borderThickness), borderColor);
            // Left border
            spriteBatch.Draw(gradientTexture, new Rectangle(backgroundRectangle.X, backgroundRectangle.Y, borderThickness, backgroundRectangle.Height), borderColor);
            // Right border
            spriteBatch.Draw(gradientTexture, new Rectangle(backgroundRectangle.Right - borderThickness, backgroundRectangle.Y, borderThickness, backgroundRectangle.Height), borderColor);

            // Draw the hint text
            spriteBatch.DrawString(font, message, textPosition, Color.White);
        }
    }
}