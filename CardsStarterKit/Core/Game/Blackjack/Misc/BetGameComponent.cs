//-----------------------------------------------------------------------------
// BetGameComponent.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CardsFramework;
using GameStateManagement;
using Microsoft.Xna.Framework.Input.Touch;
using System.IO;

namespace Blackjack
{
    public class BetGameComponent : DrawableGameComponent
    {
        List<Player> players;
        string theme;
        int[] assetNames = { 5, 25, 100, 500 };
        Dictionary<int, Texture2D> chipsAssets;
        Texture2D blankChip;
        Vector2[] positions;
        CardsFramework.CardsGame cardGame;
        SpriteBatch spriteBatch;
        Matrix globalTransformation;

        bool isKeyDown = false;

        // In network games, this specifies which player index the local user controls
        public int LocalPlayerIndex { get; set; } = -1;

        Button bet;
        Button clear;

        Vector2 ChipOffset { get; set; }
        float insuranceYPosition;
        Vector2 secondHandOffset;

        List<AnimatedGameComponent> currentChipComponent = new List<AnimatedGameComponent>();
        int currentBet = 0;
        InputState input;
        InputHelper inputHelper;

        /// <summary>
        /// Creates a new instance of the <see cref="BetGameComponent"/> class.
        /// </summary>
        /// <param name="players">A list of participating players.</param>
        /// <param name="input">An instance of 
        /// <see cref="GameStateManagement.InputState"/> which can be used to 
        /// check user input.</param>
        /// <param name="theme">The name of the selcted card theme.</param>
        /// <param name="cardGame">An instance of <see cref="CardsGame"/> which
        /// is the current game.</param>
        public BetGameComponent(List<Player> players, InputState input,
            string theme, CardsGame cardGame, SpriteBatch spriteBatch, Matrix globalTransformation)
            : base(cardGame.Game)
        {
            this.players = players;
            this.theme = theme;
            this.cardGame = cardGame;
            this.input = input;
            this.spriteBatch = spriteBatch;
            this.globalTransformation = globalTransformation;
            chipsAssets = new Dictionary<int, Texture2D>();

            // Calculate proportional values based on screen size
            var blackjackGame = cardGame as BlackjackCardGame;
            if (blackjackGame != null)
            {
                int screenWidth = blackjackGame.ScreenManager.SafeArea.Width;
                int screenHeight = blackjackGame.ScreenManager.SafeArea.Height;
                secondHandOffset = UIConstants.GetBetSecondHandOffset(screenWidth, screenHeight);
                insuranceYPosition = UIConstants.GetInsuranceYPosition(screenHeight);
            }
            else
            {
                // Fallback to default values
                secondHandOffset = new Vector2(25, 30);
                insuranceYPosition = 120;
            }
        }


