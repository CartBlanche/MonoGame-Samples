# Tutorial: Building a 9-Card Magic Trick Game

## Overview

In this tutorial, you'll learn how to extend the CardsStarterKit framework to create a simple interactive magic trick called the "9-Card Mind Reader." This trick teaches you the fundamentals of:

- Extending the `CardsGame` base class
- Creating custom game rules with `GameRule`
- Managing game state machines
- Working with card animations
- Creating interactive UI with buttons
- Handling single-player gameplay

**Target Audience:** MonoGame developers new to card games

**Estimated Time:** 2-3 hours

**Difficulty:** Beginner

---

## The Magic Trick Explained

### How It Works

The 9-Card Mind Reader is a classic mathematical card trick:

1. **Setup:** Lay out 9 cards face-up in a 3x3 grid
2. **Selection:** The spectator (player) mentally selects one card
3. **Reveal Phase 1:** The magician picks up the cards in 3 columns and asks "Which pile is your card in?"
4. **Rearrange:** The magician places the selected pile in the middle and lays out the cards again in 3 columns
5. **Reveal Phase 2:** The magician asks again "Which pile?"
6. **Final Reveal:** The magician dramatically reveals the middle card - which is always the selected card!

### The Secret

By placing the selected pile in the middle position twice, the card always ends up in the center position (5th card). Simple mathematics makes it foolproof!

---

## Part 1: Project Structure Setup

### Step 1.1: Create the Directory Structure

We'll organize our magic trick code similarly to the Blackjack implementation:

```
Core/Game/MagicTrick/
├── Game/
│   ├── MagicTrickCardGame.cs
│   ├── MagicTrickGameState.cs
├── Players/
│   └── MagicTrickPlayer.cs
├── Rules/
│   ├── CardSelectionRule.cs
│   └── RevealRule.cs
└── UI/
    └── (We'll use existing Button class)
```

Create these directories:

```bash
mkdir -p Core/Game/MagicTrick/Game
mkdir -p Core/Game/MagicTrick/Players
mkdir -p Core/Game/MagicTrick/Rules
mkdir -p Core/Game/MagicTrick/UI
```

---

## Part 2: Define Game State

### Step 2.1: Create the Game State Enum

The magic trick has distinct phases, so we'll use a state machine to control flow.

**Create:** `Core/Game/MagicTrick/Game/MagicTrickGameState.cs`

```csharp
namespace CardsFramework.MagicTrick
{
    /// <summary>
    /// Represents the various states of the magic trick game
    /// </summary>
    public enum MagicTrickGameState
    {
        /// <summary>
        /// Initial setup - dealing 9 cards
        /// </summary>
        Dealing,

        /// <summary>
        /// Player is selecting a card mentally
        /// </summary>
        PlayerSelecting,

        /// <summary>
        /// First pile selection phase
        /// </summary>
        FirstPileSelection,

        /// <summary>
        /// Cards being rearranged after first selection
        /// </summary>
        FirstRearrange,

        /// <summary>
        /// Second pile selection phase
        /// </summary>
        SecondPileSelection,

        /// <summary>
        /// Final rearrangement
        /// </summary>
        SecondRearrange,

        /// <summary>
        /// Revealing the selected card
        /// </summary>
        Revealing,

        /// <summary>
        /// Trick complete - show result
        /// </summary>
        Complete
    }
}
```

**Why This Approach?**
- Each state represents a distinct phase of the trick
- Makes the game loop clear and maintainable
- Easy to add animations and UI changes per state
- Follows the same pattern as BlackjackGameState

---

## Part 3: Create the Player Class

### Step 3.1: Define MagicTrickPlayer

Our player needs minimal state - just tracking which pile they selected.

**Create:** `Core/Game/MagicTrick/Players/MagicTrickPlayer.cs`

```csharp
using CardsFramework.Players;
using CardsFramework.Game;

namespace CardsFramework.MagicTrick
{
    /// <summary>
    /// Represents the spectator in the magic trick
    /// </summary>
    public class MagicTrickPlayer : Player
    {
        /// <summary>
        /// Which pile (0, 1, or 2) the player selected in the current round
        /// </summary>
        public int SelectedPile { get; set; }

        /// <summary>
        /// Whether the player has made their selection
        /// </summary>
        public bool HasSelected { get; set; }

        /// <summary>
        /// Creates a new magic trick player
        /// </summary>
        /// <param name="name">Player's name</param>
        /// <param name="game">Reference to the game instance</param>
        public MagicTrickPlayer(string name, CardsGame game)
            : base(name, game)
        {
            SelectedPile = -1;
            HasSelected = false;
        }

        /// <summary>
        /// Resets the player state for a new trick
        /// </summary>
        public void ResetSelection()
        {
            SelectedPile = -1;
            HasSelected = false;
        }
    }
}
```

