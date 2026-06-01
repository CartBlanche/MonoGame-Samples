# Tutorial: Building a 9-Card Magic Trick Game

## Overview

This tutorial walks you through building a classic 9-card magic trick, the kind that always fools people because it's based on pure maths, not sleight of hand. Along the way, you'll learn how to extend the CardsStarterKit framework with:

- Custom game logic by extending `CardsGame`
- Game rules that respond to player actions
- State machines for controlling game flow
- Card animations and positioning
- Simple UI with buttons
- Single-player gameplay

If you're new to MonoGame and card game frameworks, this is a great starting point. It's short enough to complete in 2-3 hours but teaches solid patterns you'll reuse in bigger games.

**Framework note:** The snippets below target the current Cards.Framework / Cards.Framework.Core API shape in this repo.
**Host note:** This tutorial is written to be implemented inside the Blank sample template under `3-Games/Blank/Core/`.

**Difficulty:** Beginner | **Time:** 2-3 hours

---

## The Magic Trick Explained

### How It Works

The 9-Card Mind Reader is a classic maths card trick:

1. **Setup:** Lay out 9 cards face-up in a 3x3 grid
2. **Selection:** The spectator (player) mentally selects one card
3. **Reveal Phase 1:** The magician picks up the cards in 3 columns and asks "Which pile is your card in?"
4. **Rearrange:** The magician places the selected pile in the middle and lays out the cards again in 3 columns
5. **Reveal Phase 2:** The magician asks again "Which pile?"
6. **Final Reveal:** The magician dramatically reveals the middle card - which is always the selected card!

### How the Magic Works

Honestly, it's not magic, it's maths. Put the selected pile in the middle twice, and the card mathematically ends up at the centre position (index 4). Once you understand this, you can impress anyone and explain how the code makes it happen.

---

## Part 1: Project Structure Setup

### Step 1.1: Create Module Folders in Blank

Inside `3-Games/Blank/Core`, keep Magic Trick organized like this:

```
3-Games/Blank/Core/MagicTrick/
├── Core/
│   ├── MagicTrickCardGame.cs
│   ├── MagicTrickGameState.cs
├── Players/
│   └── MagicTrickPlayer.cs
├── Rules/
│   ├── CardSelectionRule.cs
│   └── RevealRule.cs
├── UI/
│   └── Button.cs (copied from Blackjack)
└── Screens/
    └── MagicTrickGameplayScreen.cs
```

Create these directories:

```bash
mkdir -p 3-Games/Blank/Core/MagicTrick/Core
mkdir -p 3-Games/Blank/Core/MagicTrick/Players
mkdir -p 3-Games/Blank/Core/MagicTrick/Rules
mkdir -p 3-Games/Blank/Core/MagicTrick/UI
mkdir -p 3-Games/Blank/Core/MagicTrick/Core/Screens
```

Then copy `3-Games/Blackjack/Core/UI/Button.cs` into `3-Games/Blank/Core/MagicTrick/UI/Button.cs` and change its namespace from `Blackjack` to `MagicTrick`.

---

## Part 2: Define Game State

### Step 2.1: Create the Game State Enum

The trick flows through distinct phases (dealing → selecting → rearranging → revealing). A state enum keeps things organised and makes the code easy to follow.

**Create:** `3-Games/Blank/Core/MagicTrick/Core/MagicTrickGameState.cs`

```csharp
namespace MagicTrick
{
    public enum MagicTrickGameState
    {
        Dealing,              // Deal 9 cards to table
        PlayerSelecting,      // Player picks a card mentally
        FirstPileSelection,   // First "which pile?" question
        FirstRearrange,       // Rearrange after first selection
        SecondPileSelection,  // Second "which pile?" question
        SecondRearrange,      // Final rearrangement
        Revealing,            // Show the selected card
        Complete              // Trick done, show result
    }
}
```

Each state handles one phase of the trick. This keeps the update loop simple and makes it trivial to add animations or UI changes specific to each phase.

---

## Part 3: Create the Player Class

### Step 3.1: Define MagicTrickPlayer

Keep this simple. The player just needs to track which pile they picked.

**Create:** `3-Games/Blank/Core/MagicTrick/Players/MagicTrickPlayer.cs`

