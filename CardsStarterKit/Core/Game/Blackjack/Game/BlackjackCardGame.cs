//-----------------------------------------------------------------------------
// BlackjackGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CardsFramework;
using Microsoft.Xna.Framework;
using System.Threading;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using GameStateManagement;
using System.Reflection;
using System.IO;

namespace Blackjack
{
    class BlackjackCardGame : CardsGame
    {
        public Microsoft.Xna.Framework.Net.NetworkSession NetworkSession { get; set; }
        public bool IsNetworkGame { get; set; }
        public bool IsHost { get; set; }

        // Public accessor for the players list (needed for network synchronization)
        public System.Collections.Generic.List<CardsFramework.Player> Players => players;

        private int currentShuffleSeed;

        Dictionary<Player, string> playerHandValueTexts =
            new Dictionary<Player, string>();
        Dictionary<Player, string> playerSecondHandValueTexts =
            new Dictionary<Player, string>();
        private Hand deadCards = new Hand(); // stores used cards
        private BlackjackPlayer dealerPlayer;
        bool[] turnFinishedByPlayer;
        
        /// <summary>
        /// Gets the animation duration multiplier based on the AnimationSpeed setting.
        /// Fast = 0.5x, Normal = 1.0x, Slow = 1.5x
        /// </summary>
        private float AnimationSpeedMultiplier =>
            GameSettings.Instance.AnimationSpeed switch
            {
                AnimationSpeed.Fast => 0.5f,
                AnimationSpeed.Slow => 1.5f,
                _ => 1.0f // Normal
            };
        
        TimeSpan DealDuration => TimeSpan.FromMilliseconds(500 * AnimationSpeedMultiplier);

        AnimatedHandGameComponent[] animatedHands;
        // An additional list for managing hands created when performing a split.
        AnimatedHandGameComponent[] animatedSecondHands;

        BetGameComponent betGameComponent;
        AnimatedHandGameComponent dealerHandComponent;
        DeckDisplayComponent deckDisplay;
        Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        Button newGame;
        bool showInsurance;

        // An offset used for drawing the second hand which appears after a split in
        // the correct location. Calculated proportionally based on screen size.
        Vector2 secondHandOffset;
        // Ring offset is now calculated proportionally in the constructor

        Vector2 frameSize;

        public BlackjackGameState State { get; set; }
        ScreenManager screenManager;

        /// <summary>
        /// Public accessor for screen manager (needed for hand components to calculate scaling)
        /// </summary>
        public ScreenManager ScreenManager => screenManager;

        /// <summary>
        /// Creates a new instance of the <see cref="BlackjackCardGame"/> class.
        /// </summary>
        /// <param name="tableBounds">The table bounds. These serves as the bounds for 
        /// the game's main area.</param>
        /// <param name="dealerPosition">Position for the dealer's deck.</param>
        /// <param name="placeOrder">A method that translate a player index into the
        /// position of his deck on the game table.</param>
        /// <param name="screenManager">The games <see cref="ScreenManager"/>.</param>
        /// <param name="theme">The game's deck theme name.</param>
        public BlackjackCardGame(Rectangle tableBounds, Vector2 dealerPosition,
            Func<int, Vector2> placeOrder, ScreenManager screenManager, string theme)
            : base(6, 0, CardSuit.AllSuits, CardsFramework.CardValue.NonJokers,
            BlackjackConstants.MinPlayers, BlackjackConstants.MaxPlayers, new BlackJackTable(UIConstants.GetRingOffset(screenManager.SafeArea.Height), tableBounds,
                dealerPosition, BlackjackConstants.MaxPlayers, placeOrder, theme, screenManager.Game, screenManager.SpriteBatch, screenManager.GlobalTransformation),
            theme, screenManager.Game)
        {
            dealerPlayer = new BlackjackPlayer("Dealer", this);
            turnFinishedByPlayer = new bool[MaximumPlayers];
            this.screenManager = screenManager;

            // Calculate proportional UI sizes based on screen dimensions
            secondHandOffset = UIConstants.GetSecondHandOffset(screenManager.SafeArea.Width, screenManager.SafeArea.Height);
            frameSize = UIConstants.GetFrameSize(screenManager.SafeArea.Width, screenManager.SafeArea.Height);

            if (animatedHands == null)
            {
                animatedHands = new AnimatedHandGameComponent[BlackjackConstants.MaxPlayers];
            }
            if (animatedSecondHands == null)
            {
                animatedSecondHands = new AnimatedHandGameComponent[BlackjackConstants.MaxPlayers];
            }
        }

        /// <summary>
        /// Performs necessary initializations.
        /// </summary>
        public void Initialize()
        {
            base.LoadContent();
            // Initialize a new bet component
            // You may need to pass input state from elsewhere
            betGameComponent = new BetGameComponent(players, screenManager.InputState, Theme, this, screenManager.SpriteBatch, screenManager.GlobalTransformation);
            Game.Components.Add(betGameComponent);

            // Calculate proportional dimensions
            int screenWidth = screenManager.SafeArea.Width;
            int screenHeight = screenManager.SafeArea.Height;
            int smallPadding = UIConstants.GetSmallPadding(screenWidth);
            int mediumPadding = UIConstants.GetMediumPadding(screenHeight);
            int buttonWidth = UIConstants.GetButtonWidth(screenWidth);
            int buttonHeight = UIConstants.GetButtonHeight(screenHeight);
            int buttonSpacing = UIConstants.GetButtonSpacing(screenWidth);
            int wideButtonWidth = UIConstants.GetWideButtonWidth(screenWidth);

            // Initialize the game buttons
            string[] buttonsText = { "Hit", "Stand", "Double", "Split", "Insurance" };
            for (int buttonIndex = 0; buttonIndex < buttonsText.Length; buttonIndex++)
            {
                Button button = new Button("ButtonRegular", "ButtonPressed",
                    screenManager.InputState, this, screenManager.SpriteBatch, screenManager.GlobalTransformation)
                {
                    Text = buttonsText[buttonIndex],
                    Bounds = new Rectangle(screenManager.SafeArea.Left + smallPadding + buttonIndex * buttonSpacing,
                        screenManager.SafeArea.Bottom - buttonHeight - smallPadding,
                    buttonWidth, buttonHeight),
                    Font = this.Font,
                    Visible = false,
                    Enabled = false
                };
                buttons.Add(buttonsText[buttonIndex], button);
                Game.Components.Add(button);
            }

            newGame = new Button("ButtonRegular", "ButtonPressed",
                    screenManager.InputState, this, screenManager.SpriteBatch, screenManager.GlobalTransformation)
            {
                Text = "New Hand",

                Bounds = new Rectangle(screenManager.SafeArea.Left + smallPadding,
                    screenManager.SafeArea.Bottom - buttonHeight - smallPadding, wideButtonWidth, buttonHeight),
                Font = this.Font,
                Visible = false,
                Enabled = false
            };

            // Alter the insurance button's bounds as it is considerably larger than
            // the other buttons
            Rectangle insuranceBounds = buttons["Insurance"].Bounds;
            insuranceBounds.Width = wideButtonWidth;
            buttons["Insurance"].Bounds = insuranceBounds;

            newGame.Click += newGame_Click;
            Game.Components.Add(newGame);

            // Register to click event
            buttons["Hit"].Click += Hit_Click;
            buttons["Stand"].Click += Stand_Click;
            buttons["Double"].Click += Double_Click;
            buttons["Split"].Click += Split_Click;
            buttons["Insurance"].Click += Insurance_Click;
        }