**Key Points:**
- Extends `Player` base class (gives us Name, Game, Hand)
- `SelectedPile`: Tracks which of the 3 piles contains their card (0=left, 1=middle, 2=right)
- `HasSelected`: Simple flag to prevent double-selection
- `ResetSelection()`: Prepares for a new trick

---

## Part 4: Create Game Rules

### Step 4.1: Card Selection Rule

This rule checks if the player has made their pile selection and triggers the next phase.

**Create:** `Core/Game/MagicTrick/Rules/CardSelectionRule.cs`

```csharp
using System;
using CardsFramework.Rules;

namespace CardsFramework.MagicTrick
{
    /// <summary>
    /// Event arguments for card selection events
    /// </summary>
    public class CardSelectionEventArgs : EventArgs
    {
        public MagicTrickPlayer Player { get; set; }
        public int SelectedPile { get; set; }
    }

    /// <summary>
    /// Rule that fires when the player selects a pile
    /// </summary>
    public class CardSelectionRule : GameRule
    {
        private readonly MagicTrickPlayer player;
        private bool previousHasSelected;

        public CardSelectionRule(MagicTrickPlayer player)
        {
            this.player = player;
            this.previousHasSelected = false;
        }

        /// <summary>
        /// Checks if the player has made a new selection
        /// </summary>
        public override void Check()
        {
            // Only fire event when selection changes from false to true
            if (player.HasSelected && !previousHasSelected)
            {
                previousHasSelected = true;

                // Fire the event
                FireRuleMatch(new CardSelectionEventArgs
                {
                    Player = player,
                    SelectedPile = player.SelectedPile
                });
            }

            // Reset tracking when starting a new selection phase
            if (!player.HasSelected)
            {
                previousHasSelected = false;
            }
        }
    }
}
```

**How It Works:**
- Inherits from `GameRule` base class
- Tracks previous state to detect state changes
- Fires `RuleMatch` event only when selection transitions from false → true
- Resets when player starts a new selection phase
- Event includes which pile was selected

### Step 4.2: Reveal Rule

This rule determines when we're ready to reveal the selected card.

**Create:** `Core/Game/MagicTrick/Rules/RevealRule.cs`

```csharp
using System;
using CardsFramework.Rules;
using CardsFramework.Cards;

namespace CardsFramework.MagicTrick
{
    /// <summary>
    /// Event arguments for reveal events
    /// </summary>
    public class RevealEventArgs : EventArgs
    {
        public TraditionalCard RevealedCard { get; set; }
    }

    /// <summary>
    /// Rule that fires when it's time to reveal the selected card
    /// </summary>
    public class RevealRule : GameRule
    {
        private readonly MagicTrickCardGame game;
        private bool hasRevealed;

        public RevealRule(MagicTrickCardGame game)
        {
            this.game = game;
            this.hasRevealed = false;
        }

        /// <summary>
        /// Checks if we're in the revealing state
        /// </summary>
        public override void Check()
        {
            if (game.State == MagicTrickGameState.Revealing && !hasRevealed)
            {
                hasRevealed = true;

                // The 5th card (index 4) is always the selected card after two rounds
                if (game.TableCards.Count >= 5)
                {
                    FireRuleMatch(new RevealEventArgs
                    {
                        RevealedCard = game.TableCards[4]
                    });
                }
            }

            // Reset for next trick
            if (game.State == MagicTrickGameState.Dealing)
            {
                hasRevealed = false;
            }
        }
    }
}
```

**Key Points:**
- Fires when game enters `Revealing` state
- The magic happens here: `TableCards[4]` is always at index 4 after two rearrangements
- One-shot rule: Only fires once until reset
- Passes the revealed card to event handlers

---

## Part 5: Create the Main Game Class

### Step 5.1: MagicTrickCardGame - Part 1 (Fields and Constructor)

This is the core of our implementation. We'll build it in sections.

**Create:** `Core/Game/MagicTrick/Game/MagicTrickCardGame.cs`

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CardsFramework.Cards;
using CardsFramework.Game;
using CardsFramework.Players;
using CardsFramework.UI;
using CardsFramework.Rules;
using GameStateManagement;

namespace CardsFramework.MagicTrick
{
    /// <summary>
    /// Main game class for the 9-card magic trick
    /// </summary>
    public class MagicTrickCardGame : CardsGame
    {
        #region Fields

        // Game state
        private MagicTrickGameState currentState;
        public MagicTrickGameState State
        {
            get { return currentState; }
            set { currentState = value; }
        }

