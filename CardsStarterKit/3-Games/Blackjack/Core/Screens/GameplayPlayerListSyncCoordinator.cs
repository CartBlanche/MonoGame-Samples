using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    internal sealed class GameplayPlayerListSyncCoordinator
    {
        private readonly BlackjackCardGame blackJackGame;
        private readonly Func<NetworkSession> networkSessionProvider;
        private readonly Action<int> calculatePlayerPositions;

        public GameplayPlayerListSyncCoordinator(
            BlackjackCardGame blackJackGame,
            Func<NetworkSession> networkSessionProvider,
            Action<int> calculatePlayerPositions)
        {
            this.blackJackGame = blackJackGame ?? throw new ArgumentNullException(nameof(blackJackGame));
            this.networkSessionProvider = networkSessionProvider ?? throw new ArgumentNullException(nameof(networkSessionProvider));
            this.calculatePlayerPositions = calculatePlayerPositions ?? throw new ArgumentNullException(nameof(calculatePlayerPositions));
        }

        public void Process(Blackjack.Networking.PlayerListSyncPacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            SyncPlayersFromPacket(packet.Players);

            int totalPlayers = blackJackGame.Players.Count;
            calculatePlayerPositions(totalPlayers);
            blackJackGame.GameTable.SetPlaces(totalPlayers);

            AssignLocalPlayerIndexAfterSync();

            Debug.WriteLine($"[PlayerListSync] Client received {packet.Players.Count} players from host, starting round now");
            blackJackGame.StartRound();
        }

        private void SyncPlayersFromPacket(IReadOnlyList<Blackjack.Networking.PlayerInfo> packetPlayers)
        {
            int currentPlayerCount = blackJackGame.Players.Count;
            int startIndex = currentPlayerCount == 0 ? 0 : currentPlayerCount;

            for (int i = startIndex; i < packetPlayers.Count; i++)
            {
                AddPlayerFromSyncInfo(packetPlayers[i]);
            }
        }

        private void AddPlayerFromSyncInfo(Blackjack.Networking.PlayerInfo playerInfo)
        {
            if (playerInfo.IsNPC)
            {
                BlackjackNPCPlayer npcPlayer = new BlackjackNPCPlayer(playerInfo.Name, blackJackGame);
                blackJackGame.AddPlayer(npcPlayer);
                return;
            }

            TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;
            blackJackGame.AddPlayer(new BlackjackPlayer(textInfo.ToTitleCase(playerInfo.Name), blackJackGame));
        }

        private void AssignLocalPlayerIndexAfterSync()
        {
            NetworkSession networkSession = networkSessionProvider();
            if (networkSession == null || networkSession.LocalGamers.Count <= 0)
                return;

            if (GameplayLocalPlayerIndexResolver.TryAssign(blackJackGame, networkSession, out int assignedIndex))
            {
                Debug.WriteLine($"[PlayerListSync] Client set LocalPlayerIndex to {assignedIndex}");
                return;
            }

            string localGamerTag = networkSession.LocalGamers[0].Gamertag;
            Debug.WriteLine($"[PlayerListSync] WARNING: Could not find local player '{localGamerTag}' in the synced player list!");
            Debug.WriteLine($"[PlayerListSync] Available players: {string.Join(", ", blackJackGame.Players.Select(p => p.Name))}");
        }
    }
}