```csharp
using CardsFramework;

namespace MagicTrick
{
    public class MagicTrickPlayer : Player
    {
        public int SelectedPile { get; set; }     // Which pile (0, 1, or 2)
        public bool HasSelected { get; set; }     // Did they pick one yet?

        public MagicTrickPlayer(string name, CardsGame game)
            : base(name, game)
        {
            SelectedPile = -1;
            HasSelected = false;
        }

        public void ResetSelection()
        {
            SelectedPile = -1;
            HasSelected = false;
        }
    }
}
```

That's it. We inherit from `Player` (which gives us Name, Game, Hand), track their selection, and reset for new rounds.

---

## Part 4: Create Game Rules

### Step 4.1: Card Selection Rule

This rule watches for the player's pile selection and fires an event when they pick one. The game loop will respond to that event and rearrange the cards.

**Create:** `3-Games/Blank/Core/MagicTrick/Rules/CardSelectionRule.cs`

```csharp
using System;
using CardsFramework;

namespace MagicTrick
{
    public class CardSelectionEventArgs : EventArgs
    {
        public MagicTrickPlayer Player { get; set; }
        public int SelectedPile { get; set; }
    }

    public class CardSelectionRule : GameRule
    {
        private readonly MagicTrickPlayer player;
        private bool previousHasSelected;

        public CardSelectionRule(MagicTrickPlayer player)
        {
            this.player = player;
            this.previousHasSelected = false;
        }

        public override void Check()
        {
            // Fire event only on the transition from not-selected to selected
            if (player.HasSelected && !previousHasSelected)
            {
                previousHasSelected = true;
                FireRuleMatch(new CardSelectionEventArgs
                {
                    Player = player,
                    SelectedPile = player.SelectedPile
                });
            }

            // Reset tracking for the next selection
            if (!player.HasSelected)
                previousHasSelected = false;
        }
    }
}
```

The key trick here: we track the *previous* state and only fire the event when HasSelected changes from false → true. This prevents firing multiple times if the player's selection stays the same.

### Step 4.2: Reveal Rule

This fires when the game is ready to reveal. The magic happens here - we know the selected card is always at position 4 (the centre).

**Create:** `3-Games/Blank/Core/MagicTrick/Rules/RevealRule.cs`

```csharp
using System;
using CardsFramework;

namespace MagicTrick
{
    public class RevealEventArgs : EventArgs
    {
        public TraditionalCard RevealedCard { get; set; }
    }

    public class RevealRule : GameRule
    {
        private readonly MagicTrickCardGame game;
        private bool hasRevealed;

        public RevealRule(MagicTrickCardGame game)
        {
            this.game = game;
            this.hasRevealed = false;
        }

        public override void Check()
        {
            if (game.State == MagicTrickGameState.Revealing && !hasRevealed)
            {
                hasRevealed = true;

                // The selected card is always at index 4 after two rearrangements
                if (game.TableCards.Count >= 5)
                {
                    FireRuleMatch(new RevealEventArgs
                    {
                        RevealedCard = game.TableCards[4]
                    });
                }
            }

            if (game.State == MagicTrickGameState.Dealing)
                hasRevealed = false;
        }
    }
}
```

Notice the one-shot pattern: `hasRevealed` ensures we only fire once per reveal phase. We reset when dealing starts again.

---

## Part 5: Create the Main Game Class

### Step 5.1: MagicTrickCardGame - Part 1 (Fields and Constructor)

Now for the main game class. This is where everything comes together. We'll build it in chunks to keep it manageable.

**Create:** `3-Games/Blank/Core/MagicTrick/Core/MagicTrickCardGame.cs`