        // The 9 cards currently on the table
        public List<TraditionalCard> TableCards { get; private set; }

        // Animated components for displaying cards in 3x3 grid
        private List<AnimatedCardsGameComponent> animatedCards;

        // UI Components
        private Button buttonPile1;
        private Button buttonPile2;
        private Button buttonPile3;
        private Button buttonContinue;
        private Button buttonNewTrick;

        // The player (spectator)
        private MagicTrickPlayer player;

        // Game rules
        private CardSelectionRule cardSelectionRule;
        private RevealRule revealRule;

        // Display text for instructions
        private string instructionText;
        private Vector2 instructionPosition;
        private SpriteFont instructionFont;

        // Card layout constants
        private const int CardsPerRow = 3;
        private const int TotalCards = 9;
        private const float CardSpacingX = 120f;
        private const float CardSpacingY = 160f;
        private Vector2 gridStartPosition;

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new magic trick game
        /// </summary>
        /// <param name="gameTable">The game table for layout</param>
        public MagicTrickCardGame(GameTable gameTable)
            : base(
                decks: 1,                           // Only need 1 deck
                jokersInDeck: 0,                    // No jokers
                suits: CardSuit.AllSuits,           // All suits available
                cardValues: CardValue.NonJokers,    // Standard cards only
                minimumPlayers: 1,                  // Single player
                maximumPlayers: 1,                  // Single player
                gameTable: gameTable,
                theme: "Default")
        {
            TableCards = new List<TraditionalCard>();
            animatedCards = new List<AnimatedCardsGameComponent>();
            instructionText = "";
        }

        /// <summary>
        /// Initializes the game components
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // Calculate grid start position (centered on screen)
            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;

            gridStartPosition = new Vector2(
                (screenWidth - (CardSpacingX * (CardsPerRow - 1))) / 2 - 50,
                100
            );

            instructionPosition = new Vector2(screenWidth / 2, 50);

            // Start with dealing state
            currentState = MagicTrickGameState.Dealing;
        }

        /// <summary>
        /// Loads content and creates UI components
        /// </summary>
        public override void LoadContent()
        {
            base.LoadContent();

            // Load instruction font
            instructionFont = Game.Content.Load<SpriteFont>(@"Fonts\Regular");

            // Create buttons for pile selection
            Texture2D buttonTexture = Game.Content.Load<Texture2D>(@"Images\UI\ButtonRegular");
            Texture2D buttonPressedTexture = Game.Content.Load<Texture2D>(@"Images\UI\ButtonPressed");

            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;
            int buttonWidth = 200;
            int buttonHeight = 60;
            int buttonY = screenHeight - 250;

            // Pile 1 button (left)
            buttonPile1 = new Button(
                new Rectangle((screenWidth / 2) - buttonWidth - 120, buttonY, buttonWidth, buttonHeight),
                buttonTexture,
                buttonPressedTexture,
                instructionFont,
                "Left Pile",
                Color.White
            );
            buttonPile1.Click += ButtonPile1_Click;
            buttonPile1.Visible = false;
            Game.Components.Add(buttonPile1);

            // Pile 2 button (middle)
            buttonPile2 = new Button(
                new Rectangle((screenWidth / 2) - buttonWidth / 2, buttonY, buttonWidth, buttonHeight),
                buttonTexture,
                buttonPressedTexture,
                instructionFont,
                "Middle Pile",
                Color.White
            );
            buttonPile2.Click += ButtonPile2_Click;
            buttonPile2.Visible = false;
            Game.Components.Add(buttonPile2);

            // Pile 3 button (right)
            buttonPile3 = new Button(
                new Rectangle((screenWidth / 2) + 120, buttonY, buttonWidth, buttonHeight),
                buttonTexture,
                buttonPressedTexture,
                instructionFont,
                "Right Pile",
                Color.White
            );
            buttonPile3.Click += ButtonPile3_Click;
            buttonPile3.Visible = false;
            Game.Components.Add(buttonPile3);

            // Continue button (for advancing through states)
            buttonContinue = new Button(
                new Rectangle((screenWidth / 2) - buttonWidth / 2, buttonY + 80, buttonWidth, buttonHeight),
                buttonTexture,
                buttonPressedTexture,
                instructionFont,
                "Continue",
                Color.LightGreen
            );
            buttonContinue.Click += ButtonContinue_Click;
            buttonContinue.Visible = false;
            Game.Components.Add(buttonContinue);

            // New Trick button (restart)
            buttonNewTrick = new Button(
                new Rectangle((screenWidth / 2) - buttonWidth / 2, buttonY + 160, buttonWidth, buttonHeight),
                buttonTexture,
                buttonPressedTexture,
                instructionFont,
                "New Trick",
                Color.LightBlue
            );
            buttonNewTrick.Click += ButtonNewTrick_Click;
            buttonNewTrick.Visible = false;
            Game.Components.Add(buttonNewTrick);
        }

