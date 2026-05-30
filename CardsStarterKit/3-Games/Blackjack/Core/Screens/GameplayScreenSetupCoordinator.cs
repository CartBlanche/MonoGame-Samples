using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using CardsFramework;
using CardsFramework.Core;
using System.IO;

namespace Blackjack
{
    internal sealed class GameplayScreenSetupResult
    {
        public BlackjackCardGame BlackJackGame { get; init; }
        public Blackjack.Networking.GameplayPacketDispatcher PacketDispatcher { get; init; }
        public GameplayHintController HintController { get; init; }
        public GameplayPauseStateController PauseStateController { get; init; }
    }

    internal sealed class GameplayScreenSetupCoordinator
    {
        private readonly ScreenManager screenManager;
        private readonly string theme;
        private readonly List<string> joinedPlayers;
        private readonly NetworkSession networkSession;
        private readonly GameplayPacketProcessingPolicy packetProcessingPolicy;
        private readonly Action<int> calculatePlayerPositions;
        private readonly Func<int, Vector2> getPlayerCardPosition;
        private readonly EventHandler npcHitHandler;
        private readonly EventHandler npcStandHandler;

        public GameplayScreenSetupCoordinator(
            ScreenManager screenManager,
            string theme,
            List<string> joinedPlayers,
            NetworkSession networkSession,
            GameplayPacketProcessingPolicy packetProcessingPolicy,
            Action<int> calculatePlayerPositions,
            Func<int, Vector2> getPlayerCardPosition,
            EventHandler npcHitHandler,
            EventHandler npcStandHandler)
        {
            this.screenManager = screenManager ?? throw new ArgumentNullException(nameof(screenManager));
            this.theme = theme;
            this.joinedPlayers = joinedPlayers;
            this.networkSession = networkSession;
            this.packetProcessingPolicy = packetProcessingPolicy ?? throw new ArgumentNullException(nameof(packetProcessingPolicy));
            this.calculatePlayerPositions = calculatePlayerPositions ?? throw new ArgumentNullException(nameof(calculatePlayerPositions));
            this.getPlayerCardPosition = getPlayerCardPosition ?? throw new ArgumentNullException(nameof(getPlayerCardPosition));
            this.npcHitHandler = npcHitHandler;
            this.npcStandHandler = npcStandHandler;
        }

        public GameplayScreenSetupResult Build(Rectangle safeArea)
        {
            var blackJackGame = new BlackjackCardGame(
                safeArea,
                new Vector2(safeArea.Left + safeArea.Width / 2 - 50, safeArea.Top + 20),
                getPlayerCardPosition,
                screenManager,
                theme);

            ConfigureNetworkMode(blackJackGame);

            var playerListSyncCoordinator = new GameplayPlayerListSyncCoordinator(
                blackJackGame,
                () => networkSession,
                calculatePlayerPositions);

            var packetHandlers = new GameplayPacketHandlers(
                blackJackGame,
                packetProcessingPolicy,
                playerListSyncCoordinator);

            InitializeGame(blackJackGame);

            var gradientTexture = screenManager.Game.Content.Load<Texture2D>(Path.Combine("Images", "UI", "gradient"));
            var hintController = new GameplayHintController(safeArea, gradientTexture, GameSettings.Instance.ShowHints);
            var pauseStateController = new GameplayPauseStateController(screenManager);

            var packetDispatcher = GameplayPacketDispatcherFactory.Create(
                packetHandlers.HandlePlayerListSyncPacket,
                packetHandlers.HandleCardDealtPacket,
                packetHandlers.HandleBetPlacedPacket,
                packetHandlers.HandleChipAddedPacket,
                packetHandlers.HandlePlayerActionPacket,
                packetHandlers.HandleShuffleSeedPacket,
                packetHandlers.HandleHitActionPacket,
                packetHandlers.HandleStandActionPacket,
                packetHandlers.HandleDoubleActionPacket,
                packetHandlers.HandleSplitActionPacket,
                packetHandlers.HandleInsuranceActionPacket,
                packetHandlers.HandleTurnChangedPacket);

            return new GameplayScreenSetupResult
            {
                BlackJackGame = blackJackGame,
                PacketDispatcher = packetDispatcher,
                HintController = hintController,
                PauseStateController = pauseStateController,
            };
        }

        private void ConfigureNetworkMode(BlackjackCardGame blackJackGame)
        {
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
        }

        private void InitializeGame(BlackjackCardGame blackJackGame)
        {
            blackJackGame.Initialize();
            blackJackGame.UpdateButtonText();

            var sessionBootstrapper = new GameplaySessionBootstrapper(
                blackJackGame,
                joinedPlayers,
                networkSession,
                theme,
                calculatePlayerPositions,
                npcHitHandler,
                npcStandHandler);

            sessionBootstrapper.InitializeGame();
        }
    }
}