```csharp
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CardsFramework;
using CardsFramework.Core;

namespace MagicTrick
{
    public class MagicTrickCardGame : CardsGame
    {
        #region Fields

        private MagicTrickGameState currentState;
        public MagicTrickGameState State
        {
            get { return currentState; }
            set { currentState = value; }
        }

        public List<TraditionalCard> TableCards { get; private set; }
        private List<AnimatedCardsGameComponent> animatedCards;

        // UI buttons
        private Button buttonPile1, buttonPile2, buttonPile3;
        private Button buttonContinue, buttonNewTrick;

        // Game pieces
        private MagicTrickPlayer player;
        private CardSelectionRule cardSelectionRule;
        private RevealRule revealRule;

        // Display
        private string instructionText;
        private Vector2 instructionPosition;
        private SpriteFont instructionFont;
        private ScreenManager screenManager;

        // Layout
        private const int CardsPerRow = 3;
        private const int TotalCards = 9;
        private const float CardSpacingX = 120f;
        private const float CardSpacingY = 160f;
        private Vector2 gridStartPosition;

        // State transitions
        private float stateTransitionTimer, stateTransitionDelay;
        private MagicTrickGameState nextState;
        private bool waitingForStateTransition;

        #endregion

        public MagicTrickCardGame(GameTable gameTable, ScreenManager screenManager)
            : base(1, 0, CardSuit.AllSuits, CardValue.NonJokers, 1, 1, gameTable, "Default", screenManager.Game)
        {
            this.screenManager = screenManager;
            TableCards = new List<TraditionalCard>();
            animatedCards = new List<AnimatedCardsGameComponent>();
            instructionText = "";
        }

        public void Initialize()
        {
            int screenWidth = screenManager.SafeArea.Width;
            int screenHeight = screenManager.SafeArea.Height;

            gridStartPosition = new Vector2(
                (screenWidth - (CardSpacingX * (CardsPerRow - 1))) / 2 - 50,
                100
            );

            instructionPosition = new Vector2(screenWidth / 2, 50);
            currentState = MagicTrickGameState.Dealing;
        }

        public void LoadContent()
        {
            base.LoadContent();

            instructionFont = screenManager.Font;
            InputState input = screenManager.InputState;

            // Create the 5 buttons we need
            buttonPile1 = CreateButton(input);
            buttonPile1.Click += (s, e) => SelectPile(0);

            buttonPile2 = CreateButton(input);
            buttonPile2.Click += (s, e) => SelectPile(1);

            buttonPile3 = CreateButton(input);
            buttonPile3.Click += (s, e) => SelectPile(2);

            buttonContinue = CreateButton(input);
            buttonContinue.Click += ButtonContinue_Click;

            buttonNewTrick = CreateButton(input);
            buttonNewTrick.Click += ButtonNewTrick_Click;
        }

        private Button CreateButton(InputState input)
        {
            var btn = new Button("ButtonRegular", "ButtonPressed", input, this,
                screenManager.SpriteBatch, screenManager.GlobalTransformation);
            btn.Visible = false;
            Game.Components.Add(btn);
            return btn;
        }

        #endregion

### Step 5.2: MagicTrickCardGame - Part 2 (Player Management)

Next, add methods to register the player and initialize rules:

```csharp
        public override void AddPlayer(Player newPlayer)
        {
            if (!(newPlayer is MagicTrickPlayer))
                throw new ArgumentException("Expected MagicTrickPlayer");

            if (players.Count >= MaximumPlayers)
                throw new InvalidOperationException("Maximum players reached.");

            players.Add(newPlayer);
            player = (MagicTrickPlayer)newPlayer;

            // Set up rules now that we have a player
            cardSelectionRule = new CardSelectionRule(player);
            cardSelectionRule.RuleMatch += CardSelectionRule_RuleMatch;
            rules.Add(cardSelectionRule);

            revealRule = new RevealRule(this);
            revealRule.RuleMatch += RevealRule_RuleMatch;
            rules.Add(revealRule);
        }

        public override Player GetCurrentPlayer() => player;
