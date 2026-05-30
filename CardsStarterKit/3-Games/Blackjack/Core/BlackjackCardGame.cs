//-----------------------------------------------------------------------------
// BlackjackGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using System.Threading;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

using System.Reflection;
using System.IO;

namespace Blackjack
{
    partial class BlackjackCardGame : CardsGame
    {
        public Microsoft.Xna.Framework.Net.NetworkSession NetworkSession { get; set; }
        public bool IsNetworkGame { get; set; }
        public bool IsHost { get; set; }

        // Public accessor for the players list (needed for network synchronization)
        public System.Collections.Generic.List<CardsFramework.Player> Players => players;

        private int currentShuffleSeed;
        private static readonly Random random = new();

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

        public int LocalPlayerIndex { get; set; } = -1;

        // Track the dealer deck cards so they aren't removed during hand cleanup
        private List<AnimatedCardsGameComponent> dealerDeckCards = new List<AnimatedCardsGameComponent>();
        Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        Button dealButton;
        Button clearButton;
        Button newGame;
        Button backButton;
        bool showInsurance;
        bool balanceAnimationsStarted = false;

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
            : base(2, 0, CardSuit.AllSuits, CardsFramework.CardValue.NonJokers,
                BlackjackConstants.MinPlayers, BlackjackConstants.MaxPlayers, new BlackJackTable(
                    UIConstants.GetRingOffset(screenManager.SafeArea.Height), tableBounds,
                    dealerPosition, BlackjackConstants.MaxPlayers, placeOrder, theme, screenManager.Game,
                    screenManager.SpriteBatch, screenManager.GlobalTransformation),
                theme, screenManager.Game)
        {
            dealerPlayer = new BlackjackPlayer("Dealer", this);
            turnFinishedByPlayer = new bool[MaximumPlayers];
            this.screenManager = screenManager;

            // Calculate proportional UI sizes based on screen dimensions
            secondHandOffset =
                UIConstants.GetSecondHandOffset(screenManager.SafeArea.Width, screenManager.SafeArea.Height);
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
        /// Perform the game's update logic.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to 
        /// this method.</param>
        public void Update(GameTime gameTime)
        {
            // Track total game time for animation timing
            currentGameTime += gameTime.ElapsedGameTime;

            switch (State)
            {
                case BlackjackGameState.Shuffling:
                {
                    ShowShuffleAnimation();
                }
                    break;
                case BlackjackGameState.Betting:
                {
                    ChangeButtonsEnablement(false);
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

                        // If the current player is an NPC player, make it play
                        if (player is BlackjackNPCPlayer NPCPlayer)
                        {
                            if (!IsNetworkGame || IsHost)
                            {
                                NPCPlayer.NPCPlay();
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
                        ChangeButtonsEnablement(false);
                }
                    break;
                case BlackjackGameState.RoundEnd:
                {
                    if (dealerHandComponent.EstimatedTimeForAnimationsCompletion() == TimeSpan.Zero)
                    {
                        // Start chip animations only once
                        if (!balanceAnimationsStarted)
                        {
                            balanceAnimationsStarted = true;
                            betGameComponent.CalculateBalanceWithAnimations(dealerPlayer, () =>
                            {
                                // Callback when all animations complete
                                balanceAnimationsStarted = false;

                                // Check if there is enough money to play
                                // then show new game option or tell the player he has lost
                                int localIdx = LocalPlayerIndex >= 0 ? LocalPlayerIndex : 0;
                                if (((BlackjackPlayer)players[localIdx]).Balance < 5)
                                {
                                    EndGame();
                                }
                                else
                                {
                                    // Hide all gameplay buttons before showing "New Hand" button
                                    foreach (var btn in buttons.Values)
                                    {
                                        btn.Visible = false;
                                        btn.Enabled = false;
                                    }

                                    newGame.Enabled = true;
                                    newGame.Visible = true;

                                    // Ensure "New Hand" button is centered
                                    LayoutVisibleButtons();
                                }
                            });
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
            // Hide dealer deck cards during shuffle (they'll be part of the shuffle animation)
            foreach (var deckCard in dealerDeckCards)
            {
                deckCard.Visible = false;
            }

            dealerDeckCards.Clear();

            // Create a list of cards for the shuffle animation (only show a subset for performance)
            // Using 52 cards (one deck) is enough for a good visual effect
            var deckCards = new List<TraditionalCard>();
            int cardsToShow = Math.Min(52, dealer.Count); // Show max 52 cards
            for (int i = 0; i < cardsToShow; i++)
            {
                deckCards.Add(dealer[i]);
            }

            // Calculate shuffle position in top-center (where shuffle visibly happens)
            Rectangle tableBounds = GameTable.TableBounds;
            int cardWidth = UIConstants.GetCardWidth(screenManager.SafeArea.Width);
            int cardHeight = UIConstants.GetCardHeight(screenManager.SafeArea.Height);
            float shuffleCenterX = tableBounds.Left + (tableBounds.Width / 2f);
            float shuffleY = tableBounds.Top + 40;
            Vector2 shufflePosition = new Vector2(shuffleCenterX, shuffleY);

            // Calculate final deck position (top-right where deck sits for dealing)
            float finalDeckX = tableBounds.Right - cardWidth - 65;
            float finalDeckY = tableBounds.Top + 15;
            Vector2 finalDeckPosition = new Vector2(finalDeckX, finalDeckY);

            // Get scaled card size and shuffle parameters
            Vector2 cardSize = UIConstants.GetCardSize(screenManager.SafeArea.Width, screenManager.SafeArea.Height);
            float splitDistance = UIConstants.GetShuffleSplitDistance(screenManager.SafeArea.Width);
            float cascadeHeight = UIConstants.GetShuffleCascadeHeight(screenManager.SafeArea.Height);

            // Create a riffle shuffle animation at top-center
            var shuffleAnimation = new RiffleShuffleAnimation(
                    this,
                    shufflePosition, // Shuffle happens at top-center
                    TimeSpan.FromSeconds(1.0 * AnimationSpeedMultiplier),
                    cardSize) // Scaled card size
                {
                    SplitDistance = splitDistance, // Scaled split distance
                    CascadeHeight = cascadeHeight // Scaled cascade height
                };

            // Set up callbacks
            shuffleAnimation.OnAnimationComplete = () =>
            {
                AudioManager.PlaySound("Shuffle");

                // Animate 4 cards from center to dealer deck position (top-right)
                // This creates a nice visual transition instead of the deck magically appearing
                AnimateDeckToDealerPosition(shufflePosition, finalDeckPosition, deckCards);

                // Transition to betting state
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
        /// Animates 4 cards from the shuffle center position to the dealer deck position.
        /// This creates a visual transition instead of the deck magically appearing.
        /// </summary>
        /// <param name="shufflePosition">Starting position (center of table)</param>
        /// <param name="dealerPosition">Ending position (top-right dealer deck)</param>
        /// <param name="deckCards">The deck of cards to animate from</param>
        private void AnimateDeckToDealerPosition(Vector2 shufflePosition, Vector2 dealerPosition,
            List<TraditionalCard> deckCards)
        {
            // Clear previous dealer deck cards
            dealerDeckCards.Clear();

            // Use 4 cards to represent the deck
            int cardsToAnimate = Math.Min(4, deckCards.Count);
            TimeSpan duration = TimeSpan.FromSeconds(0.6 * AnimationSpeedMultiplier);
            float dealerRotation = MathHelper.ToRadians(-47f); // Match the dealer deck's rotation

            for (int i = 0; i < cardsToAnimate; i++)
            {
                TraditionalCard card = deckCards[i];

                // Create an animated card component
                var animatedCard = new AnimatedCardsGameComponent(
                    card,
                    this,
                    screenManager.SpriteBatch,
                    screenManager.GlobalTransformation);

                // Position at shuffle center with slight offset for stacking
                float stackOffsetX = i * 2f;
                float stackOffsetY = i * 2f;
                animatedCard.CurrentPosition = shufflePosition + new Vector2(stackOffsetX, stackOffsetY);
                animatedCard.CurrentRotation = 0f; // Start vertical
                animatedCard.Visible = true;

                // Add to game components
                Game.Components.Add(animatedCard);

                // Track this as a dealer deck card so it won't be removed during hand cleanup
                dealerDeckCards.Add(animatedCard);

                // Create transition animation with the swooping effect
                var transitionAnim = new TransitionGameComponentAnimation(
                    animatedCard.CurrentPosition,
                    dealerPosition + new Vector2(stackOffsetX, stackOffsetY))
                {
                    Duration = duration
                };

                // Add animation immediately - all 4 cards animate together
                // The slight stacking offset creates a cascade visual effect
                animatedCard.AddAnimation(transitionAnim);

                // Add a rotation animation to rotate to match dealer deck angle
                var rotationAnim = new RotationGameComponentAnimation(0f, dealerRotation)
                {
                    Duration = duration
                };
                animatedCard.AddAnimation(rotationAnim);
            }
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
                                TimeSpan.FromSeconds(
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
                    AddDealAnimation(card, dealerHandComponent, isHoleCard, DealDuration, TimeSpan.Zero);

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
                        Debug.WriteLine(
                            $"[StartPlaying] Warning: animatedHands[{playerIndex}] is null for player {players[playerIndex].Name}");
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
        /// <param name="startDelay">The delay before the animation should start.</param>
        public void AddDealAnimation(TraditionalCard card, AnimatedHandGameComponent
            animatedHand, bool flipCard, TimeSpan duration, TimeSpan startDelay)
        {
            // Safety check: if animatedHand is null, we can't create animations
            if (animatedHand == null)
            {
                Debug.WriteLine($"[AddDealAnimation] ERROR: animatedHand parameter is null! Cannot create animation.");
                return;
            }

            // Get the card location and card component
            int cardLocationInHand = animatedHand.GetCardLocationInHand(card);
            AnimatedCardsGameComponent cardComponent = animatedHand.GetCardGameComponent(cardLocationInHand);

            // Cards are dealt from the deck position in the top-right corner
            Rectangle tableBounds = GameTable.TableBounds;
            int cardWidth = UIConstants.GetCardWidth(screenManager.SafeArea.Width);
            int cardHeight = UIConstants.GetCardHeight(screenManager.SafeArea.Height);
            float deckX = tableBounds.Right - cardWidth - 40; // Match deck display position
            float deckY = tableBounds.Top + 40;
            Vector2 deckPosition = new Vector2(deckX, deckY);

            var cardAnimation = new TransitionGameComponentAnimation(deckPosition,
                animatedHand.CurrentPosition +
                animatedHand.GetCardRelativePosition(cardLocationInHand))
            {
                StartDelay = startDelay,
                PerformBeforeStart = ShowCardAndPlayDealSound,
                PerformBeforeStartArgs = new object[] { cardComponent, animatedHand }
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
                    StartDelay = startDelay + duration,
                    PerformWhenDone = PlayFlipSound
                });
            }
        }

        /// <summary>
        /// Helper method to show card component and play deal sound with contextual pitch/volume/panning.
        /// Called when card animation starts.
        /// </summary>
        /// <param name="obj">Array containing [cardComponent, animatedHand]</param>
        void ShowCardAndPlayDealSound(object obj)
        {
            var args = (object[])obj;
            var cardComponent = (AnimatedCardsGameComponent)args[0];
            var animatedHand = (AnimatedHandGameComponent)args[1];

            cardComponent.Visible = true;

            // Determine who is receiving the card
            float pitch = 0f;
            float volumeMultiplier = 1.0f;

            if (animatedHand == dealerHandComponent)
            {
                // Dealer dealing to itself: default pitch and volume
                pitch = 0f;
                volumeMultiplier = 1.0f;
            }
            else
            {
                // Find which player is receiving the card
                int playerIndex = -1;
                for (int i = 0; i < animatedHands.Length; i++)
                {
                    if (animatedHands[i] == animatedHand || animatedSecondHands[i] == animatedHand)
                    {
                        playerIndex = i;
                        break;
                    }
                }

                if (playerIndex == LocalPlayerIndex)
                {
                    // Human player: higher pitch and louder
                    pitch = (float)(random.NextDouble() * 0.15 + 0.15); // Range: 0.15 to 0.30
                    volumeMultiplier = 1.15f;
                }
                else if (playerIndex >= 0)
                {
                    // NPC player: slightly higher pitch and medium volume
                    pitch = (float)(random.NextDouble() * 0.10 + 0.05); // Range: 0.05 to 0.15
                    volumeMultiplier = 1.08f;
                }
            }

            // Calculate stereo panning based on card's X position on screen
            // Get the target position where the card is heading
            Vector2 targetPosition = animatedHand.CurrentPosition;
            float screenCenterX = screenManager.SafeArea.Width / 2f;

            // Calculate pan: -1.0 (left) to 1.0 (right), with 0.0 at screen center
            // We'll use a moderate panning range to keep it subtle
            float pan = (targetPosition.X - screenCenterX) / screenCenterX;
            pan = MathHelper.Clamp(pan * 0.7f, -0.7f, 0.7f); // Scale to 70% max for subtlety

            // Calculate final volume based on settings
            float volume = GameSettings.Instance.SoundVolume * volumeMultiplier;
            volume = MathHelper.Clamp(volume, 0f, 1f);

            AudioManager.PlaySound("Deal", pitch: pitch, volume: volume, pan: pan);
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
        /// Helper method to play card removal sound when cards leave the table.
        /// Called when cards are animated off-screen at the end of a round.
        /// </summary>
        /// <param name="obj"></param>
        void PlayCardRemovalSound(object obj)
        {
            AudioManager.PlaySound("CardRemoval", pitch: (float)(random.NextDouble() * 0.1 - 0.05)); // Slight pitch variation
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
            int humanIndex = LocalPlayerIndex >= 0 ? LocalPlayerIndex : 0;
            if (players.IndexOf(player) == humanIndex &&
                (assetName == "win" || assetName == "blackjack"))
            {
                 AudioManager.PlaySound("Win");
            }

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
                        // CurrentPosition already includes the hand's offset, so don't add secondHandOffset again
                        currentPosition = currentAnimatedHand.CurrentPosition;
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
                new AnimatedGameComponent(this, cardsAssets[assetName], screenManager.SpriteBatch,
                    screenManager.GlobalTransformation)
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
                StartDelay = estimatedTimeToCompleteAnimations,
                Duration = TimeSpan.FromSeconds(1f * AnimationSpeedMultiplier),
                PerformBeforeStart = ShowComponent,
                PerformBeforeStartArgs = animationComponent
            });
        }

        /// <summary>
        /// Starts a new game round.
        /// </summary>
        public void StartRound()
        {
            playerHandValueTexts.Clear();

            // Reset card dealing sequence tracking for network synchronization
            cardSequenceCounter = 0;
            lastDealTime = TimeSpan.FromSeconds(-10);

            // Check if we need to shuffle: either cards are low (<20) OR dealer has never been shuffled
            // (A freshly created dealer is sequential and needs initial shuffle)
            bool needsShuffle = dealer.Count < 20 || !dealer.HasBeenShuffled;

            if (needsShuffle)
            {
                // Only reinitialize with new decks if we're actually low on cards
                // Don't reinitialize if this is just the first shuffle of a fresh deck
                if (dealer.Count < 20)
                {
                    ReinitializeDealerWithDynamicDeckCount();
                }

                AudioManager.PlaySound("Shuffle");

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
            }

            DisplayPlayingHands();
            State = needsShuffle ? BlackjackGameState.Shuffling : BlackjackGameState.Betting;
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
            animationComponent = new AnimatedGameComponent(this, texture, screenManager.SpriteBatch,
                screenManager.GlobalTransformation)
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
            // Remove all unnecessary components EXCEPT dealer deck cards
            for (int componentIndex = 0; componentIndex < Game.Components.Count; componentIndex++)
            {
                if (!(Game.Components[componentIndex] is GameTable ||
                      Game.Components[componentIndex] is BlackjackCardGame ||
                      Game.Components[componentIndex] is BetGameComponent ||
                      Game.Components[componentIndex] is Button ||
                      Game.Components[componentIndex] is ScreenManager))
                {
                    if (Game.Components[componentIndex] is AnimatedCardsGameComponent)
                    {
                        AnimatedCardsGameComponent animatedCard =
                            (Game.Components[componentIndex] as AnimatedCardsGameComponent);

                        // Skip dealer deck cards - they should remain visible
                        if (dealerDeckCards.Contains(animatedCard))
                        {
                            continue;
                        }

                        animatedCard.AddAnimation(
                            new TransitionGameComponentAnimation(animatedCard.CurrentPosition,
                                new Vector2(animatedCard.CurrentPosition.X, ScreenManager.BASE_BUFFER_HEIGHT))
                            {
                                Duration = TimeSpan.FromSeconds(0.40 * AnimationSpeedMultiplier),
                                PerformBeforeStart = PlayCardRemovalSound,
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
        /// Add an indication that the player has passed on the current round.
        /// </summary>
        /// <param name="indexPlayer">The player's index.</param>
        public void ShowPlayerPass(int indexPlayer)
        {
            // Add animation component
            AnimatedGameComponent passComponent = new AnimatedGameComponent(this, cardsAssets["pass"],
                screenManager.SpriteBatch, screenManager.GlobalTransformation)
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

            // Wrap PerformWhenDone: clear CurrentDestination so the card renders via
            // CardDrawScaleMultiplier (1.25×) instead of the 1.0× destination rectangle
            // left behind by ScaleGameComponentAnimation.
            Action<object> capturedWhenDone = performWhenDone;
            Action<object> whenDone = (obj) =>
            {
                passComponent.CurrentDestination = null;
                capturedWhenDone?.Invoke(obj);
            };

            // Add scale animation for the pass "card"
            passComponent.AddAnimation(new ScaleGameComponentAnimation(2.0f, 1.0f)
            {
                AnimationCycles = 1,
                PerformBeforeStart = ShowComponent,
                PerformBeforeStartArgs = passComponent,
                StartDelay = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(1 * AnimationSpeedMultiplier),
                PerformWhenDone = whenDone
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
            BroadcastCurrentTurnChanged();
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
            BroadcastCurrentTurnChanged();
        }

    }
}