        #endregion
```

**What We've Set Up:**
- **Fields**: Track game state, cards, UI components
- **Constructor**: Initializes with 1 deck, no jokers, single player
- **Initialize**: Calculates card grid positioning
- **LoadContent**: Creates 5 buttons (3 pile buttons, continue, new trick)

### Step 5.2: MagicTrickCardGame - Part 2 (Player Management)

Add these methods to handle players:

```csharp
        #region Player Management

        /// <summary>
        /// Adds a player to the game
        /// </summary>
        public override void AddPlayer(Player newPlayer)
        {
            if (!(newPlayer is MagicTrickPlayer))
            {
                throw new ArgumentException("Player must be of type MagicTrickPlayer");
            }

            base.AddPlayer(newPlayer);
            player = (MagicTrickPlayer)newPlayer;

            // Initialize rules now that we have a player
            cardSelectionRule = new CardSelectionRule(player);
            cardSelectionRule.RuleMatch += CardSelectionRule_RuleMatch;
            Rules.Add(cardSelectionRule);

            revealRule = new RevealRule(this);
            revealRule.RuleMatch += RevealRule_RuleMatch;
            Rules.Add(revealRule);
        }

        /// <summary>
        /// Gets the current player
        /// </summary>
        public override Player GetCurrentPlayer()
        {
            return player;
        }

        #endregion
```

**Key Points:**
- Validates player type
- Initializes rules after player is added (rules need player reference)
- Wires up rule event handlers

### Step 5.3: MagicTrickCardGame - Part 3 (Dealing Cards)

```csharp
        #region Card Management

        /// <summary>
        /// Deals 9 cards into a 3x3 grid
        /// </summary>
        public override void Deal()
        {
            // Clear previous cards
            ClearTable();

            // Shuffle the deck
            dealer.Shuffle();

            // Deal 9 cards to the table
            for (int i = 0; i < TotalCards; i++)
            {
                TraditionalCard card = dealer[i];
                TableCards.Add(card);

                // Create animated component for this card
                AnimatedCardsGameComponent animatedCard = new AnimatedCardsGameComponent(card, Game);
                animatedCard.LoadContent();

                // Calculate position in 3x3 grid
                int row = i / CardsPerRow;
                int col = i % CardsPerRow;

                Vector2 targetPosition = gridStartPosition + new Vector2(
                    col * CardSpacingX,
                    row * CardSpacingY
                );

                animatedCard.CurrentPosition = targetPosition;
                animatedCard.IsFaceDown = false; // Show cards face-up

                animatedCards.Add(animatedCard);
                Game.Components.Add(animatedCard);
            }
        }

        /// <summary>
        /// Clears all cards from the table
        /// </summary>
        private void ClearTable()
        {
            // Remove animated components
            foreach (var animatedCard in animatedCards)
            {
                Game.Components.Remove(animatedCard);
            }

            animatedCards.Clear();
            TableCards.Clear();
        }

        /// <summary>
        /// Rearranges cards after pile selection
        /// </summary>
        /// <param name="selectedPile">Which pile (0, 1, 2) contains the player's card</param>
        private void RearrangeCards(int selectedPile)
        {
            // Collect cards by column (pile)
            List<TraditionalCard> pile1 = new List<TraditionalCard>(); // Left column
            List<TraditionalCard> pile2 = new List<TraditionalCard>(); // Middle column
            List<TraditionalCard> pile3 = new List<TraditionalCard>(); // Right column

            // Group cards into columns
            for (int i = 0; i < TableCards.Count; i++)
            {
                int col = i % CardsPerRow;

                if (col == 0)
                    pile1.Add(TableCards[i]);
                else if (col == 1)
                    pile2.Add(TableCards[i]);
                else
                    pile3.Add(TableCards[i]);
            }

            // Rearrange: Put selected pile in the middle
            // This is the key to the trick!
            List<TraditionalCard> rearranged = new List<TraditionalCard>();

            if (selectedPile == 0) // Left pile selected
            {
                rearranged.AddRange(pile2); // Other pile first
                rearranged.AddRange(pile1); // Selected pile in middle
                rearranged.AddRange(pile3); // Other pile last
            }
            else if (selectedPile == 1) // Middle pile selected
            {
                rearranged.AddRange(pile1);
                rearranged.AddRange(pile2); // Already in middle
                rearranged.AddRange(pile3);
            }
            else // Right pile selected
            {
                rearranged.AddRange(pile1);
                rearranged.AddRange(pile3); // Selected pile in middle
                rearranged.AddRange(pile2);
            }

            // Update table cards
            TableCards.Clear();
            TableCards.AddRange(rearranged);

            // Redeal cards in new order
            RedealCards();
        }

