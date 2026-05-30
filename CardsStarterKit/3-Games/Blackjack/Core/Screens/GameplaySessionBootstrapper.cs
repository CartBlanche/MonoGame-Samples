using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    /// <summary>
    /// Isolates gameplay session/player bootstrap so GameplayScreen can focus on orchestration.
    /// </summary>
    internal sealed class GameplaySessionBootstrapper
    {
        private readonly BlackjackCardGame blackJackGame;
        private readonly List<string> joinedPlayers;
        private readonly NetworkSession networkSession;
        private readonly string theme;
        private readonly Action<int> calculatePlayerPositions;
        private readonly EventHandler npcHitHandler;
        private readonly EventHandler npcStandHandler;

        public GameplaySessionBootstrapper(
            BlackjackCardGame blackJackGame,
            List<string> joinedPlayers,
            NetworkSession networkSession,
            string theme,
            Action<int> calculatePlayerPositions,
            EventHandler npcHitHandler,
            EventHandler npcStandHandler)
        {
            this.blackJackGame = blackJackGame ?? throw new ArgumentNullException(nameof(blackJackGame));
            this.joinedPlayers = joinedPlayers;
            this.networkSession = networkSession;
            this.theme = theme;
            this.calculatePlayerPositions = calculatePlayerPositions ?? throw new ArgumentNullException(nameof(calculatePlayerPositions));
            this.npcHitHandler = npcHitHandler;
            this.npcStandHandler = npcStandHandler;
        }

        public void InitializeGame()
        {
            TextInfo myTI = new CultureInfo("en-GB", false).TextInfo;

            Debug.WriteLine($"[InitializeGame] joinedPlayers={(joinedPlayers == null ? "null" : joinedPlayers.Count.ToString())}, networkSession={(networkSession == null ? "null" : "not null")}, IsHost={(networkSession?.IsHost ?? false)}");

            if (joinedPlayers != null && joinedPlayers.Count > 0)
            {
                AddPlayersFromLobby(myTI);
            }
            else
            {
                AddFallbackSinglePlayerWithNpcs(myTI);
            }

            int totalPlayers = blackJackGame.Players.Count;
            calculatePlayerPositions(totalPlayers);
            blackJackGame.GameTable.SetPlaces(totalPlayers);

            string[] assets = { "blackjack", "bust", "lose", "push", "win", "pass", "Shuffle_" + theme };
            for (int chipIndex = 0; chipIndex < assets.Length; chipIndex++)
            {
                blackJackGame.LoadUITexture("UI", assets[chipIndex]);
            }

            if (networkSession != null && networkSession.IsHost)
            {
                blackJackGame.BroadcastPlayerList();
            }

            AssignLocalPlayerIndex();

            if (networkSession == null || networkSession.IsHost)
            {
                blackJackGame.StartRound();
            }
        }

        private void AddPlayersFromLobby(TextInfo myTI)
        {
            if (networkSession != null && !networkSession.IsHost)
            {
                Debug.WriteLine("[InitializeGame] Client waiting for PlayerListSync from host...");
                return;
            }

            int humanPlayerCount = joinedPlayers.Count;
            if (networkSession != null)
            {
                humanPlayerCount = networkSession.AllGamers.Count;
            }

            for (int i = 0; i < humanPlayerCount; i++)
            {
                var player = new BlackjackPlayer(myTI.ToTitleCase(joinedPlayers[i]), blackJackGame);

                if (i == 0 && !blackJackGame.IsNetworkGame && GameSettings.Instance.PersistWinnings)
                {
                    if (GameSettings.Instance.SavedPlayerBalance <= 0)
                    {
                        GameSettings.Instance.SavedPlayerBalance = 500f;
                        GameSettings.Save();
                        Debug.WriteLine("[PersistWinnings] (Path 1) Reset negative/zero balance to default: 500");
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

            if (networkSession == null || networkSession.IsHost)
            {
                int maxNPC = GameSettings.Instance.MaxNPCPlayers;
                int npcSlotsToFill = GameSettings.Instance.FillEmptySlotsWithNPC
                    ? Math.Min(BlackjackConstants.MaxPlayers - humanPlayerCount, maxNPC)
                    : Math.Min(maxNPC, BlackjackConstants.MaxPlayers - humanPlayerCount);

                for (int i = 0; i < npcSlotsToFill && i < BlackjackConstants.DefaultAINames.Length; i++)
                {
                    BlackjackNPCPlayer player = new BlackjackNPCPlayer(BlackjackConstants.DefaultAINames[i], blackJackGame);
                    blackJackGame.AddPlayer(player);
                    player.Hit += npcHitHandler;
                    player.Stand += npcStandHandler;
                }
            }
        }

        private void AddFallbackSinglePlayerWithNpcs(TextInfo myTI)
        {
            var defaultPlayerName = Environment.UserName;
            if (string.IsNullOrEmpty(defaultPlayerName))
            {
                defaultPlayerName = "You";
            }

            var humanPlayer = new BlackjackPlayer(myTI.ToTitleCase(defaultPlayerName), blackJackGame);

            if (GameSettings.Instance.PersistWinnings)
            {
                if (GameSettings.Instance.SavedPlayerBalance <= 0)
                {
                    GameSettings.Instance.SavedPlayerBalance = 500f;
                    GameSettings.Save();
                    Debug.WriteLine("[PersistWinnings] (Path 2 - Fallback) Reset negative/zero balance to default: 500");
                }
                humanPlayer.Balance = GameSettings.Instance.SavedPlayerBalance;
                Debug.WriteLine($"[PersistWinnings] (Path 2 - Fallback) Loaded balance: {humanPlayer.Balance}");
            }
            else
            {
                Debug.WriteLine($"[PersistWinnings] (Path 2 - Fallback) Using default balance: {humanPlayer.Balance}");
            }

            blackJackGame.AddPlayer(humanPlayer);

            int maxNPC = GameSettings.Instance.MaxNPCPlayers;
            for (int i = 0; i < maxNPC && i < BlackjackConstants.DefaultAINames.Length; i++)
            {
                BlackjackNPCPlayer player = new BlackjackNPCPlayer(BlackjackConstants.DefaultAINames[i], blackJackGame);
                blackJackGame.AddPlayer(player);
                player.Hit += npcHitHandler;
                player.Stand += npcStandHandler;
            }
        }

        private void AssignLocalPlayerIndex()
        {
            GameplayLocalPlayerIndexResolver.TryAssign(blackJackGame, networkSession, out _);
        }
    }
}