```

**Key Points:**
- Validates player type
- Initializes rules after player is added (rules need player reference)
- Wires up rule event handlers

### Step 5.3: MagicTrickCardGame - Part 3 (Card Dealing & Rearranging)

Now the fun part: dealing cards and rearranging them.

```csharp
        public override void Deal()
        {
            ClearTable();
            dealer.Shuffle();

            for (int i = 0; i < TotalCards; i++)
            {
                TraditionalCard card = dealer[i];
                TableCards.Add(card);

                var animatedCard = new AnimatedCardsGameComponent(
                    card, this, screenManager.SpriteBatch, screenManager.GlobalTransformation);
                animatedCard.LoadContent();

                int row = i / CardsPerRow;
                int col = i % CardsPerRow;
                Vector2 pos = gridStartPosition + new Vector2(col * CardSpacingX, row * CardSpacingY);

                animatedCard.CurrentPosition = pos;
                animatedCard.IsFaceDown = false;

                animatedCards.Add(animatedCard);
                Game.Components.Add(animatedCard);
            }
        }

        private void ClearTable()
        {
            foreach (var card in animatedCards)
                Game.Components.Remove(card);
            animatedCards.Clear();
            TableCards.Clear();
        }

        private void RearrangeCards(int selectedPile)
        {
            // Split table cards into 3 piles by column
            var pile0 = new List<TraditionalCard>();
            var pile1 = new List<TraditionalCard>();
            var pile2 = new List<TraditionalCard>();

            for (int i = 0; i < TableCards.Count; i++)
            {
                int col = i % CardsPerRow;
                if (col == 0) pile0.Add(TableCards[i]);
                else if (col == 1) pile1.Add(TableCards[i]);
                else pile2.Add(TableCards[i]);
            }

            // Put selected pile in the middle - this is the magic!
            var rearranged = new List<TraditionalCard>();
            if (selectedPile == 0)
            {
                rearranged.AddRange(pile1);
                rearranged.AddRange(pile0);
                rearranged.AddRange(pile2);
            }
            else if (selectedPile == 1)
            {
                rearranged.AddRange(pile0);
                rearranged.AddRange(pile1);
                rearranged.AddRange(pile2);
            }
            else
            {
                rearranged.AddRange(pile0);
                rearranged.AddRange(pile2);
                rearranged.AddRange(pile1);
            }

            TableCards.Clear();
            TableCards.AddRange(rearranged);
            RedealCards();
        }

        private void RedealCards()
        {
            foreach (var card in animatedCards)
                Game.Components.Remove(card);
            animatedCards.Clear();

            for (int i = 0; i < TableCards.Count; i++)
            {
                var animatedCard = new AnimatedCardsGameComponent(
                    TableCards[i], this, screenManager.SpriteBatch, screenManager.GlobalTransformation);
                animatedCard.LoadContent();

                int row = i / CardsPerRow;
                int col = i % CardsPerRow;
                Vector2 pos = gridStartPosition + new Vector2(col * CardSpacingX, row * CardSpacingY);

                animatedCard.CurrentPosition = pos;
                animatedCard.IsFaceDown = false;

                animatedCards.Add(animatedCard);
                Game.Components.Add(animatedCard);
            }
        }
```

The key: `RearrangeCards()` does the maths. We split into 3 piles (by column), then place the selected pile in the middle of the new arrangement. Do this twice, and the selected card is guaranteed to be at index 4.

### Step 5.4: MagicTrickCardGame - Part 4 (Game Flow)

Now let's wire up the state machine and button handlers.

```csharp
        public override void StartPlaying()
        {
            currentState = MagicTrickGameState.Dealing;
            Deal();
            ScheduleStateTransition(MagicTrickGameState.PlayerSelecting, 1000f);
        }

        private void ScheduleStateTransition(MagicTrickGameState newState, float delayMs)
        {
            nextState = newState;
            stateTransitionDelay = delayMs;
            stateTransitionTimer = 0;
            waitingForStateTransition = true;
        }

        public void Update(GameTime gameTime)
        {
            if (waitingForStateTransition)
            {
                stateTransitionTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (stateTransitionTimer >= stateTransitionDelay)
                {
                    currentState = nextState;
                    waitingForStateTransition = false;
                }
            }

            CheckRules();
            UpdateUIForState();
        }

        private void UpdateUIForState()
        {
            buttonPile1.Visible = buttonPile2.Visible = buttonPile3.Visible = false;
            buttonContinue.Visible = buttonNewTrick.Visible = false;

            switch (currentState)
            {
                case MagicTrickGameState.Dealing:
                    instructionText = "Watch carefully as the cards are dealt...";
                    break;
                case MagicTrickGameState.PlayerSelecting:
                    instructionText = "Mentally pick a card. Remember it!\nClick Continue when ready.";
                    buttonContinue.Visible = true;
                    break;
                case MagicTrickGameState.FirstPileSelection:
                    instructionText = "Which pile has your card?";
                    buttonPile1.Visible = buttonPile2.Visible = buttonPile3.Visible = true;
                    break;
                case MagicTrickGameState.FirstRearrange:
                    instructionText = "Rearranging the cards...";
                    break;
                case MagicTrickGameState.SecondPileSelection:
                    instructionText = "Which pile has your card now?";
                    buttonPile1.Visible = buttonPile2.Visible = buttonPile3.Visible = true;
                    break;
                case MagicTrickGameState.SecondRearrange:
                    instructionText = "One final rearrangement...";
                    break;
                case MagicTrickGameState.Revealing:
                    instructionText = "Your card is...";
                    break;
                case MagicTrickGameState.Complete:
                    if (TableCards.Count >= 5)
                    {
                        TraditionalCard revealed = TableCards[4];
                        instructionText = $"It\'s the {revealed.Value} of {revealed.Type}!\n\nWas I right?";
                    }
                    buttonNewTrick.Visible = true;
                    break;
            }
        }