        /// <summary>
        /// Initializes the component.
        /// </summary>
        public override void Initialize()
        {
            // Get xbox cursor
            inputHelper = null;
            for (int componentIndex = 0; componentIndex < Game.Components.Count; componentIndex++)
            {
                if (Game.Components[componentIndex] is InputHelper)
                {
                    inputHelper = (InputHelper)Game.Components[componentIndex];
                    break;
                }
            }

            // Show mouse
            Game.IsMouseVisible = true;
            base.Initialize();

            // Calculate chips position for the chip buttons which allow placing the bet
            Rectangle size = chipsAssets[assetNames[0]].Bounds;

            Rectangle bounds = new Rectangle(0, 0, ScreenManager.BASE_BUFFER_WIDTH, ScreenManager.BASE_BUFFER_HEIGHT);

            int smallPadding = UIConstants.GetSmallPadding(bounds.Width);
            int chipSpacing = UIConstants.GetChipSpacing(bounds.Height);
            int mediumPadding = UIConstants.GetMediumPadding(bounds.Height);
            int buttonWidth = UIConstants.GetButtonWidth(bounds.Width);
            int buttonHeight = UIConstants.GetButtonHeight(bounds.Height);
            int buttonSpacing = UIConstants.GetButtonSpacing(bounds.Width);

            // Position chip buttons higher to avoid overlap with Deal/Clear buttons
            // Place them above the buttons with extra spacing
            int chipAreaBottomMargin = buttonHeight + (smallPadding * 3); // Space for buttons + gap
            positions[chipsAssets.Count - 1] = new Vector2(bounds.Left + smallPadding,
                bounds.Bottom - size.Height - chipSpacing - chipAreaBottomMargin);
            for (int chipIndex = 2; chipIndex <= chipsAssets.Count; chipIndex++)
            {
                size = chipsAssets[assetNames[chipsAssets.Count - chipIndex]].Bounds;
                positions[chipsAssets.Count - chipIndex] = positions[chipsAssets.Count - (chipIndex - 1)] -
                    new Vector2(0, size.Height + smallPadding);
            }

            // Initialize bet button
            bet = new Button("ButtonRegular", "ButtonPressed", input, cardGame, spriteBatch, globalTransformation)
            {
                Bounds = new Rectangle(bounds.Left + smallPadding, bounds.Bottom - buttonHeight - smallPadding, buttonWidth, buttonHeight),
                Font = cardGame.Font,
                Text = "Deal",
            };
            bet.Click += Bet_Click;
            Game.Components.Add(bet);

            // Initialize clear button
            clear = new Button("ButtonRegular", "ButtonPressed", input, cardGame, spriteBatch, globalTransformation)
            {
                Bounds = new Rectangle(bounds.Left + smallPadding + buttonWidth + smallPadding, bounds.Bottom - buttonHeight - smallPadding, buttonWidth, buttonHeight),
                Font = cardGame.Font,
                Text = "Clear",
            };
            clear.Click += Clear_Click;
            Game.Components.Add(clear);
            ShowAndEnableButtons(false);
        }

        /// <summary>
        /// Load component content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load blank chip texture
            blankChip = Game.Content.Load<Texture2D>(Path.Combine("Images", "Chips", "chipWhite"));

            // Load chip textures
            int[] assetNames = { 5, 25, 100, 500 };
            for (int chipIndex = 0; chipIndex < assetNames.Length; chipIndex++)
            {
                chipsAssets.Add(assetNames[chipIndex], Game.Content.Load<Texture2D>(Path.Combine("Images", "Chips", $"chip{assetNames[chipIndex]}")));
            }
            positions = new Vector2[assetNames.Length];

            base.LoadContent();
        }