        /// <summary>
        /// Redeals cards to table in their current order
        /// </summary>
        private void RedealCards()
        {
            // Remove old animated components
            foreach (var animatedCard in animatedCards)
            {
                Game.Components.Remove(animatedCard);
            }
            animatedCards.Clear();

            // Create new animated components in new positions
            for (int i = 0; i < TableCards.Count; i++)
            {
                AnimatedCardsGameComponent animatedCard = new AnimatedCardsGameComponent(TableCards[i], Game);
                animatedCard.LoadContent();

                // Calculate position in 3x3 grid
                int row = i / CardsPerRow;
                int col = i % CardsPerRow;

                Vector2 targetPosition = gridStartPosition + new Vector2(
                    col * CardSpacingX,
                    row * CardSpacingY
                );

                animatedCard.CurrentPosition = targetPosition;
                animatedCard.IsFaceDown = false;

                animatedCards.Add(animatedCard);
                Game.Components.Add(animatedCard);
            }
        }

        #endregion
```

**The Magic Explained:**
- `RearrangeCards()` is where the trick happens!
- We collect cards by **column** (each column = a pile)
- We place the selected pile in the **middle** of the new arrangement
- After doing this **twice**, the selected card mathematically ends up at position 4 (center)

### Step 5.4: MagicTrickCardGame - Part 4 (Game Flow)

```csharp
        #region Game Flow

        /// <summary>
        /// Starts the trick
        /// </summary>
        public override void StartPlaying()
        {
            currentState = MagicTrickGameState.Dealing;
            Deal();

            // Move to selection phase after brief delay
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
            {
                currentState = MagicTrickGameState.PlayerSelecting;
            });
        }

        /// <summary>
        /// Updates the game state each frame
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Check rules
            CheckRules();

            // Update UI based on current state
            UpdateUIForState();
        }

        /// <summary>
        /// Updates button visibility and instruction text based on state
        /// </summary>
        private void UpdateUIForState()
        {
            // Hide all buttons first
            buttonPile1.Visible = false;
            buttonPile2.Visible = false;
            buttonPile3.Visible = false;
            buttonContinue.Visible = false;
            buttonNewTrick.Visible = false;

            switch (currentState)
            {
                case MagicTrickGameState.Dealing:
                    instructionText = "Watch carefully as the cards are dealt...";
                    break;

                case MagicTrickGameState.PlayerSelecting:
                    instructionText = "Mentally select one card. Remember it!\nClick Continue when ready.";
                    buttonContinue.Visible = true;
                    break;

                case MagicTrickGameState.FirstPileSelection:
                    instructionText = "Which pile contains your card?\n(Cards in each column are a pile)";
                    buttonPile1.Visible = true;
                    buttonPile2.Visible = true;
                    buttonPile3.Visible = true;
                    break;

                case MagicTrickGameState.FirstRearrange:
                    instructionText = "Watch as I rearrange the cards...";
                    break;

                case MagicTrickGameState.SecondPileSelection:
                    instructionText = "Now, which pile contains your card?";
                    buttonPile1.Visible = true;
                    buttonPile2.Visible = true;
                    buttonPile3.Visible = true;
                    break;

                case MagicTrickGameState.SecondRearrange:
                    instructionText = "One more rearrangement...";
                    break;

                case MagicTrickGameState.Revealing:
                    instructionText = "Your card is...";
                    break;

                case MagicTrickGameState.Complete:
                    // Get the revealed card for display
                    if (TableCards.Count >= 5)
                    {
                        TraditionalCard revealedCard = TableCards[4];
                        instructionText = $"Your card is the {revealedCard.Value} of {revealedCard.Type}!\n\nDid I guess correctly?";
                    }
                    buttonNewTrick.Visible = true;
                    break;
            }
        }

        #endregion