```

### Step 5.5: MagicTrickCardGame - Part 5 (Event Handlers)

Wire up the buttons and rule events:

```csharp
        private void SelectPile(int pileIndex)
        {
            player.SelectedPile = pileIndex;
            player.HasSelected = true;
        }

        private void ButtonContinue_Click(object sender, EventArgs e)
        {
            if (currentState == MagicTrickGameState.PlayerSelecting)
                currentState = MagicTrickGameState.FirstPileSelection;
        }

        private void ButtonNewTrick_Click(object sender, EventArgs e)
        {
            player.ResetSelection();
            StartPlaying();
        }

        private void CardSelectionRule_RuleMatch(object sender, EventArgs e)
        {
            var args = (CardSelectionEventArgs)e;

            if (currentState == MagicTrickGameState.FirstPileSelection)
            {
                currentState = MagicTrickGameState.FirstRearrange;
                RearrangeCards(args.SelectedPile);
                player.ResetSelection();
                ScheduleStateTransition(MagicTrickGameState.SecondPileSelection, 1500f);
            }
            else if (currentState == MagicTrickGameState.SecondPileSelection)
            {
                currentState = MagicTrickGameState.SecondRearrange;
                RearrangeCards(args.SelectedPile);
                ScheduleStateTransition(MagicTrickGameState.Revealing, 1500f);
            }
        }

        private void RevealRule_RuleMatch(object sender, EventArgs e)
        {
            // Simple approach: just move to complete
            // (You could add fancy animations here)
            Task.Delay(2000).ContinueWith(t => currentState = MagicTrickGameState.Complete);
        }