        /// <summary>
        /// Perform update logic related to the component.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to 
        /// this method.</param>
        public override void Update(GameTime gameTime)
        {
            if (players.Count > 0)
            {
                // If betting is possible
                if (((BlackjackCardGame)cardGame).State == BlackjackGameState.Betting &&
                    !((BlackjackPlayer)players[players.Count - 1]).IsDoneBetting)
                {
                    int playerIndex = GetCurrentPlayer();

                    BlackjackPlayer player = (BlackjackPlayer)players[playerIndex];

                    // If the player is an AI player, have it bet
                    if (player is BlackjackAIPlayer)
                    {
                        ShowAndEnableButtons(false);
                        int bet = ((BlackjackAIPlayer)player).AIBet();

                        BlackjackCardGame blackjackGame = cardGame as BlackjackCardGame;

                        if (bet > 0)
                        {
                            AddChip(playerIndex, bet, false);
                        }
                        else
                        {
                            // Show that the player has passed on this round
                            blackjackGame?.ShowPlayerPass(playerIndex);
                        }

                        // Mark AI player as done betting
                        player.IsDoneBetting = true;

                        // Broadcast the AI's bet to network
                        if (blackjackGame != null && blackjackGame.IsNetworkGame && blackjackGame.IsHost)
                        {
                            blackjackGame.BroadcastBetPlaced((byte)playerIndex, bet);
                        }

                        currentChipComponent.Clear();
                        currentBet = 0;
                    }
                    else
                    {
                        // Reveal the input buttons for a human player and handle input
                        // remember that buttons handle their own imput, so we only check
                        // for input on the chip buttons
                        ShowAndEnableButtons(true);

                        HandleInput();
                    }
                }

                // Once all players are done betting, advance the game to the dealing stage
                if (((BlackjackPlayer)players[players.Count - 1]).IsDoneBetting)
                {
                    BlackjackCardGame blackjackGame = ((BlackjackCardGame)cardGame);

                    if (!blackjackGame.CheckForRunningAnimations<AnimatedGameComponent>())
                    {
                        ShowAndEnableButtons(false);
                        blackjackGame.State = BlackjackGameState.Dealing;

                        Enabled = false;
                    }
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Gets the player which is currently betting. This is the first player who has
        /// yet to finish betting.
        /// </summary>
        /// <returns>The player which is currently betting.</returns>
        private int GetCurrentPlayer()
        {
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                if (!((BlackjackPlayer)players[playerIndex]).IsDoneBetting)
                {
                    return playerIndex;
                }
            }
            return -1;
        }

        /// <summary>
        /// Handle the input of adding chip on all platform
        /// </summary>
        /// <param name="mouseState">Mouse input information.</param>
        private void HandleInput()
        {
            bool isClicked = false;
            Vector2 position = Vector2.Zero;

            // Check for tap gestures (touch release)
            if (input.Gestures.Count > 0 && input.Gestures[0].GestureType == GestureType.Tap)
            {
                isClicked = true;
                position = input.Gestures[0].Position;
            }

            // Check for mouse click (button was pressed last frame, released this frame)
            bool wasPressed = input.LastMouseState.LeftButton == ButtonState.Pressed;
            bool isReleased = input.CurrentMouseState.LeftButton == ButtonState.Released;

            if (wasPressed && isReleased)
            {
                isClicked = true;
                position = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
            }

            // Handle chip interaction logic only on click/tap completion
            if (isClicked)
            {
                int chipValue = GetIntersectingChipValue(position);
                if (chipValue != 0)
                {
                    // Determine which player should receive the chip
                    int targetPlayerIndex;
                    BlackjackCardGame blackjackGame = cardGame as BlackjackCardGame;

                    if (blackjackGame != null && blackjackGame.IsNetworkGame && LocalPlayerIndex >= 0)
                    {
                        // In network games, always bet for the local player
                        targetPlayerIndex = LocalPlayerIndex;
                    }
                    else
                    {
                        // In single-player, bet for the current player
                        targetPlayerIndex = GetCurrentPlayer();
                    }

                    AddChip(targetPlayerIndex, chipValue, false);
                }
            }
        }

        /// <summary>
        /// Get which chip intersects with a given position.
        /// </summary>
        /// <param name="position">The position to check for intersection.</param>
        /// <returns>The value of the chip intersecting with the specified position, or
        /// 0 if no chips intersect with the position.</returns>
        private int GetIntersectingChipValue(Vector2 position)
        {
            Rectangle size;
            // Calculate the bounds of the position
            Rectangle touchTap = new Rectangle((int)position.X - 1,
                (int)position.Y - 1, 2, 2);
            for (int chipIndex = 0; chipIndex < chipsAssets.Count; chipIndex++)
            {
                // Calculate the bounds of the asset
                size = chipsAssets[assetNames[chipIndex]].Bounds;
                size.X = (int)positions[chipIndex].X;
                size.Y = (int)positions[chipIndex].Y;
                if (size.Intersects(touchTap))
                {
                    return assetNames[chipIndex];
                }
            }

            return 0;
        }

        /// <summary>
        /// Draws the component
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to 
        /// this method.</param>
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);

            // Draws the chips
            for (int chipIndex = 0; chipIndex < chipsAssets.Count; chipIndex++)
            {
                spriteBatch.Draw(chipsAssets[assetNames[chipIndex]], positions[chipIndex],
                    Color.White);
            }

            BlackjackPlayer player;

            // Draws the player balance, bet amount, and names below chip circles
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                BlackJackTable table = (BlackJackTable)cardGame.GameTable;

                // Account for scaled ring texture
                float ringScale = UIConstants.GetChipScale();
                float scaledRingHeight = table.RingTexture.Bounds.Height * ringScale;

                // Position text below the chip circle center
                // RingOffset puts us at the circle center, so add half the scaled height to get to bottom
                Vector2 basePosition = table[playerIndex] + table.RingOffset +
                    new Vector2(0, scaledRingHeight / 2f + 10); // 10px padding below circle

                player = (BlackjackPlayer)players[playerIndex];

                // Draw bet amount (top line)
                spriteBatch.DrawString(cardGame.Font, "$" + player.BetAmount.ToString(),
                    basePosition, Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

                // Draw balance (second line)
                spriteBatch.DrawString(cardGame.Font, "$" + player.Balance.ToString(),
                    basePosition + new Vector2(0, 20), Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

                // Draw player name with AI indicator (bottom line)
                string playerName = player.Name;
                bool isAI = player is BlackjackAIPlayer;
                Color nameColor = isAI ? Color.Yellow : Color.Cyan; // Yellow for AI, Cyan for human

                // Strip GUID suffix from human player names for display (but keep full name for network identification)
                string displayName = CardsFramework.UIUtility.StripGuidSuffix(playerName);

                // Add (AI) suffix for AI players
                if (isAI)
                {
                    displayName = $"{displayName} (AI)";
                }
                // Add (Host) suffix for the first human player in network games
                else if (cardGame is BlackjackCardGame blackjackGame &&
                         blackjackGame.IsNetworkGame &&
                         blackjackGame.NetworkSession != null &&
                         playerIndex == 0)
                {
                    displayName = $"{displayName} (Host)";
                    nameColor = Color.LightGreen; // Distinct color for host
                }

                spriteBatch.DrawString(cardGame.Font, displayName,
                    basePosition + new Vector2(0, 40), nameColor, 0f, Vector2.Zero, 0.70f, SpriteEffects.None, 0f);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Adds the chip to one of the player betting zones.
        /// </summary>
        /// <param name="playerIndex">Index of the player for whom to add 
        /// a chip.</param>
        /// <param name="chipValue">The value on the chip to add.</param>
        /// <param name="secondHand">True if this chip is added to the chip pile
        /// belonging to the player's second hand.</param>
        /// <param name="sendToNetwork">Whether to send this chip addition over the network. 
        /// Set to false when receiving chip additions from the network to avoid loops.</param>
        public void AddChip(int playerIndex, int chipValue, bool secondHand, bool sendToNetwork = true)
        {
            // Only add the chip if the bet is successfully performed
            if (((BlackjackPlayer)players[playerIndex]).Bet(chipValue))
            {
                currentBet += chipValue;
                // Add chip component
                AnimatedGameComponent chipComponent = new AnimatedGameComponent(cardGame,
                    chipsAssets[chipValue], spriteBatch, globalTransformation)
                {
                    Visible = false
                };

                Game.Components.Add(chipComponent);

                // Calculate the position for the new chip
                Vector2 position;
                // Get the proper offset according to the platform (pc, phone, xbox)
                Vector2 offset = GetChipOffset(playerIndex, secondHand);

                position = cardGame.GameTable[playerIndex] + offset +
                    new Vector2(-currentChipComponent.Count * 2, currentChipComponent.Count * 1);


                // Find the index of the chip
                int currentChipIndex = 0;
                for (int chipIndex = 0; chipIndex < chipsAssets.Count; chipIndex++)
                {
                    if (assetNames[chipIndex] == chipValue)
                    {
                        currentChipIndex = chipIndex;
                        break;
                    }
                }

                // Add transition animation
                chipComponent.AddAnimation(new TransitionGameComponentAnimation(
                    positions[currentChipIndex], position)
                {
                    Duration = TimeSpan.FromSeconds(1f),
                    PerformBeforeStart = ShowComponent,
                    PerformBeforSartArgs = chipComponent,
                    PerformWhenDone = PlayBetSound
                });

                // Add flip animation
                chipComponent.AddAnimation(new FlipGameComponentAnimation()
                {
                    Duration = TimeSpan.FromSeconds(1f),
                    AnimationCycles = 3,
                });

                currentChipComponent.Add(chipComponent);

                // Send chip addition to network in real-time (if enabled)
                if (sendToNetwork)
                {
                    BlackjackCardGame blackjackGame = cardGame as BlackjackCardGame;
                    if (blackjackGame != null && blackjackGame.IsNetworkGame)
                    {
                        if (blackjackGame.IsHost)
                        {
                            // Host broadcasts the chip addition to all clients
                            blackjackGame.BroadcastChipAdded((byte)playerIndex, chipValue);
                        }
                        else
                        {
                            // Client sends their chip addition to the host
                            blackjackGame.SendChipAdded((byte)playerIndex, chipValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to show component
        /// </summary>
        /// <param name="obj"></param>
        void ShowComponent(object obj)
        {
            ((AnimatedGameComponent)obj).Visible = true;
        }

        /// <summary>
        /// Helper method to play bet sound
        /// </summary>
        /// <param name="obj"></param>
        void PlayBetSound(object obj)
        {
            AudioManager.PlaySound("Bet");
        }

        /// <summary>
        /// Adds chips to a specified player.
        /// </summary>
        /// <param name="playerIndex">Index of the player.</param>
        /// <param name="amount">The total amount to add.</param>
        /// <param name="insurance">If true, an insurance chip is added instead of
        /// regular chips.</param>
        /// <param name="secondHand">True if chips are to be added to the player's
        /// second hand.</param>
        public void AddChips(int playerIndex, float amount, bool insurance, bool secondHand)
        {
            if (insurance)
            {
                AddInsuranceChipAnimation(amount);
            }
            else
            {
                AddChips(playerIndex, amount, secondHand);
            }
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            ShowAndEnableButtons(true);
            currentChipComponent.Clear();
        }

        /// <summary>
        /// Updates the balance of all players in light of their bets and the dealer's
        /// hand.
        /// </summary>
        /// <param name="dealerPlayer">Player object representing the dealer.</param>
        public void CalculateBalance(BlackjackPlayer dealerPlayer)
        {
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                BlackjackPlayer player = (BlackjackPlayer)players[playerIndex];

                // Calculate first factor, which represents the amount of the first
                // hand bet which returns to the player
                float factor = CalculateFactorForHand(dealerPlayer, player,
                    HandTypes.First);


                if (player.IsSplit)
                {
                    // Calculate the return factor for the second hand
                    float factor2 = CalculateFactorForHand(dealerPlayer, player,
                        HandTypes.Second);
                    // Calculate the initial bet performed by the player
                    float initialBet =
                        player.BetAmount /
                        ((player.Double ? 2f : 1f) + (player.SecondDouble ? 2f : 1f));

                    float bet1 = initialBet * (player.Double ? 2f : 1f);
                    float bet2 = initialBet * (player.SecondDouble ? 2f : 1f);

                    // Update the balance in light of the bets and results
                    player.Balance += bet1 * factor + bet2 * factor2;

                    if (player.IsInsurance && dealerPlayer.BlackJack)
                    {
                        player.Balance += initialBet;
                    }
                }
                else
                {
                    if (player.IsInsurance && dealerPlayer.BlackJack)
                    {
                        player.Balance += player.BetAmount;
                    }

                    // Update the balance in light of the bets and results
                    player.Balance += player.BetAmount * factor;
                }

                player.ClearBet();
            }
        }

        /// <summary>
        /// Adds chips to a specified player in order to reach a specified bet amount.
        /// </summary>
        /// <param name="playerIndex">Index of the player to whom the chips are to
        /// be added.</param>
        /// <param name="amount">The bet amount to add to the player.</param>
        /// <param name="secondHand">True to add the chips to the player's second
        /// hand, false to add them to the first hand.</param>
        private void AddChips(int playerIndex, float amount, bool secondHand)
        {
            int[] assetNames = { 5, 25, 100, 500 };

            while (amount > 0)
            {
                if (amount >= 5)
                {
                    // Add the chip with the highest possible value
                    for (int chipIndex = assetNames.Length; chipIndex > 0; chipIndex--)
                    {
                        while (assetNames[chipIndex - 1] <= amount)
                        {
                            AddChip(playerIndex, assetNames[chipIndex - 1], secondHand);
                            amount -= assetNames[chipIndex - 1];
                        }
                    }
                }
                else
                {
                    amount = 0;
                }
            }
        }

        /// <summary>
        /// Animates the placement of an insurance chip on the table.
        /// </summary>
        /// <param name="amount">The amount which should appear on the chip.</param>
        private void AddInsuranceChipAnimation(float amount)
        {
            // Add chip component
            AnimatedGameComponent chipComponent = new AnimatedGameComponent(cardGame, blankChip, spriteBatch, globalTransformation)
            {
                TextColor = Color.Black,
                Enabled = true,
                Visible = false
            };

            Game.Components.Add(chipComponent);

            // Add transition animation
            chipComponent.AddAnimation(new TransitionGameComponentAnimation(positions[0],
                new Vector2(ScreenManager.BASE_BUFFER_WIDTH / 2, insuranceYPosition))
            {
                PerformBeforeStart = ShowComponent,
                PerformBeforSartArgs = chipComponent,
                PerformWhenDone = ShowChipAmountAndPlayBetSound,
                PerformWhenDoneArgs = new object[] { chipComponent, amount },
                Duration = TimeSpan.FromSeconds(1),
                StartTime = DateTime.Now
            });

            // Add flip animation
            chipComponent.AddAnimation(new FlipGameComponentAnimation()
            {
                Duration = TimeSpan.FromSeconds(1f),
                AnimationCycles = 3,
            });
        }

        /// <summary>
        /// Helper method to show the amount on the chip and play bet sound
        /// </summary>
        /// <param name="obj"></param>
        void ShowChipAmountAndPlayBetSound(object obj)
        {
            object[] arr = (object[])obj;
            ((AnimatedGameComponent)arr[0]).Text = arr[1].ToString();
            AudioManager.PlaySound("Bet");
        }

        /// <summary>
        /// Gets the offset at which newly added chips should be placed.
        /// </summary>
        /// <param name="playerIndex">Index of the player to whom the chip 
        /// is added.</param>
        /// <param name="secondHand">True if the chip is added to the player's second
        /// hand, false otherwise.</param>
        /// <returns>The offset from the player's position where chips should be
        /// placed.</returns>
        private Vector2 GetChipOffset(int playerIndex, bool secondHand)
        {
            Vector2 offset = Vector2.Zero;

            BlackJackTable table = ((BlackJackTable)cardGame.GameTable);

            // The ring is drawn with center origin, so we need to account for that
            // Ring position is at the CENTER of the scaled ring texture
            float ringScale = UIConstants.GetChipScale();

            // Since ring is drawn from center, the offset should just center the chip
            // within the ring (no need to calculate top-left position)
            offset = table.RingOffset - new Vector2(blankChip.Bounds.Width / 2f, blankChip.Bounds.Height / 2f);

            if (secondHand == true)
            {
                offset += secondHandOffset;
            }

            return offset;
        }

        /// <summary>
        /// Show and enable, or hide and disable, the bet related buttons.
        /// </summary>
        /// <param name="visibleEnabled">True to show and enable the buttons, false
        /// to hide and disable them.</param>
        private void ShowAndEnableButtons(bool visibleEnabled)
        {
            bet.Visible = visibleEnabled;
            bet.Enabled = visibleEnabled;
            clear.Visible = visibleEnabled;
            clear.Enabled = visibleEnabled;
        }

        /// <summary>
        /// Returns a factor which determines how much of a bet a player should get 
        /// back, according to the outcome of the round.
        /// </summary>
        /// <param name="dealerPlayer">The player representing the dealer.</param>
        /// <param name="player">The player for whom we calculate the factor.</param>
        /// <param name="currentHand">The hand to calculate the factor for.</param>
        /// <returns></returns>
        private float CalculateFactorForHand(BlackjackPlayer dealerPlayer,
            BlackjackPlayer player, HandTypes currentHand)
        {
            float factor;

            bool blackjack, bust, considerAce;
            int playerValue;
            player.CalculateValues();

            // Get some player status information according to the desired hand
            switch (currentHand)
            {
                case HandTypes.First:
                    blackjack = player.BlackJack;
                    bust = player.Bust;
                    playerValue = player.FirstValue;
                    considerAce = player.FirstValueConsiderAce;
                    break;
                case HandTypes.Second:
                    blackjack = player.SecondBlackJack;
                    bust = player.SecondBust;
                    playerValue = player.SecondValue;
                    considerAce = player.SecondValueConsiderAce;
                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }

            if (considerAce)
            {
                playerValue += 10;
            }


            if (bust)
            {
                factor = -1; // Bust
            }
            else if (dealerPlayer.Bust)
            {
                if (blackjack)
                {
                    factor = 1.5f; // Win BlackJack
                }
                else
                {
                    factor = 1; // Win
                }
            }
            else if (dealerPlayer.BlackJack)
            {
                if (blackjack)
                {
                    factor = 0; // Push BlackJack
                }
                else
                {
                    factor = -1; // Lose BlackJack
                }
            }
            else if (blackjack)
            {
                factor = 1.5f;
            }
            else
            {
                int dealerValue = dealerPlayer.FirstValue;

                if (dealerPlayer.FirstValueConsiderAce)
                {
                    dealerValue += 10;
                }

                if (playerValue > dealerValue)
                {
                    factor = 1; // Win
                }
                else if (playerValue < dealerValue)
                {
                    factor = -1; // Lose
                }
                else
                {
                    factor = 0; // Push
                }
            }
            return factor;
        }

        /// <summary>
        /// Handles the Click event of the Clear button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Clear_Click(object sender, EventArgs e)
        {
            // Clear current player chips from screen and resets his bet
            currentBet = 0;
            ((BlackjackPlayer)players[GetCurrentPlayer()]).ClearBet();
            for (int chipComponentIndex = 0; chipComponentIndex < currentChipComponent.Count; chipComponentIndex++)
            {
                Game.Components.Remove(currentChipComponent[chipComponentIndex]);
            }
            currentChipComponent.Clear();
        }

        /// <summary>
        /// Handles the Click event of the Bet button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Bet_Click(object sender, EventArgs e)
        {
            // Finish the bet
            int playerIndex;
            BlackjackCardGame blackjackGame = cardGame as BlackjackCardGame;

            if (blackjackGame != null && blackjackGame.IsNetworkGame && LocalPlayerIndex >= 0)
            {
                // In network games, mark the LOCAL player as done betting
                playerIndex = LocalPlayerIndex;
            }
            else
            {
                // In single-player, use current player
                playerIndex = GetCurrentPlayer();
            }

            int finalBetAmount = currentBet;

            // If the player did not bet, show that he has passed on this round
            if (currentBet == 0)
            {
                ((BlackjackCardGame)cardGame).ShowPlayerPass(playerIndex);
            }

            ((BlackjackPlayer)players[playerIndex]).IsDoneBetting = true;

            // Send/broadcast bet to network
            if (blackjackGame != null && blackjackGame.IsNetworkGame)
            {
                if (blackjackGame.IsHost)
                {
                    // Host broadcasts the bet to all clients
                    blackjackGame.BroadcastBetPlaced((byte)playerIndex, finalBetAmount);
                }
                else
                {
                    // Client sends their bet to the host
                    blackjackGame.SendBetPlaced((byte)playerIndex, finalBetAmount);
                }
            }

            currentChipComponent.Clear();
            currentBet = 0;
        }
    }
}