        /// <summary>
        /// Perform the game's update logic.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to 
        /// this method.</param>
        public void Update(GameTime gameTime)
        {
            switch (State)
            {
                case BlackjackGameState.Shuffling:
                    {
                        ShowShuffleAnimation();
                    }
                    break;
                case BlackjackGameState.Betting:
                    {
                        EnableButtons(false);
                    }
                    break;
                case BlackjackGameState.Dealing:
                    {
                        // Deal 2 cards and start playing
                        State = BlackjackGameState.Playing;

                        // In network games, only the host deals cards
                        // Clients will receive CardDealt packets and create animations via HandleReceivedCardDealt
                        if (!IsNetworkGame || IsHost)
                        {
                            Deal();
                        }

                        StartPlaying();
                    }
                    break;
                case BlackjackGameState.Playing:
                    {
                        // Calculate players' current hand values
                        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                        {
                            ((BlackjackPlayer)players[playerIndex]).CalculateValues();
                        }
                        dealerPlayer.CalculateValues();

                        // Make sure no animations are running
                        if (!CheckForRunningAnimations<AnimatedCardsGameComponent>())
                        {
                            BlackjackPlayer player =
                                (BlackjackPlayer)GetCurrentPlayer();

                            // If the current player is an AI player, make it play
                            if (player is BlackjackAIPlayer aiPlayer)
                            {
                                if (!IsNetworkGame || IsHost)
                                {
                                    aiPlayer.AIPlay();
                                }
                            }

                            CheckRules();

                            // If all players have finished playing, the 
                            // current round ends
                            if (State == BlackjackGameState.Playing &&
                                GetCurrentPlayer() == null)
                            {
                                EndRound();
                            }

                            // Update button availability according to player options
                            SetButtonAvailability();
                        }
                        else
                            EnableButtons(false);
                    }
                    break;
                case BlackjackGameState.RoundEnd:
                    {
                        if (dealerHandComponent.EstimatedTimeForAnimationsCompletion() == TimeSpan.Zero)
                        {
                            betGameComponent.CalculateBalance(dealerPlayer);
                            // Check if there is enough money to play
                            // then show new game option or tell the player he has lost
                            if (((BlackjackPlayer)players[0]).Balance < 5)
                            {
                                EndGame();
                            }
                            else
                            {
                                newGame.Enabled = true;
                                newGame.Visible = true;
                            }
                        }
                    }
                    break;
                case BlackjackGameState.GameOver:
                    {

                    }
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Shows the card shuffling animation.
        /// </summary>
        private void ShowShuffleAnimation()
        {
            // Hide the deck display during shuffle animation
            if (deckDisplay != null)
            {
                deckDisplay.Visible = false;
            }

            // Create a list of cards for the shuffle animation (only show a subset for performance)
            // Using 52 cards (one deck) is enough for a good visual effect
            var deckCards = new List<TraditionalCard>();
            int cardsToShow = Math.Min(52, dealer.Count); // Show max 52 cards
            for (int i = 0; i < cardsToShow; i++)
            {
                deckCards.Add(dealer[i]);
            }

            // Calculate deck position in center of the right quarter of the table
            Rectangle tableBounds = GameTable.TableBounds;
            float rightQuarterCenter = tableBounds.Left + (tableBounds.Width * 5f / 8f);
            Vector2 deckPosition = new Vector2(rightQuarterCenter, GameTable.DealerPosition.Y);

            // Get scaled card size and shuffle parameters
            Vector2 cardSize = UIConstants.GetCardSize(screenManager.SafeArea.Width, screenManager.SafeArea.Height);
            float splitDistance = UIConstants.GetShuffleSplitDistance(screenManager.SafeArea.Width);
            float cascadeHeight = UIConstants.GetShuffleCascadeHeight(screenManager.SafeArea.Height);

            // Create a riffle shuffle animation ending at the deck position
            var shuffleAnimation = new RiffleShuffleAnimation(
                this,
                deckPosition, // Shuffle ends at the deck position (right quarter center of table)
                TimeSpan.FromSeconds(1.8), // 1.8 seconds - enough time to see the motion
                cardSize) // Scaled card size
            {
                SplitDistance = splitDistance, // Scaled split distance
                CascadeHeight = cascadeHeight // Scaled cascade height
            };

            // Set up callbacks
            shuffleAnimation.OnAnimationComplete = () =>
            {
                AudioManager.PlaySound("Shuffle");

                // Show the deck display after shuffle completes (if setting is enabled)
                if (deckDisplay != null && GameSettings.Instance.ShowCardCount)
                {
                    deckDisplay.Visible = true;
                }

                State = BlackjackGameState.Betting;
            };

            // Create and initialize the shuffle animation component
            var shuffleComponent = new ShuffleAnimationComponent(
                Game,
                shuffleAnimation,
                deckCards,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation);

            Game.Components.Add(shuffleComponent);
            shuffleComponent.Initialize();
        }

        /// <summary>
        /// Helper method to show component (used by other animations)
        /// </summary>
        /// <param name="obj"></param>
        void ShowComponent(object obj)
        {
            ((AnimatedGameComponent)obj).Visible = true;
        }

        /// <summary>
        /// Renders the visual elements for which the game itself is responsible.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to 
        /// this method.</param>
        public void Draw(GameTime gameTime)
        {
            screenManager.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, screenManager.GlobalTransformation);

            switch (State)
            {
                case BlackjackGameState.Playing:
                    {
                        ShowPlayerValues();
                    }
                    break;
                case BlackjackGameState.GameOver:
                    {
                    }
                    break;
                case BlackjackGameState.RoundEnd:
                    {
                        if (dealerHandComponent.EstimatedTimeForAnimationsCompletion() == TimeSpan.Zero)
                        {
                            ShowDealerValue();
                        }
                        ShowPlayerValues();
                    }
                    break;
                default: break;
            }

            screenManager.SpriteBatch.End();
        }

        /// <summary>
        /// Draws the dealer's hand value on the screen.
        /// </summary>
        private void ShowDealerValue()
        {
            // Calculate the value to display
            string dealerValue = dealerPlayer.FirstValue.ToString();
            if (dealerPlayer.FirstValueConsiderAce)
            {
                if (dealerPlayer.FirstValue + 10 == 21)
                {
                    dealerValue = "21";
                }
                else
                {
                    dealerValue += @"\" + (dealerPlayer.FirstValue + 10).ToString();
                }
            }

            // Draw the value
            Vector2 measure = Font.MeasureString(dealerValue);
            Vector2 position = GameTable.DealerPosition - new Vector2(measure.X + 20, 0);

            screenManager.SpriteBatch.Draw(screenManager.BlankTexture,
                new Rectangle((int)position.X - 4, (int)position.Y,
                (int)measure.X + 8, (int)measure.Y), Color.Black);

            screenManager.SpriteBatch.DrawString(Font, dealerValue, position, Color.White);
        }

        /// <summary>
        /// Draws the players' hand value on the screen.
        /// </summary>
        private void ShowPlayerValues()
        {
            Color color = Color.Black;
            Player currentPlayer = GetCurrentPlayer();

            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                BlackjackPlayer player = (BlackjackPlayer)players[playerIndex];
                // The current player's hand value will be red to serve as a visual
                // prompt for who the active player is
                if (player == currentPlayer)
                {
                    color = Color.Red;
                }
                else
                {
                    color = Color.White;
                }

                // Calculate the values to draw
                string playerHandValueText;
                string playerSecondHandValueText = null;
                if (!animatedHands[playerIndex].IsAnimating)
                {
                    if (player.FirstValue > 0)
                    {
                        playerHandValueText = player.FirstValue.ToString();
                        // Take the fact that an ace is wither 1 or 11 into 
                        // consideration when calculating the value to display
                        // Since the ace already counts as 1, we add 10 to get
                        // the alternate value
                        if (player.FirstValueConsiderAce)
                        {
                            if (player.FirstValue + 10 == 21)
                            {
                                playerHandValueText = "21";
                            }
                            else
                            {
                                playerHandValueText += @"\" + (player.FirstValue + 10).ToString();
                            }
                        }
                        playerHandValueTexts[player] = playerHandValueText;
                    }
                    else
                    {
                        playerHandValueText = null;
                    }

                    if (player.IsSplit)
                    {
                        // If the player has performed a split, he has an additional
                        // hand with its own value
                        if (player.SecondValue > 0)
                        {
                            playerSecondHandValueText = player.SecondValue.ToString();
                            if (player.SecondValueConsiderAce)
                            {
                                if (player.SecondValue + 10 == 21)
                                {
                                    playerSecondHandValueText = "21";
                                }
                                else
                                {
                                    playerSecondHandValueText += @"\" + (player.SecondValue + 10).ToString();
                                }
                            }
                            playerSecondHandValueTexts[player] = playerSecondHandValueText;
                        }
                        else
                        {
                            playerSecondHandValueText = null;
                        }
                    }
                }
                else
                {
                    playerHandValueTexts.TryGetValue(player, out playerHandValueText);
                    playerSecondHandValueTexts.TryGetValue(
                        player, out playerSecondHandValueText);
                }

                if (player.IsSplit)
                {
                    // If the player has performed a split, mark the active hand alone
                    // with a red value
                    color = player.CurrentHandType == HandTypes.First &&
                        player == currentPlayer ? Color.Red : Color.White;

                    if (playerHandValueText != null)
                    {
                        DrawValue(animatedHands[playerIndex], playerIndex, playerHandValueText, color);
                    }

                    color = player.CurrentHandType == HandTypes.Second &&
                        player == currentPlayer ? Color.Red : Color.White;

                    if (playerSecondHandValueText != null)
                    {
                        DrawValue(animatedSecondHands[playerIndex], playerIndex, playerSecondHandValueText,
                            color);
                    }
                }
                else
                {
                    // If there is a value to draw, draw it
                    if (playerHandValueText != null)
                    {
                        DrawValue(animatedHands[playerIndex], playerIndex, playerHandValueText, color);
                    }
                }
            }
        }

        /// <summary>
        /// Draws the value of a player's hand above his top card.
        /// The value will be drawn over a black background.
        /// </summary>
        /// <param name="animatedHand">The player's hand.</param>
        /// <param name="place">A number representing the player's position on the
        /// game table.</param>
        /// <param name="value">The value to draw.</param>
        /// <param name="valueColor">The color in which to draw the value.</param>
        private void DrawValue(AnimatedHandGameComponent animatedHand, int place,
            string value, Color valueColor)
        {
            Hand hand = animatedHand.Hand;

            Vector2 position = GameTable.PlaceOrder(place) +
                animatedHand.GetCardRelativePosition(hand.Count - 1);
            Vector2 measure = Font.MeasureString(value);

            position.X += (cardsAssets["CardBack_" + Theme].Bounds.Width - measure.X) / 2;
            position.Y -= measure.Y + 5;

            screenManager.SpriteBatch.Draw(screenManager.BlankTexture,
                new Rectangle((int)position.X - 4, (int)position.Y,
                (int)measure.X + 8, (int)measure.Y), Color.Black);
            screenManager.SpriteBatch.DrawString(Font, value, position, valueColor);

        }

