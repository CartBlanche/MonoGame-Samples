//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    class GameplayScreen : GameScreen, CardsFramework.Core.ILanguageAware, CardsFramework.Core.IPausable
    {
        BlackjackCardGame blackJackGame;

        string theme;
        GameplayPauseStateController pauseStateController;
        Rectangle safeArea;

        Vector2[] playerCardOffset;

        NetworkSession networkSession;
        Blackjack.Networking.GameplayPacketDispatcher packetDispatcher;
        GameplayPacketProcessingPolicy packetProcessingPolicy;
        GameplayHintController hintController;

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
            this.packetProcessingPolicy = new GameplayPacketProcessingPolicy(() => this.networkSession);
        }

        private List<string> joinedPlayers;

        /// <summary>
        /// Load content and initializes the actual game.
        /// </summary>
        public override void LoadContent()
        {
            safeArea = ScreenManager.SafeArea;

            var setupCoordinator = new GameplayScreenSetupCoordinator(
                ScreenManager,
                theme,
                joinedPlayers,
                networkSession,
                packetProcessingPolicy,
                CalculatePlayerPositions,
                GetPlayerCardPosition,
                OnNpcHit,
                OnNpcStand);

            GameplayScreenSetupResult setup = setupCoordinator.Build(safeArea);
            blackJackGame = setup.BlackJackGame;
            packetDispatcher = setup.PacketDispatcher;
            hintController = setup.HintController;
            pauseStateController = setup.PauseStateController;

            // Update button text/fonts to match current language
            UpdateButtonText();

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
            if (!coveredByOtherScreen)
            {
                hintController?.Update(gameTime, blackJackGame);
            }

            // Centralized network packet dispatcher
            ProcessNetworkPackets();

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        // Centralized network packet dispatcher
        private void ProcessNetworkPackets()
        {
            packetDispatcher?.Process(networkSession);
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
            if (hintController?.IsActive == true)
            {
                var spriteBatch = ScreenManager.SpriteBatch;
                spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
                hintController.Draw(spriteBatch, ScreenManager.RegularFont, blackJackGame);
                spriteBatch.End();
            }

        }

        /// <summary>
        /// Calculates player positions dynamically based on the actual number of players.
        /// Centers and spreads them evenly across the table.
        /// </summary>
        /// <param name="playerCount">The actual number of players in the game.</param>
        private void CalculatePlayerPositions(int playerCount)
        {
            playerCardOffset = GameplayPlayerLayoutCalculator.Calculate(safeArea, playerCount);
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
            pauseStateController?.PauseGameplayComponents();
        }

        /// <summary>
        /// Returns from pause.
        /// </summary>
        public void ReturnFromPause()
        {
            // Resume the background music
            AudioManager.ResumeMusic();

            // Reveal and enable all previously hidden components
            pauseStateController?.ResumeGameplayComponents();
        }

        /// <summary>
        /// Responds to the event sent when NPC player's choose to "Stand".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void OnNpcStand(object sender, EventArgs e)
        {
            blackJackGame.Stand();
        }

        /// <summary>
        /// Responds to the event sent when NPC player's choose to "Hit".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void OnNpcHit(object sender, EventArgs e)
        {
            blackJackGame.Hit();
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


    }
}