```

**State Machine Logic:**
- Each state has specific UI configuration
- Instructions guide the player through the trick
- Buttons appear/hide based on what's needed
- The state flows: Dealing → Selecting → FirstPile → Rearrange → SecondPile → Rearrange → Reveal → Complete

### Step 5.5: MagicTrickCardGame - Part 5 (Event Handlers)

```csharp
        #region Event Handlers

        /// <summary>
        /// Handles pile 1 (left) button click
        /// </summary>
        private void ButtonPile1_Click(object sender, EventArgs e)
        {
            SelectPile(0);
        }

        /// <summary>
        /// Handles pile 2 (middle) button click
        /// </summary>
        private void ButtonPile2_Click(object sender, EventArgs e)
        {
            SelectPile(1);
        }

        /// <summary>
        /// Handles pile 3 (right) button click
        /// </summary>
        private void ButtonPile3_Click(object sender, EventArgs e)
        {
            SelectPile(2);
        }

        /// <summary>
        /// Handles pile selection
        /// </summary>
        private void SelectPile(int pileIndex)
        {
            player.SelectedPile = pileIndex;
            player.HasSelected = true;

            // Rule will detect this change and fire event
        }

        /// <summary>
        /// Handles continue button click
        /// </summary>
        private void ButtonContinue_Click(object sender, EventArgs e)
        {
            if (currentState == MagicTrickGameState.PlayerSelecting)
            {
                currentState = MagicTrickGameState.FirstPileSelection;
            }
        }

        /// <summary>
        /// Handles new trick button click
        /// </summary>
        private void ButtonNewTrick_Click(object sender, EventArgs e)
        {
            player.ResetSelection();
            StartPlaying();
        }

        /// <summary>
        /// Handles card selection rule match
        /// </summary>
        private void CardSelectionRule_RuleMatch(object sender, EventArgs e)
        {
            CardSelectionEventArgs args = (CardSelectionEventArgs)e;

            if (currentState == MagicTrickGameState.FirstPileSelection)
            {
                // First selection made
                currentState = MagicTrickGameState.FirstRearrange;

                // Rearrange cards with selected pile in middle
                RearrangeCards(args.SelectedPile);

                // Reset for next selection
                player.ResetSelection();

                // Move to second selection after delay
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(t =>
                {
                    currentState = MagicTrickGameState.SecondPileSelection;
                });
            }
            else if (currentState == MagicTrickGameState.SecondPileSelection)
            {
                // Second selection made
                currentState = MagicTrickGameState.SecondRearrange;

                // Rearrange again
                RearrangeCards(args.SelectedPile);

                // Move to reveal after delay
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(t =>
                {
                    currentState = MagicTrickGameState.Revealing;
                });
            }
        }

        /// <summary>
        /// Handles reveal rule match
        /// </summary>
        private void RevealRule_RuleMatch(object sender, EventArgs e)
        {
            RevealEventArgs args = (RevealEventArgs)e;

            // Highlight the revealed card (center card)
            if (animatedCards.Count >= 5)
            {
                // You could add a scale animation or glow effect here
                // For simplicity, we'll just move to complete state
            }

            // Move to complete state
            System.Threading.Tasks.Task.Delay(2000).ContinueWith(t =>
            {
                currentState = MagicTrickGameState.Complete;
            });
        }

        #endregion
```

**Event Flow:**
1. Player clicks pile button → `SelectPile()` → Updates player state
2. `CardSelectionRule` detects state change → Fires `RuleMatch`
3. `CardSelectionRule_RuleMatch()` → Rearranges cards → Advances state
4. After 2 selections → `RevealRule` fires → Shows result

### Step 5.6: MagicTrickCardGame - Part 6 (Drawing and Utilities)

```csharp
        #region Drawing

        /// <summary>
        /// Draws the game
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw instruction text
            if (!string.IsNullOrEmpty(instructionText) && instructionFont != null)
            {
                SpriteBatch spriteBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));

                if (spriteBatch != null)
                {
                    // Measure text for centering
                    Vector2 textSize = instructionFont.MeasureString(instructionText);
                    Vector2 centeredPosition = new Vector2(
                        instructionPosition.X - textSize.X / 2,
                        instructionPosition.Y
                    );

                    // Draw text with shadow for readability
                    spriteBatch.DrawString(instructionFont, instructionText,
                        centeredPosition + new Vector2(2, 2), Color.Black);
                    spriteBatch.DrawString(instructionFont, instructionText,
                        centeredPosition, Color.White);
                }
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets the value of a card (not used in magic trick, but required by base class)
        /// </summary>
        public override int CardValue(TraditionalCard card)
        {
            // Magic trick doesn't need card values, but we implement for completeness
            switch (card.Value)
            {
                case CardValue.Ace:
                    return 1;
                case CardValue.Two:
                    return 2;
                case CardValue.Three:
                    return 3;
                case CardValue.Four:
                    return 4;
                case CardValue.Five:
                    return 5;
                case CardValue.Six:
                    return 6;
                case CardValue.Seven:
                    return 7;
                case CardValue.Eight:
                    return 8;
                case CardValue.Nine:
                    return 9;
                case CardValue.Ten:
                case CardValue.Jack:
                case CardValue.Queen:
                case CardValue.King:
                    return 10;
                default:
                    return 0;
            }
        }

        #endregion
    }
}
```

**Drawing:**
- Centers instruction text on screen
- Adds shadow for readability
- Cards draw themselves via `AnimatedCardsGameComponent`

---

## Part 6: Integrate with Screen System

### Step 6.1: Create a Screen for the Magic Trick

**Create:** `Core/Game/Screens/MagicTrickGameplayScreen.cs`

```csharp
using System;
using Microsoft.Xna.Framework;
using CardsFramework.MagicTrick;
using CardsFramework.UI;
using GameStateManagement;