        /// <summary>
        /// Adds a player to the game.
        /// </summary>
        /// <param name="player">The player to add.</param>
        public override void AddPlayer(Player player)
        {
            if (player is BlackjackPlayer && players.Count < MaximumPlayers)
            {
                players.Add(player);
            }
        }

        /// <summary>
        /// Gets the active player.
        /// </summary>
        /// <returns>The first payer who has placed a bet and has not 
        /// finish playing.</returns>
        public override Player GetCurrentPlayer()
        {
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                if (((BlackjackPlayer)players[playerIndex]).MadeBet && turnFinishedByPlayer[playerIndex] == false)
                {
                    return players[playerIndex];
                }
            }
            return null;
        }

        /// <summary>
        /// Calculate the value of a blackjack card.
        /// </summary>
        /// <param name="card">The card to calculate the value for.</param>
        /// <returns>The card's value. All card values are equal to their face number,
        /// except for jack/queen/king which value at 10.</returns>
        /// <remarks>An ace's value will be 1. Game logic will treat it as 11 where
        /// appropriate.</remarks>
        public override int CardValue(TraditionalCard card)
        {
            return Math.Min(base.CardValue(card), 10);
        }

        /// <summary>
        /// Deals 2 cards to each player including the dealer and adds the appropriate 
        /// animations.
        /// </summary>
        public override void Deal()
        {
            if (State == BlackjackGameState.Playing)
            {
                TraditionalCard card;
                for (int dealIndex = 0; dealIndex < 2; dealIndex++)
                {
                    for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                    {
                        if (((BlackjackPlayer)players[playerIndex]).MadeBet)
                        {
                            // Deal a card to one of the players
                            card = dealer.DealCardToHand(players[playerIndex].Hand);

                            AddDealAnimation(card, animatedHands[playerIndex], true, DealDuration,
                                DateTime.Now + TimeSpan.FromSeconds(
                                DealDuration.TotalSeconds * (dealIndex * players.Count + playerIndex)));

                            // Broadcast card dealt in network games (host only)
                            if (IsNetworkGame && IsHost)
                            {
                                BroadcastCardDealt(card, (byte)playerIndex, false, HandTypes.First);
                            }
                        }
                    }
                    // Deal a card to the dealer
                    card = dealer.DealCardToHand(dealerPlayer.Hand);
                    bool isHoleCard = (dealIndex == 0); // First dealer card is the hole card
                    AddDealAnimation(card, dealerHandComponent, isHoleCard, DealDuration, DateTime.Now);

                    // Broadcast dealer card in network games (host only)
                    if (IsNetworkGame && IsHost)
                    {
                        // Use player index 255 to indicate dealer
                        BroadcastCardDealt(card, 255, isHoleCard, HandTypes.First);
                    }
                }
            }
        }