```

**Event Flow:**
1. Player clicks pile button → `SelectPile()` → Updates player state
2. `CardSelectionRule` detects state change → Fires `RuleMatch`
3. `CardSelectionRule_RuleMatch()` → Rearranges cards → Advances state
4. After 2 selections → `RevealRule` fires → Shows result

### Step 5.6: MagicTrickCardGame - Part 6 (Drawing & Utilities)

Almost done with the game class. Just need drawing and a utility method:

```csharp
        #region Drawing

        /// <summary>
        /// Draws the game
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            // Draw instruction text
            if (!string.IsNullOrEmpty(instructionText) && instructionFont != null)
            {
                SpriteBatch spriteBatch = screenManager.SpriteBatch;

                if (spriteBatch != null)
                {
                    // Measure text for centring
                    Vector2 textSize = instructionFont.MeasureString(instructionText);
                    Vector2 centeredPosition = new Vector2(
                        instructionPosition.X - textSize.X / 2,
                        instructionPosition.Y
                    );

                    // Draw text with shadow for readability
                    spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null,
                        screenManager.GlobalTransformation);
                    spriteBatch.DrawString(instructionFont, instructionText,
                        centeredPosition + new Vector2(2, 2), Color.Black);
                    spriteBatch.DrawString(instructionFont, instructionText,
                        centeredPosition, Color.White);
                    spriteBatch.End();
                }
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets the value of a card (not used in magic trick, but required by base class)
        /// </summary>
        /// <remarks>
        /// IMPORTANT: The CardsGame base class declares CardValue() as an abstract method,
        /// so every game MUST override it. Even though the magic trick doesn't need card
        /// values for scoring, we provide a standard implementation here. Games like Blackjack
        /// and Gin Rummy use this for actual scoring logic.
        /// </remarks>
        public override int CardValue(TraditionalCard card)
        {
            // Magic trick doesn't need card values, but we implement for completeness
            // This is a standard card value mapping (Aces=1, Face cards=10)
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
- Centres instruction text on screen
- Adds shadow for readability
- Cards draw themselves via `AnimatedCardsGameComponent`

---

## Part 6: Integrate with Screen System

### Step 6.1: Create a Screen for the Magic Trick

**Create:** `3-Games/Blank/Core/MagicTrick/Core/Screens/MagicTrickGameplayScreen.cs`

```csharp
using System;
using Microsoft.Xna.Framework;
using CardsFramework;
using CardsFramework.Core;
using MagicTrick;

namespace Blank
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
            GameTable gameTable = new GameTable(ScreenManager.Game, ScreenManager.SpriteBatch, 1); // 1 player position

            // Create the magic trick game
            magicTrickGame = new MagicTrickCardGame(gameTable, ScreenManager);
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
            magicTrickGame?.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            magicTrickGame?.Draw(gameTime);
        }

        /// <summary>
        /// Cleanup when exiting
        /// </summary>
        public override void UnloadContent()
        {
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

**Modify:** `3-Games/Blank/Core/Screens/MainMenuScreen.cs`

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

---

## Part 7: Test It Out

Build and run:

```bash
dotnet build 3-Games/Blank/Desktop/Blank.csproj
dotnet run --project 3-Games/Blank/Desktop/Blank.csproj
```

### Step 7.1: Run It

Launch the game, select "Magic Trick" from the menu. Here's what should happen:

1. **9 cards deal** in a 3x3 grid
2. **You pick a card mentally** (like "5 of Hearts in the middle row")
3. **First question:** Click which column (left/middle/right) has your card
4. **Cards rearrange** with your pile in the middle
5. **Second question:** Click which column again
6. **Final rearrange** - your card is now guaranteed to be in the centre
7. **Reveal:** The centre card is displayed

If you picked a card correctly in your head, it'll be right. If not, funny story anyway!

### Step 7.2: Verify the Maths

Try this manually:
- Pick a card at position 0 (top-left). It's in pile 0 (left column).
- After the first rearrange, it moves to positions 3, 4, or 5 (middle pile).
- After the second rearrange, it's guaranteed at position 4 (centre).

The maths always works. That's the whole point.

---

## Part 8: Enhancements (Optional)

### Add a Pulse Animation

When revealing, make the centre card pulse to emphasise it:

```csharp
if (animatedCards.Count >= 5)
{
    var scaleAnim = new ScaleGameComponentAnimation(1.0f, 1.15f)
    {
        Duration = TimeSpan.FromSeconds(0.5)
    };
    animatedCards[4].AddAnimation(scaleAnim);
}
```

### Add Sound Effects

Play a "reveal" sound when showing the card:

```csharp
// In LoadContent:
var revealSound = screenManager.Game.Content.Load<SoundEffect>("Sounds/Reveal");

// In RevealRule_RuleMatch:
revealSound.Play();
```

### Use 21 Cards (Advanced)

Make it even more impressive with 21 cards and 3 selections. Same maths, bigger impact. Requires a 7x3 grid instead of 3x3.

---

## Key Takeaways

You built a complete card game from scratch using the framework. Here's what you learned:

**Game Architecture:**
- Extend `CardsGame` to create custom games
- Use state enums to control game flow cleanly
- Wire up rules to respond to game events

**The Framework:**
- `GameRule` classes handle game logic
- `AnimatedCardsGameComponent` renders and positions cards
- `Button` class handles input
- `ScreenManager` integrates with the menu system

**The Maths:**
- A simple mathematical trick proves card handling doesn't require sleight of hand
- Controlled positioning guarantees outcomes
- This pattern appears in many card games

## What's Next?

Now that you understand the basics, check out the Gin Rummy tutorial to learn:
- Larger hand management (10 cards)
- Meld detection (combinations of cards)
- NPC opponents with AI
- Scoring systems
- Turn-based gameplay

Happy coding!