namespace CardsStarterKit
{
    /// <summary>
    /// Screen that hosts the magic trick game
    /// </summary>
    public class MagicTrickGameplayScreen : GameScreen
    {
        private MagicTrickCardGame magicTrickGame;

        public MagicTrickGameplayScreen()
        {
            EnabledGestures = Microsoft.Xna.Framework.Input.Touch.GestureType.Tap;
        }

        /// <summary>
        /// Loads content and initializes the game
        /// </summary>
        public override void LoadContent()
        {
            base.LoadContent();

            // Create game table
            GameTable gameTable = new GameTable(ScreenManager.Game, 1); // 1 player position

            // Create the magic trick game
            magicTrickGame = new MagicTrickCardGame(gameTable);
            ScreenManager.Game.Components.Add(magicTrickGame);
            magicTrickGame.Initialize();
            magicTrickGame.LoadContent();

            // Add the player
            MagicTrickPlayer player = new MagicTrickPlayer("You", magicTrickGame);
            magicTrickGame.AddPlayer(player);

            // Start the trick
            magicTrickGame.StartPlaying();
        }

        /// <summary>
        /// Handles input
        /// </summary>
        public override void HandleInput(InputState input)
        {
            base.HandleInput(input);

            // Handle back button / escape to return to menu
            if (input.IsPauseGame(null))
            {
                ScreenManager.AddScreen(new PauseScreen(), null);
            }
        }

        /// <summary>
        /// Updates the screen
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        /// <summary>
        /// Cleanup when exiting
        /// </summary>
        public override void UnloadContent()
        {
            if (magicTrickGame != null)
            {
                ScreenManager.Game.Components.Remove(magicTrickGame);
            }

            base.UnloadContent();
        }
    }
}
```

**Integration:**
- Creates `GameTable` for layout
- Instantiates `MagicTrickCardGame`
- Adds single player
- Starts the trick
- Handles pause/back navigation

### Step 6.2: Add Menu Entry

**Modify:** `Core/Game/Screens/MainMenuScreen.cs`

Find the constructor where menu entries are added and add:

```csharp
// Add magic trick menu entry
MenuEntry magicTrickMenuEntry = new MenuEntry("Magic Trick");
magicTrickMenuEntry.Selected += MagicTrickMenuEntrySelected;
menuEntries.Add(magicTrickMenuEntry);
```

Then add the event handler method:

```csharp
/// <summary>
/// Handles Magic Trick menu selection
/// </summary>
private void MagicTrickMenuEntrySelected(object sender, EventArgs e)
{
    ScreenManager.AddScreen(new MagicTrickGameplayScreen(), null);
}
```

---

## Part 7: Testing Your Magic Trick

### Step 7.1: Build and Run

```bash
dotnet build
dotnet run --project Platforms/Desktop/CardsStarterKit.Desktop.csproj
```

### Step 7.2: Test the Flow

1. **Launch:** Select "Magic Trick" from main menu
2. **Deal:** 9 cards appear in a 3x3 grid
3. **Select:** Mentally pick a card (e.g., 7 of Hearts in top-right)
4. **First Question:** Click which pile (column) contains your card
5. **Rearrange:** Watch cards rearrange
6. **Second Question:** Again, click which pile contains your card
7. **Reveal:** The center card should be your selected card!

### Step 7.3: Verify the Math

Try this test:
- Pick the card at position [0,2] (top-right)
- That's in pile 2 (right column)
- After first rearrange, it should move
- After second rearrange, it should be at index 4 (center)

The math always works!

---

## Part 8: Enhancements (Optional)

### Enhancement 1: Add Card Highlighting

In the `Revealing` state, highlight the center card:

```csharp
// In RevealRule_RuleMatch method:
if (animatedCards.Count >= 5)
{
    AnimatedCardsGameComponent centerCard = animatedCards[4];

    // Add scale animation to make it pulse
    ScaleGameComponentAnimation scaleAnim = new ScaleGameComponentAnimation(centerCard)
    {
        Duration = TimeSpan.FromSeconds(1),
        ScaleFactor = 1.2f,
        IsLooped = true,
        AnimationCycles = 3
    };

    centerCard.AnimationsList.Add(scaleAnim);
}
```

### Enhancement 2: Add Shuffle Animation

Make the rearrangement more dramatic:

```csharp
// In RearrangeCards method, before RedealCards():
// Animate cards flying off screen then back
foreach (var animatedCard in animatedCards)
{
    Vector2 offscreenPos = new Vector2(-500, animatedCard.CurrentPosition.Y);

    TransitionGameComponentAnimation transition = new TransitionGameComponentAnimation(
        animatedCard,
        offscreenPos,
        TimeSpan.FromSeconds(0.5)
    );

    animatedCard.AnimationsList.Add(transition);
}