        /// <summary>
        /// Performs necessary initializations needed after dealing the cards in order
        /// to start playing.
        /// </summary>
        public override void StartPlaying()
        {
            // Check that there are enough players to start playing
            if ((MinimumPlayers <= players.Count && players.Count <= MaximumPlayers))
            {
                // Set up and register to gameplay events

                GameRule gameRule = new BustRule(players);
                rules.Add(gameRule);
                gameRule.RuleMatch += BustGameRule;

                gameRule = new BlackJackRule(players);
                rules.Add(gameRule);
                gameRule.RuleMatch += BlackJackGameRule;

                gameRule = new InsuranceRule(dealerPlayer.Hand);
                rules.Add(gameRule);
                gameRule.RuleMatch += InsuranceGameRule;

                // Display the hands participating in the game
                for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                {
                    // Safety check: ensure animated hand component exists before accessing it
                    if (animatedHands[playerIndex] != null)
                    {
                        if (((BlackjackPlayer)players[playerIndex]).MadeBet)
                        {
                            animatedHands[playerIndex].Visible = false;
                        }
                        else
                        {
                            animatedHands[playerIndex].Visible = true;
                        }
                    }
                    else
                    {
                        System.Console.WriteLine($"[StartPlaying] Warning: animatedHands[{playerIndex}] is null for player {players[playerIndex].Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Display an animation when a card is dealt.
        /// </summary>
        /// <param name="card">The card being dealt.</param>
        /// <param name="animatedHand">The animated hand into which the card 
        /// is dealt.</param>
        /// <param name="flipCard">Should the card be flipped after dealing it.</param>
        /// <param name="duration">The animations desired duration.</param>
        /// <param name="startTime">The time at which the animation should 
        /// start.</param>
        public void AddDealAnimation(TraditionalCard card, AnimatedHandGameComponent
            animatedHand, bool flipCard, TimeSpan duration, DateTime startTime)
        {
            // Safety check: if animatedHand is null, we can't create animations
            if (animatedHand == null)
            {
                System.Console.WriteLine($"[AddDealAnimation] ERROR: animatedHand parameter is null! Cannot create animation.");
                return;
            }

            // Get the card location and card component
            int cardLocationInHand = animatedHand.GetCardLocationInHand(card);
            AnimatedCardsGameComponent cardComponent = animatedHand.GetCardGameComponent(cardLocationInHand);

            // Cards are dealt from the deck position in the center of the right quarter
            Rectangle tableBounds = GameTable.TableBounds;
            float rightQuarterCenter = tableBounds.Left + (tableBounds.Width * 5f / 8f);
            Vector2 deckPosition = new Vector2(rightQuarterCenter, GameTable.DealerPosition.Y);

            var cardAnimation = new TransitionGameComponentAnimation(deckPosition,
                animatedHand.CurrentPosition +
                animatedHand.GetCardRelativePosition(cardLocationInHand))
            {
                StartTime = startTime,
                PerformBeforeStart = ShowComponent,
                PerformBeforSartArgs = cardComponent,
                PerformWhenDone = PlayDealSound
            };
            cardAnimation.Duration = duration;

            // Add the transition animation
            cardComponent.AddAnimation(cardAnimation);

            if (flipCard)
            {
                // Add the flip animation
                cardComponent.AddAnimation(new FlipGameComponentAnimation
                {
                    IsFromFaceDownToFaceUp = true,
                    Duration = duration,
                    StartTime = startTime + duration,
                    PerformWhenDone = PlayFlipSound
                });
            }
        }

        /// <summary>
        /// Helper method to play deal sound
        /// </summary>
        /// <param name="obj"></param>
        void PlayDealSound(object obj)
        {
            AudioManager.PlaySound("Deal");
        }

        /// <summary>
        /// Helper method to play flip sound
        /// </summary>
        /// <param name="obj"></param>
        void PlayFlipSound(object obj)
        {
            AudioManager.PlaySound("Flip");
        }

        /// <summary>
        /// Adds an animation which displays an asset over a player's hand. The asset
        /// will appear above the hand and appear to "fall" on top of it.
        /// </summary>
        /// <param name="player">The player over the hand of which to place the
        /// animation.</param>
        /// <param name="assetName">Name of the asset to display above the hand.</param>
        /// <param name="animationHand">Which hand to put cue over.</param>
        /// <param name="waitForHand">Start the cue animation when the animation
        /// of this hand over null of the animation of the currentHand</param>
        void CueOverPlayerHand(BlackjackPlayer player, string assetName,
            HandTypes animationHand, AnimatedHandGameComponent waitForHand)
        {
            // Get the position of the relevant hand
            int playerIndex = players.IndexOf(player);
            AnimatedHandGameComponent currentAnimatedHand;
            Vector2 currentPosition;
            if (playerIndex >= 0)
            {
                switch (animationHand)
                {
                    case HandTypes.First:
                        currentAnimatedHand = animatedHands[playerIndex];
                        currentPosition = currentAnimatedHand.CurrentPosition;
                        break;
                    case HandTypes.Second:
                        currentAnimatedHand = animatedSecondHands[playerIndex];
                        currentPosition = currentAnimatedHand.CurrentPosition +
                            secondHandOffset;
                        break;
                    default:
                        throw new Exception(
                            "Player has an unsupported hand type.");
                }
            }
            else
            {
                currentAnimatedHand = dealerHandComponent;
                currentPosition = currentAnimatedHand.CurrentPosition;
            }

            // Add the animation component 
            AnimatedGameComponent animationComponent =
                new AnimatedGameComponent(this, cardsAssets[assetName], screenManager.SpriteBatch, screenManager.GlobalTransformation)
                {
                    CurrentPosition = currentPosition,
                    Visible = false
                };
            Game.Components.Add(animationComponent);

            // Calculate when to start the animation. The animation will only begin
            // after all hand cards finish animating
            TimeSpan estimatedTimeToCompleteAnimations;
            if (waitForHand != null)
            {
                estimatedTimeToCompleteAnimations = waitForHand.EstimatedTimeForAnimationsCompletion();
            }
            else
            {
                estimatedTimeToCompleteAnimations = currentAnimatedHand.EstimatedTimeForAnimationsCompletion();
            }

            // Add a scale effect animation
            animationComponent.AddAnimation(new ScaleGameComponentAnimation(2.0f, 1.0f)
            {
                StartTime =
                    DateTime.Now + estimatedTimeToCompleteAnimations,
                Duration = TimeSpan.FromSeconds(1f * AnimationSpeedMultiplier),
                PerformBeforeStart = ShowComponent,
                PerformBeforSartArgs = animationComponent
            });
        }

        /// <summary>
        /// Ends the current round.
        /// </summary>
        private void EndRound()
        {
            RevealDealerFirstCard();
            DealerAI();
            ShowResults();
            State = BlackjackGameState.RoundEnd;
        }

        /// <summary>
        /// Causes the dealer's hand to be displayed.
        /// </summary>
        private void ShowDealerHand()
        {
            dealerHandComponent =
                new BlackjackAnimatedDealerHandComponent(-1, dealerPlayer.Hand, this, screenManager.SpriteBatch, screenManager.GlobalTransformation);
            Game.Components.Add(dealerHandComponent);
        }

        /// <summary>
        /// Reveal's the dealer's hidden card.
        /// </summary>
        private void RevealDealerFirstCard()
        {
            // Iterate over all dealer cards expect for the last
            AnimatedCardsGameComponent cardComponent = dealerHandComponent.GetCardGameComponent(1);
            cardComponent.AddAnimation(new FlipGameComponentAnimation()
            {
                Duration = TimeSpan.FromSeconds(0.5 * AnimationSpeedMultiplier),
                StartTime = DateTime.Now
            });
        }

        /// <summary>
        /// Present visual indication as to how the players fared in the current round.
        /// </summary>
        private void ShowResults()
        {
            // Calculate the dealer's hand value
            int dealerValue = dealerPlayer.FirstValue;

            if (dealerPlayer.FirstValueConsiderAce)
            {
                dealerValue += 10;
            }

            // Show each player's result
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                ShowResultForPlayer((BlackjackPlayer)players[playerIndex], dealerValue, HandTypes.First);
                if (((BlackjackPlayer)players[playerIndex]).IsSplit)
                {
                    ShowResultForPlayer((BlackjackPlayer)players[playerIndex], dealerValue, HandTypes.Second);
                }
            }
        }

        /// <summary>
        /// Display's a player's status after the turn has ended.
        /// </summary>
        /// <param name="player">The player for which to display the status.</param>
        /// <param name="dealerValue">The dealer's hand value.</param>
        /// <param name="currentHandType">The player's hand to take into 
        /// account.</param>
        private void ShowResultForPlayer(BlackjackPlayer player, int dealerValue,
            HandTypes currentHandType)
        {
            // Calculate the player's hand value and check his state (blackjack/bust)
            bool blackjack, bust;
            int playerValue;
            switch (currentHandType)
            {
                case HandTypes.First:
                    blackjack = player.BlackJack;
                    bust = player.Bust;

                    playerValue = player.FirstValue;

                    if (player.FirstValueConsiderAce)
                    {
                        playerValue += 10;
                    }
                    break;
                case HandTypes.Second:
                    blackjack = player.SecondBlackJack;
                    bust = player.SecondBust;

                    playerValue = player.SecondValue;

                    if (player.SecondValueConsiderAce)
                    {
                        playerValue += 10;
                    }
                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }
            // The bust or blackjack state are animated independently of this method,
            // so only trigger different outcome indications
            if (player.MadeBet &&
                (!blackjack || (dealerPlayer.BlackJack && blackjack)) && !bust)
            {
                string assetName = GetResultAsset(player, dealerValue, playerValue);

                CueOverPlayerHand(player, assetName, currentHandType, dealerHandComponent);
            }
        }

        /// <summary>
        /// Return the asset name according to the result.
        /// </summary>
        /// <param name="player">The player for which to return the asset name.</param>
        /// <param name="dealerValue">The dealer's hand value.</param>
        /// <param name="playerValue">The player's hand value.</param>
        /// <returns>The asset name</returns>
        private string GetResultAsset(BlackjackPlayer player, int dealerValue, int playerValue)
        {
            string assetName;
            if (dealerPlayer.Bust)
            {
                assetName = "win";
            }
            else if (dealerPlayer.BlackJack)
            {
                if (player.BlackJack)
                {
                    assetName = "push";
                }
                else
                {
                    assetName = "lose";
                }
            }
            else if (playerValue < dealerValue)
            {
                assetName = "lose";
            }
            else if (playerValue > dealerValue)
            {
                assetName = "win";
            }
            else
            {
                assetName = "push";
            }
            return assetName;
        }

        /// <summary>
        /// Have the dealer play. The dealer hits until reaching 17+ and then 
        /// stands.
        /// </summary>
        private void DealerAI()
        {
            // The dealer may have not need to draw additional cards after his first
            // two. Check if this is the case and if so end the dealer's play.
            dealerPlayer.CalculateValues();
            int dealerValue = dealerPlayer.FirstValue;

            if (dealerPlayer.FirstValueConsiderAce)
            {
                dealerValue += 10;
            }

            if (dealerValue > 21)
            {
                dealerPlayer.Bust = true;
                CueOverPlayerHand(dealerPlayer, "bust", HandTypes.First, dealerHandComponent);
            }
            else if (dealerValue == 21)
            {
                dealerPlayer.BlackJack = true;
                CueOverPlayerHand(dealerPlayer, "blackjack", HandTypes.First, dealerHandComponent);
            }

            if (dealerPlayer.BlackJack || dealerPlayer.Bust)
            {
                return;
            }

            // Draw cards until 17 is reached, or the dealer gets a blackjack or busts
            int cardsDealed = 0;
            while (dealerValue <= 17)
            {
                TraditionalCard card = dealer.DealCardToHand(dealerPlayer.Hand);
                AddDealAnimation(card, dealerHandComponent, true, DealDuration,
                    DateTime.Now.AddMilliseconds(1000 * AnimationSpeedMultiplier * (cardsDealed + 1)));
                cardsDealed++;
                dealerPlayer.CalculateValues();
                dealerValue = dealerPlayer.FirstValue;

                if (dealerPlayer.FirstValueConsiderAce)
                {
                    dealerValue += 10;
                }

                if (dealerValue > 21)
                {
                    dealerPlayer.Bust = true;
                    CueOverPlayerHand(dealerPlayer, "bust", HandTypes.First, dealerHandComponent);
                }
            }
        }

        /// <summary>
        /// Displays the hands currently in play.
        /// </summary>
        private void DisplayPlayingHands()
        {

            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                AnimatedHandGameComponent animatedHandGameComponent =
                    new BlackjackAnimatedPlayerHandComponent(playerIndex, players[playerIndex].Hand, this, screenManager.SpriteBatch, screenManager.GlobalTransformation);
                Game.Components.Add(animatedHandGameComponent);
                animatedHands[playerIndex] = animatedHandGameComponent;
            }

            ShowDealerHand();
        }

        /// <summary>
        /// Starts a new game round.
        /// </summary>
        public void StartRound()
        {
            playerHandValueTexts.Clear();
            AudioManager.PlaySound("Shuffle");

            // Reset card dealing sequence tracking for network synchronization
            cardSequenceCounter = 0;
            dealStartTime = DateTime.MinValue;

            // Generate shuffle seed (host only) or wait for it (clients)
            if (IsNetworkGame && IsHost)
            {
                // Host generates a deterministic seed
                currentShuffleSeed = Environment.TickCount;
                dealer.Shuffle(currentShuffleSeed);

                // Broadcast shuffle seed to all clients
                BroadcastShuffleSeed(currentShuffleSeed);
            }
            else if (IsNetworkGame && !IsHost)
            {
                // Client waits for shuffle seed from host
                // The shuffle will be performed when the ShuffleSeedPacket is received
                // For now, just mark that we're waiting
            }
            else
            {
                // Local game - shuffle normally
                dealer.Shuffle();
            }

            DisplayPlayingHands();
            ShowDeckDisplay();
            State = BlackjackGameState.Shuffling;
        }

        /// <summary>
        /// Shows the visual deck display in center of the right quarter of the table
        /// </summary>
        private void ShowDeckDisplay()
        {
            // Remove existing deck display if present
            if (deckDisplay != null && Game.Components.Contains(deckDisplay))
            {
                Game.Components.Remove(deckDisplay);
            }

            // Don't show deck display if card count setting is disabled
            if (!GameSettings.Instance.ShowCardCount)
            {
                return;
            }

            // Position the deck in center of the right quarter of the table
            // Calculate the center of the right quarter for responsive positioning
            Rectangle tableBounds = GameTable.TableBounds;
            float rightQuarterCenter = tableBounds.Left + (tableBounds.Width * 5f / 8f); // 5/8 = center of right quarter
            float deckX = rightQuarterCenter;

            // Keep the same Y position as the dealer's hand
            float deckY = GameTable.DealerPosition.Y;
            Vector2 deckPosition = new Vector2(deckX, deckY);

            // Get scaled layer offset
            Vector2 layerOffset = UIConstants.GetDeckLayerOffset(screenManager.SafeArea.Width);

            // Create new deck display
            deckDisplay = new DeckDisplayComponent(
                Game,
                this,
                deckPosition,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation)
            {
                StackLayers = 4, // Show 4 layers for a nice thick deck
                LayerOffset = layerOffset // Scaled offset for depth
            };

            Game.Components.Add(deckDisplay);
        }

        /// <summary>
        /// Sets the button availability according to the options available to the 
        /// current player.
        /// </summary>
        private void SetButtonAvailability()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();
            // Hide all buttons if no player is in play or the player is an AI player
            if (player == null || player is BlackjackAIPlayer)
            {
                EnableButtons(false);
                ChangeButtonsVisiblility(false);
                return;
            }

            // Show all buttons
            EnableButtons(true);
            ChangeButtonsVisiblility(true);

            // Set insurance button availability
            buttons["Insurance"].Visible = showInsurance;
            buttons["Insurance"].Enabled = showInsurance;

            if (player.IsSplit == false)
            {
                // Remember that the bet amount was already reduced from the balance,
                // so we only need to check if the player has more money than the
                // current bet when trying to double/split

                // Set double button availability
                if (player.BetAmount > player.Balance || player.Hand.Count != 2)
                {
                    buttons["Double"].Visible = false;
                    buttons["Double"].Enabled = false;
                }

                if (player.Hand.Count != 2 ||
                    player.Hand[0].Value != player.Hand[1].Value ||
                    player.BetAmount > player.Balance)
                {
                    buttons["Split"].Visible = false;
                    buttons["Split"].Enabled = false;
                }
            }
            else
            {
                // We've performed a split. Get the initial bet amount to check whether
                // or not we can double the current bet.
                float initialBet = player.BetAmount /
                    ((player.Double ? 2f : 1f) + (player.SecondDouble ? 2f : 1f));

                // Set double button availability.
                if (initialBet > player.Balance || player.CurrentHand.Count != 2)
                {
                    buttons["Double"].Visible = false;
                    buttons["Double"].Enabled = false;
                }

                // Once you've split, you can't split again
                buttons["Split"].Visible = false;
                buttons["Split"].Enabled = false;
            }
        }

        /// <summary>
        /// Checks for running animations.
        /// </summary>
        /// <typeparam name="T">The type of animation to look for.</typeparam>
        /// <returns>True if a running animation of the desired type is found and
        /// false otherwise.</returns>
        internal bool CheckForRunningAnimations<T>() where T : AnimatedGameComponent
        {
            T animationComponent;
            for (int componentIndex = 0; componentIndex < Game.Components.Count; componentIndex++)
            {
                animationComponent = Game.Components[componentIndex] as T;
                if (animationComponent != null)
                {
                    if (animationComponent.IsAnimating)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Ends the game.
        /// </summary>
        private void EndGame()
        {
            // Calculate the estimated time for all playing animations to end
            long estimatedTime = 0;
            AnimatedGameComponent animationComponent;
            for (int componentIndex = 0; componentIndex < Game.Components.Count; componentIndex++)
            {
                animationComponent = Game.Components[componentIndex] as AnimatedGameComponent;
                if (animationComponent != null)
                {
                    estimatedTime = Math.Max(estimatedTime,
                        animationComponent.EstimatedTimeForAnimationsCompletion().Ticks);
                }
            }
            var widthCenter = screenManager.BackbufferWidth / 2;
            var heightCenter = screenManager.BackbufferHeight / 2;

            // Add a component for an empty stalling animation. This actually acts
            // as a timer.
            Texture2D texture = this.Game.Content.Load<Texture2D>(Path.Combine("Images", "youlose"));
            animationComponent = new AnimatedGameComponent(this, texture, screenManager.SpriteBatch, screenManager.GlobalTransformation)
            {
                CurrentPosition = new Vector2(
                    widthCenter - texture.Width / 2,
                    heightCenter - texture.Height / 2),
                Visible = false
            };
            this.Game.Components.Add(animationComponent);

            // Add a button to return to the main menu
            Vector2 center = new Vector2(widthCenter, heightCenter);
            Button backButton = new Button("ButtonRegular", "ButtonPressed",
                screenManager.InputState, this, screenManager.SpriteBatch, screenManager.GlobalTransformation)
            {
                Bounds = new Rectangle((int)center.X - 100, (int)center.Y + 80, 200, 50),
                Font = this.Font,
                Text = "Main Menu",
                Visible = false,
                Enabled = true,
            };

            backButton.Click += backButton_Click;

            // Add stalling animation
            animationComponent.AddAnimation(new AnimatedGameComponentAnimation()
            {
                Duration = TimeSpan.FromTicks(estimatedTime) + TimeSpan.FromSeconds(1),
                PerformWhenDone = ResetGame,
                PerformWhenDoneArgs = new object[] { animationComponent, backButton }
            });
            Game.Components.Add(backButton);
        }

        /// <summary>
        /// Helper method to reset the game
        /// </summary>
        /// <param name="obj"></param>
        void ResetGame(object obj)
        {
            object[] arr = (object[])obj;
            State = BlackjackGameState.GameOver;
            ((AnimatedGameComponent)arr[0]).Visible = true;
            ((Button)arr[1]).Visible = true;

            // Remove all unnecessary game components
            for (int compontneIndex = 0; compontneIndex < Game.Components.Count;)
            {
                if ((Game.Components[compontneIndex] != ((AnimatedGameComponent)arr[0]) &&
                    Game.Components[compontneIndex] != ((Button)arr[1])) &&
                    (Game.Components[compontneIndex] is BetGameComponent ||
                    Game.Components[compontneIndex] is AnimatedGameComponent ||
                    Game.Components[compontneIndex] is Button))
                {
                    Game.Components.RemoveAt(compontneIndex);
                }
                else
                    compontneIndex++;
            }
        }

        /// <summary>
        /// Finishes the current turn.
        /// </summary>
        private void FinishTurn()
        {
            // Remove all unnecessary components
            for (int componentIndex = 0; componentIndex < Game.Components.Count; componentIndex++)
            {
                if (!(Game.Components[componentIndex] is GameTable ||
                    Game.Components[componentIndex] is BlackjackCardGame ||
                    Game.Components[componentIndex] is BetGameComponent ||
                    Game.Components[componentIndex] is Button ||
                    Game.Components[componentIndex] is ScreenManager ||
                    Game.Components[componentIndex] is InputHelper))
                {
                    if (Game.Components[componentIndex] is AnimatedCardsGameComponent)
                    {
                        AnimatedCardsGameComponent animatedCard =
                            (Game.Components[componentIndex] as AnimatedCardsGameComponent);
                        animatedCard.AddAnimation(
                            new TransitionGameComponentAnimation(animatedCard.CurrentPosition,
                            new Vector2(animatedCard.CurrentPosition.X, ScreenManager.BASE_BUFFER_HEIGHT))
                            {
                                Duration = TimeSpan.FromSeconds(0.40),
                                PerformWhenDone = RemoveComponent,
                                PerformWhenDoneArgs = animatedCard
                            });
                    }
                    else
                    {
                        Game.Components.RemoveAt(componentIndex);
                        componentIndex--;
                    }
                }
            }

            // Reset player values
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                (players[playerIndex] as BlackjackPlayer).ResetValues();
                players[playerIndex].Hand.DealCardsToHand(deadCards, players[playerIndex].Hand.Count);
                turnFinishedByPlayer[playerIndex] = false;
                animatedHands[playerIndex] = null;
                animatedSecondHands[playerIndex] = null;
            }

            // Reset the bet component
            betGameComponent.Reset();
            betGameComponent.Enabled = true;

            // Reset dealer
            dealerPlayer.Hand.DealCardsToHand(deadCards, dealerPlayer.Hand.Count);
            dealerPlayer.ResetValues();

            // Reset rules
            rules.Clear();
        }

        /// <summary>
        /// Helper method to remove component
        /// </summary>
        /// <param name="obj"></param>
        void RemoveComponent(object obj)
        {
            Game.Components.Remove((AnimatedGameComponent)obj);
        }

        /// <summary>
        /// Performs the "Stand" move for the current player.
        /// </summary>
        public void Stand()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();
            if (player == null)
                return;

            int playerIndex = players.IndexOf(player);

            // Broadcast to network in network games
            if (IsNetworkGame && IsHost)
            {
                BroadcastStandAction((byte)playerIndex);
            }

            // If the player only has one hand, his turn ends. Otherwise, he now plays
            // using his next hand
            if (player.IsSplit == false)
            {
                turnFinishedByPlayer[playerIndex] = true;
            }
            else
            {
                switch (player.CurrentHandType)
                {
                    case HandTypes.First:
                        if (player.SecondBlackJack)
                        {
                            turnFinishedByPlayer[playerIndex] = true;
                        }
                        else
                        {
                            player.CurrentHandType = HandTypes.Second;
                        }
                        break;
                    case HandTypes.Second:
                        turnFinishedByPlayer[playerIndex] = true;
                        break;
                    default:
                        throw new Exception(
                            "Player has an unsupported hand type.");
                }
            }

            // Broadcast turn change to synchronize active player
            if (IsNetworkGame && IsHost)
            {
                Player nextPlayer = GetCurrentPlayer();
                byte nextPlayerIndex = nextPlayer != null ? (byte)players.IndexOf(nextPlayer) : (byte)255;
                BroadcastTurnChanged(nextPlayerIndex);
            }
        }

        /// <summary>
        /// Performs the "Split" move for the current player.
        /// This includes adding the animations which shows the first hand splitting
        /// into two.
        /// </summary>
        public void Split()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();

            int playerIndex = players.IndexOf(player);

            // Broadcast to network in network games
            if (IsNetworkGame && IsHost)
            {
                BroadcastSplitAction((byte)playerIndex);
            }

            player.InitializeSecondHand();

            Vector2 sourcePosition = animatedHands[playerIndex].GetCardGameComponent(1).CurrentPosition;
            Vector2 targetPosition = animatedHands[playerIndex].GetCardGameComponent(0).CurrentPosition +
                secondHandOffset;
            // Create an animation moving the top card to the second hand location
            AnimatedGameComponentAnimation animation = new TransitionGameComponentAnimation(sourcePosition,
                    targetPosition)
            {
                StartTime = DateTime.Now,
                Duration = TimeSpan.FromSeconds(0.5f)
            };

            // Actually perform the split
            player.SplitHand();

            // Add additional chip stack for the second hand
            betGameComponent.AddChips(playerIndex, player.BetAmount,
                false, true);

            // Initialize visual representation of the second hand
            animatedSecondHands[playerIndex] =
                new BlackjackAnimatedPlayerHandComponent(playerIndex, secondHandOffset,
                    player.SecondHand, this, screenManager.SpriteBatch, screenManager.GlobalTransformation);
            Game.Components.Add(animatedSecondHands[playerIndex]);

            AnimatedCardsGameComponent animatedGameComponet = animatedSecondHands[playerIndex].GetCardGameComponent(0);
            animatedGameComponet.IsFaceDown = false;
            animatedGameComponet.AddAnimation(animation);

            // Deal an additional cards to each of the new hands
            TraditionalCard card = dealer.DealCardToHand(player.Hand);
            AddDealAnimation(card, animatedHands[playerIndex], true, DealDuration,
                DateTime.Now + animation.EstimatedTimeForAnimationCompletion);

            // Broadcast first card dealt in network games
            if (IsNetworkGame && IsHost)
            {
                BroadcastCardDealt(card, (byte)playerIndex, false, HandTypes.First);
            }

            card = dealer.DealCardToHand(player.SecondHand);
            AddDealAnimation(card, animatedSecondHands[playerIndex], true, DealDuration,
                DateTime.Now + animation.EstimatedTimeForAnimationCompletion +
                DealDuration);

            // Broadcast second card dealt in network games
            if (IsNetworkGame && IsHost)
            {
                BroadcastCardDealt(card, (byte)playerIndex, false, HandTypes.Second);
            }
        }

        /// <summary>
        /// Performs the "Double" move for the current player.
        /// </summary>
        public void Double()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();

            int playerIndex = players.IndexOf(player);

            // Broadcast to network in network games
            if (IsNetworkGame && IsHost)
            {
                BroadcastDoubleAction((byte)playerIndex);
            }

            switch (player.CurrentHandType)
            {
                case HandTypes.First:
                    player.Double = true;
                    float betAmount = player.BetAmount;

                    if (player.IsSplit)
                    {
                        betAmount /= 2f;
                    }

                    betGameComponent.AddChips(playerIndex, betAmount, false, false);
                    break;
                case HandTypes.Second:
                    player.SecondDouble = true;
                    if (player.Double == false)
                    {
                        // The bet is evenly spread between both hands, add one half
                        betGameComponent.AddChips(playerIndex, player.BetAmount / 2f,
                            false, true);
                    }
                    else
                    {
                        // The first hand's bet is double, add one third of the total
                        betGameComponent.AddChips(playerIndex, player.BetAmount / 3f,
                            false, true);
                    }
                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }
            Hit();
            Stand();
        }

        /// <summary>
        /// Performs the "Hit" move for the current player.
        /// </summary>
        public void Hit()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();
            if (player == null)
                return;

            int playerIndex = players.IndexOf(player);

            // Broadcast to network in network games (host only deals cards)
            if (IsNetworkGame && IsHost)
            {
                BroadcastHitAction((byte)playerIndex);
            }

            // Draw a card to the appropriate hand
            TraditionalCard card;
            switch (player.CurrentHandType)
            {
                case HandTypes.First:
                    card = dealer.DealCardToHand(player.Hand);
                    AddDealAnimation(card, animatedHands[playerIndex], true,
                        DealDuration, DateTime.Now);

                    // Broadcast card dealt in network games
                    if (IsNetworkGame && IsHost)
                    {
                        BroadcastCardDealt(card, (byte)playerIndex, false, HandTypes.First);
                    }
                    break;
                case HandTypes.Second:
                    card = dealer.DealCardToHand(player.SecondHand);
                    AddDealAnimation(card, animatedSecondHands[playerIndex], true,
                        DealDuration, DateTime.Now);

                    // Broadcast card dealt in network games
                    if (IsNetworkGame && IsHost)
                    {
                        BroadcastCardDealt(card, (byte)playerIndex, false, HandTypes.Second);
                    }
                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }

            // Auto-stand on 21 if enabled in settings
            player.CalculateValues();
            int handValue = player.CurrentHandType == HandTypes.First ? player.FirstValue : player.SecondValue;
            if (GameSettings.Instance.AutoStandOn21 && handValue == 21)
            {
                Stand();
            }
        }

        /// <summary>
        /// Performs the "Insurance" action for the current player.
        /// </summary>
        public void Insurance()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();
            if (player == null)
                return;

            int playerIndex = players.IndexOf(player);

            // Broadcast to network in network games
            if (IsNetworkGame && IsHost)
            {
                BroadcastInsuranceAction((byte)playerIndex);
            }

            player.IsInsurance = true;
            player.Balance -= player.BetAmount / 2f;
            betGameComponent.AddChips(playerIndex, player.BetAmount / 2, true, false);
            showInsurance = false;
        }

        /// <summary>
        /// Changes the visiblility of most game buttons.
        /// </summary>
        /// <param name="visible">True to make the buttons visible, false to make
        /// them invisible.</param>
        void ChangeButtonsVisiblility(bool visible)
        {
            buttons["Hit"].Visible = visible;
            buttons["Stand"].Visible = visible;
            buttons["Double"].Visible = visible;
            buttons["Split"].Visible = visible;
            buttons["Insurance"].Visible = visible;
        }

        /// <summary>
        /// Enables or disable most game buttons.
        /// </summary>
        /// <param name="enabled">True to enable the buttons , false to 
        /// disable them.</param>
        void EnableButtons(bool enabled)
        {
            buttons["Hit"].Enabled = enabled;
            buttons["Stand"].Enabled = enabled;
            buttons["Double"].Enabled = enabled;
            buttons["Split"].Enabled = enabled;
            buttons["Insurance"].Enabled = enabled;
        }

        /// <summary>
        /// Add an indication that the player has passed on the current round.
        /// </summary>
        /// <param name="indexPlayer">The player's index.</param>
        public void ShowPlayerPass(int indexPlayer)
        {
            // Add animation component
            AnimatedGameComponent passComponent = new AnimatedGameComponent(this, cardsAssets["pass"], screenManager.SpriteBatch, screenManager.GlobalTransformation)
            {
                CurrentPosition = GameTable.PlaceOrder(indexPlayer),
                Visible = false
            };
            Game.Components.Add(passComponent);

            // Hide insurance button only when the first payer passes
            Action<object> performWhenDone = null;
            if (indexPlayer == 0)
            {
                performWhenDone = HideInshurance;
            }
            // Add scale animation for the pass "card"
            passComponent.AddAnimation(new ScaleGameComponentAnimation(2.0f, 1.0f)
            {
                AnimationCycles = 1,
                PerformBeforeStart = ShowComponent,
                PerformBeforSartArgs = passComponent,
                StartTime = DateTime.Now,
                Duration = TimeSpan.FromSeconds(1),
                PerformWhenDone = performWhenDone
            });
        }

        /// <summary>
        /// Helper method to hide insurance
        /// </summary>
        /// <param name="obj"></param>
        void HideInshurance(object obj)
        {
            showInsurance = false;
        }

        /// <summary>
        /// Shows the insurance button if the first player can afford insurance.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing 
        /// the event data.</param>
        void InsuranceGameRule(object sender, EventArgs e)
        {
            BlackjackPlayer player = (BlackjackPlayer)players[0];
            if (player.Balance >= player.BetAmount / 2)
            {
                showInsurance = true;
            }
        }

        /// <summary>
        /// Shows the bust visual cue after the bust rule has been matched.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing 
        /// the event data.</param>
        void BustGameRule(object sender, EventArgs e)
        {
            showInsurance = false;
            BlackjackGameEventArgs args = (e as BlackjackGameEventArgs);
            BlackjackPlayer player = (BlackjackPlayer)args.Player;

            CueOverPlayerHand(player, "bust", args.Hand, null);

            switch (args.Hand)
            {
                case HandTypes.First:
                    player.Bust = true;

                    if (player.IsSplit && !player.SecondBlackJack)
                    {
                        player.CurrentHandType = HandTypes.Second;
                    }
                    else
                    {
                        turnFinishedByPlayer[players.IndexOf(player)] = true;
                    }
                    break;
                case HandTypes.Second:
                    player.SecondBust = true;
                    turnFinishedByPlayer[players.IndexOf(player)] = true;
                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }

            // Broadcast turn change after bust
            if (IsNetworkGame && IsHost)
            {
                Player nextPlayer = GetCurrentPlayer();
                byte nextPlayerIndex = nextPlayer != null ? (byte)players.IndexOf(nextPlayer) : (byte)255;
                BroadcastTurnChanged(nextPlayerIndex);
            }
        }

        /// <summary>
        /// Shows the blackjack visual cue after the blackjack rule has been matched.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing 
        /// the event data.</param>
        void BlackJackGameRule(object sender, EventArgs e)
        {
            showInsurance = false;
            BlackjackGameEventArgs args = (e as BlackjackGameEventArgs);
            BlackjackPlayer player = (BlackjackPlayer)args.Player;

            CueOverPlayerHand(player, "blackjack", args.Hand, null);

            switch (args.Hand)
            {
                case HandTypes.First:
                    player.BlackJack = true;

                    if (player.IsSplit)
                    {
                        player.CurrentHandType = HandTypes.Second;
                    }
                    else
                    {
                        turnFinishedByPlayer[players.IndexOf(player)] = true;
                    }
                    break;
                case HandTypes.Second:
                    player.SecondBlackJack = true;
                    if (player.CurrentHandType == HandTypes.Second)
                    {
                        turnFinishedByPlayer[players.IndexOf(player)] = true;
                    }
                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }

            // Broadcast turn change after blackjack
            if (IsNetworkGame && IsHost)
            {
                Player nextPlayer = GetCurrentPlayer();
                byte nextPlayerIndex = nextPlayer != null ? (byte)players.IndexOf(nextPlayer) : (byte)255;
                BroadcastTurnChanged(nextPlayerIndex);
            }
        }

        /// <summary>
        /// Handles the Click event of the insurance button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Insurance_Click(object sender, EventArgs e)
        {
            if (IsNetworkGame && !IsHost)
            {
                // Client sends action to host
                SendPlayerAction(Networking.BlackjackAction.Insurance);
            }
            else
            {
                // Host or local game executes action
                Insurance();
            }
            showInsurance = false;
        }

        /// <summary>
        /// Handles the Click event of the new game button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void newGame_Click(object sender, EventArgs e)
        {
            FinishTurn();
            StartRound();
            newGame.Enabled = false;
            newGame.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the hit button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Hit_Click(object sender, EventArgs e)
        {
            if (IsNetworkGame && !IsHost)
            {
                // Client sends action to host
                SendPlayerAction(Networking.BlackjackAction.Hit);
            }
            else
            {
                // Host or local game executes action
                Hit();
            }
            showInsurance = false;
        }

        /// <summary>
        /// Handles the Click event of the stand button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Stand_Click(object sender, EventArgs e)
        {
            if (IsNetworkGame && !IsHost)
            {
                // Client sends action to host
                SendPlayerAction(Networking.BlackjackAction.Stand);
            }
            else
            {
                // Host or local game executes action
                Stand();
            }
            showInsurance = false;
        }

        /// <summary>
        /// Handles the Click event of the double button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Double_Click(object sender, EventArgs e)
        {
            if (IsNetworkGame && !IsHost)
            {
                // Client sends action to host
                SendPlayerAction(Networking.BlackjackAction.Double);
            }
            else
            {
                // Host or local game executes action
                Double();
            }
            showInsurance = false;
        }

        /// <summary>
        /// Handles the Click event of the split button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Split_Click(object sender, EventArgs e)
        {
            if (IsNetworkGame && !IsHost)
            {
                // Client sends action to host
                SendPlayerAction(Networking.BlackjackAction.Split);
            }
            else
            {
                // Host or local game executes action
                Split();
            }
            showInsurance = false;
        }

        /// <summary>
        /// Handles the Click event of the back button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">>The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void backButton_Click(object sender, EventArgs e)
        {
            // Remove all unnecessary components
            for (int componentIndex = 0; componentIndex < Game.Components.Count; componentIndex++)
            {
                if (!(Game.Components[componentIndex] is ScreenManager))
                {
                    Game.Components.RemoveAt(componentIndex);
                    componentIndex--;
                }
            }

            foreach (GameScreen screen in screenManager.GetScreens())
                screen.ExitScreen();

            screenManager.AddScreen(new BackgroundScreen(), null);
            screenManager.AddScreen(new MainMenuScreen(), null);
        }

        /// <summary>
        /// Broadcasts the shuffle seed to all players in the network session.
        /// </summary>
        private void BroadcastShuffleSeed(int seed)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.ShuffleSeedPacket { Seed = seed };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.ShuffleSeed);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Handles receiving a shuffle seed from the host.
        /// </summary>
        public void ReceiveShuffleSeed(int seed)
        {
            currentShuffleSeed = seed;
            dealer.Shuffle(seed);
        }

        /// <summary>
        /// Broadcasts the complete player list (including AI players) to all clients.
        /// Should be called by the host after all players (human + AI) have been added.
        /// </summary>
        public void BroadcastPlayerList()
        {
            if (NetworkSession == null || !IsHost)
                return;

            var playerInfoList = new System.Collections.Generic.List<Networking.PlayerInfo>();
            foreach (var player in players)
            {
                playerInfoList.Add(new Networking.PlayerInfo
                {
                    Name = player.Name,
                    IsAI = player is BlackjackAIPlayer
                });
            }

            var packet = new Networking.PlayerListSyncPacket { Players = playerInfoList };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.PlayerListSync);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Broadcasts a card dealt event to all players.
        /// </summary>
        private void BroadcastCardDealt(TraditionalCard card, byte playerIndex, bool faceDown, HandTypes handType)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.CardDealtPacket
            {
                Card = card,
                PlayerIndex = playerIndex,
                FaceDown = faceDown,
                HandType = handType
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.CardDealt);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Broadcasts a bet placed event to all players.
        /// </summary>
        public void BroadcastBetPlaced(byte playerIndex, int betAmount)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.BetPlacedPacket
            {
                PlayerIndex = playerIndex,
                BetAmount = betAmount
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.BetPlaced);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Sends a bet placed event from client to host.
        /// </summary>
        public void SendBetPlaced(byte playerIndex, int betAmount)
        {
            if (NetworkSession == null || NetworkSession.LocalGamers.Count == 0)
                return;

            var packet = new Networking.BetPlacedPacket
            {
                PlayerIndex = playerIndex,
                BetAmount = betAmount
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.BetPlaced);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Broadcasts a chip added event to all players.
        /// </summary>
        public void BroadcastChipAdded(byte playerIndex, int chipValue)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.ChipAddedPacket
            {
                PlayerIndex = playerIndex,
                ChipValue = chipValue
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.ChipAdded);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Sends a chip added event from client to host.
        /// </summary>
        public void SendChipAdded(byte playerIndex, int chipValue)
        {
            if (NetworkSession == null || NetworkSession.LocalGamers.Count == 0)
                return;

            var packet = new Networking.ChipAddedPacket
            {
                PlayerIndex = playerIndex,
                ChipValue = chipValue
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.ChipAdded);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Broadcasts a turn change event.
        /// </summary>
        private void BroadcastTurnChanged(byte currentPlayerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.TurnChangedPacket { CurrentPlayerIndex = currentPlayerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.TurnChanged);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        // Phase 5: Gameplay Action Broadcasting
        private void BroadcastHitAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.HitActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.HitAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        private void BroadcastStandAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.StandActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.StandAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        private void BroadcastDoubleAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.DoubleActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.DoubleAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        private void BroadcastSplitAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.SplitActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.SplitAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        private void BroadcastInsuranceAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.InsuranceActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.InsuranceAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Sends a player action to the host.
        /// </summary>
        public void SendPlayerAction(Networking.BlackjackAction action)
        {
            if (NetworkSession == null || NetworkSession.LocalGamers.Count == 0)
                return;

            var packet = new Networking.PlayerActionPacket { Action = action };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.PlayerAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Handles a card dealt packet received from the host (client-side).
        /// Recreates the dealing action that happened on the host.
        /// </summary>
        // Track the sequence of cards dealt for proper animation timing on clients
        private int cardSequenceCounter = 0;
        private DateTime dealStartTime = DateTime.MinValue;

        public void HandleReceivedCardDealt(TraditionalCard card, byte playerIndex, bool faceDown, HandTypes handType)
        {
            // This method is called on clients when they receive a CardDealt packet from the host
            // The deck is already synchronized via the shuffle seed, so cards are dealt in the same order

            // Reset sequence counter at the start of a new deal sequence
            if (dealStartTime == DateTime.MinValue || (DateTime.Now - dealStartTime).TotalSeconds > 5)
            {
                dealStartTime = DateTime.Now;
                cardSequenceCounter = 0;
            }

            // Calculate staggered start time based on card sequence
            // This matches the timing logic in Deal() method
            DateTime startTime = dealStartTime + TimeSpan.FromSeconds(DealDuration.TotalSeconds * cardSequenceCounter);
            cardSequenceCounter++;

            if (playerIndex == 255)
            {
                // Dealer card
                var dealtCard = dealer.DealCardToHand(dealerPlayer.Hand);

                if (dealerHandComponent != null)
                {
                    AddDealAnimation(dealtCard, dealerHandComponent, faceDown, DealDuration, startTime);
                }
                else
                {
                    System.Console.WriteLine($"[CardDealt] Warning: dealerHandComponent is null, skipping animation");
                }
            }
            else if (playerIndex < players.Count)
            {
                // Player card
                var player = (BlackjackPlayer)players[playerIndex];
                Hand targetHand = (handType == HandTypes.First) ? player.Hand : player.SecondHand;

                var dealtCard = dealer.DealCardToHand(targetHand);

                // Only add animation if the animated hand component exists for this player
                if (animatedHands != null && playerIndex < animatedHands.Length && animatedHands[playerIndex] != null)
                {
                    AddDealAnimation(dealtCard, animatedHands[playerIndex], !faceDown, DealDuration, startTime);
                }
                else
                {
                    System.Console.WriteLine($"[CardDealt] Warning: animatedHands[{playerIndex}] is null, skipping animation");
                }
            }
        }

        /// <summary>
        /// Handles a bet placed packet received from the host (client-side).
        /// Updates the player's bet state to match the host.
        /// </summary>
        public void HandleReceivedBetPlaced(byte playerIndex, int betAmount)
        {
            // This method is called on clients when they receive a BetPlaced packet from the host
            if (playerIndex < players.Count)
            {
                var player = (BlackjackPlayer)players[playerIndex];

                if (betAmount == 0)
                {
                    // Player passed
                    ShowPlayerPass(playerIndex);
                }
                else
                {
                    // Apply the bet (this updates balance and bet amount)
                    player.Bet(betAmount);
                }

                player.IsDoneBetting = true;
            }
        }

        /// <summary>
        /// Handles a chip added packet received over the network.
        /// Adds the chip with animation on the receiving machine.
        /// </summary>
        public void HandleReceivedChipAdded(byte playerIndex, int chipValue)
        {
            // This method is called when receiving a ChipAdded packet
            // We add the chip with animation but don't re-send to network (sendToNetwork: false)
            if (playerIndex < players.Count && betGameComponent != null)
            {
                // Add the visual chip component with animation
                // The sendToNetwork parameter is false to prevent infinite loop
                betGameComponent.AddChip(playerIndex, chipValue, false, sendToNetwork: false);
            }
        }

        // Phase 5: Gameplay Action Handlers
        /// <summary>
        /// Handles a Hit action received from the network.
        /// Executes the Hit move for the specified player.
        /// NOTE: Does NOT deal cards - cards are dealt via CardDealt packets.
        /// </summary>
        public void HandleReceivedHitAction(byte playerIndex)
        {
            // This handler is for game state synchronization only
            // The actual card dealing is handled by CardDealt packets
            // which are sent separately by the host

            // No action needed here - the CardDealt packet will handle the card
        }

        /// <summary>
        /// Handles a Stand action received from the network.
        /// Executes the Stand move for the specified player.
        /// </summary>
        public void HandleReceivedStandAction(byte playerIndex)
        {
            if (playerIndex >= players.Count)
                return;

            var player = (BlackjackPlayer)players[playerIndex];

            // Execute Stand logic
            if (player.IsSplit == false)
            {
                turnFinishedByPlayer[playerIndex] = true;
            }
            else
            {
                switch (player.CurrentHandType)
                {
                    case HandTypes.First:
                        if (player.SecondBlackJack)
                        {
                            turnFinishedByPlayer[playerIndex] = true;
                        }
                        else
                        {
                            player.CurrentHandType = HandTypes.Second;
                        }
                        break;
                    case HandTypes.Second:
                        turnFinishedByPlayer[playerIndex] = true;
                        break;
                    default:
                        throw new System.Exception("Player has an unsupported hand type.");
                }
            }
        }

        /// <summary>
        /// Handles a Double action received from the network.
        /// Executes the Double move for the specified player.
        /// NOTE: Does NOT deal cards - cards are dealt via CardDealt packets.
        /// </summary>
        public void HandleReceivedDoubleAction(byte playerIndex)
        {
            if (playerIndex >= players.Count)
                return;

            var player = (BlackjackPlayer)players[playerIndex];

            // Execute Double logic - update chip stacks and flags
            switch (player.CurrentHandType)
            {
                case HandTypes.First:
                    player.Double = true;
                    betGameComponent.AddChips(playerIndex, player.BetAmount, false, false);
                    break;
                case HandTypes.Second:
                    player.SecondDouble = true;
                    if (player.Double == false)
                    {
                        betGameComponent.AddChips(playerIndex, player.BetAmount / 2f, false, true);
                    }
                    else
                    {
                        betGameComponent.AddChips(playerIndex, player.BetAmount / 3f, false, true);
                    }
                    break;
                default:
                    throw new System.Exception("Player has an unsupported hand type.");
            }

            // Update turn state (card dealing is handled by CardDealt packet)
            // Automatically stand after double
            if (player.IsSplit == false)
            {
                turnFinishedByPlayer[playerIndex] = true;
            }
            else
            {
                switch (player.CurrentHandType)
                {
                    case HandTypes.First:
                        if (player.SecondBlackJack)
                        {
                            turnFinishedByPlayer[playerIndex] = true;
                        }
                        else
                        {
                            player.CurrentHandType = HandTypes.Second;
                        }
                        break;
                    case HandTypes.Second:
                        turnFinishedByPlayer[playerIndex] = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Handles a Split action received from the network.
        /// Executes the Split move for the specified player.
        /// NOTE: Does NOT deal new cards - cards are dealt via CardDealt packets.
        /// </summary>
        public void HandleReceivedSplitAction(byte playerIndex)
        {
            if (playerIndex >= players.Count)
                return;

            var player = (BlackjackPlayer)players[playerIndex];

            player.InitializeSecondHand();

            Vector2 sourcePosition = animatedHands[playerIndex].GetCardGameComponent(1).CurrentPosition;
            Vector2 targetPosition = animatedHands[playerIndex].GetCardGameComponent(0).CurrentPosition +
                secondHandOffset;

            var animation = new TransitionGameComponentAnimation(sourcePosition, targetPosition)
            {
                StartTime = DateTime.Now,
                Duration = TimeSpan.FromSeconds(0.5f)
            };

            player.SplitHand();

            betGameComponent.AddChips(playerIndex, player.BetAmount, false, true);

            animatedSecondHands[playerIndex] =
                new BlackjackAnimatedPlayerHandComponent(playerIndex, secondHandOffset,
                    player.SecondHand, this, screenManager.SpriteBatch, screenManager.GlobalTransformation);
            Game.Components.Add(animatedSecondHands[playerIndex]);

            AnimatedCardsGameComponent animatedGameComponent = animatedSecondHands[playerIndex].GetCardGameComponent(0);
            animatedGameComponent.IsFaceDown = false;
            animatedGameComponent.AddAnimation(animation);

            // Note: Additional cards will be dealt via CardDealt packets from the host
        }

        /// <summary>
        /// Handles an Insurance action received from the network.
        /// Executes the Insurance move for the specified player.
        /// </summary>
        public void HandleReceivedInsuranceAction(byte playerIndex)
        {
            if (playerIndex >= players.Count)
                return;

            var player = (BlackjackPlayer)players[playerIndex];

            player.IsInsurance = true;
            player.Balance -= player.BetAmount / 2f;
            betGameComponent.AddChips(playerIndex, player.BetAmount / 2, true, false);
            showInsurance = false;
        }

        /// <summary>
        /// Handles a turn changed notification received from the network.
        /// Updates UI and button state to reflect the new active player.
        /// </summary>
        public void HandleReceivedTurnChanged(byte currentPlayerIndex)
        {
            // Value 255 indicates no active player (all players finished)
            if (currentPlayerIndex == 255)
            {
                System.Console.WriteLine("[TurnChanged] All players have finished their turns");
                return;
            }

            if (currentPlayerIndex >= players.Count)
            {
                System.Console.WriteLine($"[TurnChanged] Invalid player index: {currentPlayerIndex}");
                return;
            }

            var currentPlayer = (BlackjackPlayer)players[currentPlayerIndex];
            System.Console.WriteLine($"[TurnChanged] Turn changed to player {currentPlayerIndex}: {currentPlayer.Name}");

            // The button availability will be updated in the next Update() cycle
            // via SetButtonAvailability(), which checks GetCurrentPlayer()
        }
    }
}