// Then delay RedealCards() until animation completes
```

### Enhancement 3: Add Sound Effects

```csharp
// In LoadContent:
SoundEffect cardDealSound = Game.Content.Load<SoundEffect>(@"Sounds\CardPlace");
SoundEffect revealSound = Game.Content.Load<SoundEffect>(@"Sounds\Reveal");

// Play at appropriate times:
cardDealSound.Play(); // When dealing
revealSound.Play();   // When revealing
```

---

## Key Takeaways

### What You Learned

1. **Extending CardsGame:**
   - Override `Deal()`, `AddPlayer()`, `GetCurrentPlayer()`
   - Use constructor to specify deck configuration
   - Implement state machines for game flow

2. **Creating Game Rules:**
   - Inherit from `GameRule`
   - Override `Check()` method
   - Fire `RuleMatch` events when conditions are met
   - Track state changes to prevent duplicate events

3. **Working with Animations:**
   - Use `AnimatedCardsGameComponent` for card rendering
   - Position cards in grids using calculated vectors
   - Cards automatically handle face-up/face-down rendering

4. **UI Management:**
   - Create buttons with `Button` class
   - Show/hide buttons based on game state
   - Use `SpriteBatch` for custom text rendering

5. **Game Flow:**
   - Use state enums to control phases
   - Update UI based on current state
   - Use async delays for timed transitions

### The Magic Trick Pattern

This trick demonstrates a key card game concept: **controlled card positioning through mathematical manipulation**. The same pattern appears in:

- Card sorting algorithms
- Deck cutting tricks
- Dealing patterns in games like Bridge

### Next Steps

Try modifying the trick:
- Use 21 cards in a 7x3 grid (requires 3 selections)
- Add a "shuffle" phase between selections
- Let the player choose the reveal method
- Create different grid layouts

This framework makes it easy to experiment with card logic without worrying about rendering or input handling!

---

## Troubleshooting

### Cards Don't Appear
- Check that `LoadContent()` was called
- Verify card textures exist in Content/Images/Cards/
- Ensure `IsFaceDown = false` is set

### Buttons Don't Respond
- Confirm `EnabledGestures` includes `GestureType.Tap`
- Check button `Visible` property
- Verify `Game.Components.Add(button)` was called

### Wrong Card Revealed
- Debug the `RearrangeCards()` logic
- Print `TableCards` order after each rearrange
- Verify columns are collected correctly (index % 3)

### State Machine Stuck
- Add debug output in `Update()` to track state changes
- Check that all state transitions are implemented
- Verify async tasks complete properly

---

## Complete File Checklist

- [ ] `Core/Game/MagicTrick/Game/MagicTrickGameState.cs`
- [ ] `Core/Game/MagicTrick/Game/MagicTrickCardGame.cs`
- [ ] `Core/Game/MagicTrick/Players/MagicTrickPlayer.cs`
- [ ] `Core/Game/MagicTrick/Rules/CardSelectionRule.cs`
- [ ] `Core/Game/MagicTrick/Rules/RevealRule.cs`
- [ ] `Core/Game/Screens/MagicTrickGameplayScreen.cs`
- [ ] Modified `Core/Game/Screens/MainMenuScreen.cs`

---

## Conclusion

Congratulations! You've built a complete interactive magic trick using the CardsStarterKit framework. You now understand:

- How to extend the framework for custom card games
- How to use the rule system for game logic
- How to manage game state and flow
- How to create interactive UI with buttons
- How card animations work

This foundation prepares you to build more complex card games. The next tutorial covers **Gin Rummy**, which introduces multi-phase gameplay, meld detection, scoring, and AI opponents.

**Happy coding and enjoy amazing your friends with your magic trick!**
