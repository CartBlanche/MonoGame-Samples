# Tutorial: Implementing Gin Rummy

## Overview

In this tutorial, you'll build a complete Gin Rummy card game using the CardsStarterKit framework. This is a more complex implementation than the magic trick, teaching you:

- Managing larger hands (10 cards vs 2)
- Implementing meld detection (sets and runs)
- Creating intermediate NPC opponents
- Turn-based gameplay with draw/discard mechanics
- Calculating deadwood and scoring
- Multi-player support
- Advanced UI for card organization

**Target Audience:** MonoGame developers new to card games

**Estimated Time:** 6-8 hours

**Difficulty:** Intermediate

---

## What is Gin Rummy?

### Game Rules Summary

**Objective:** Form melds (sets/runs) and minimize deadwood (unmatched cards).

**Setup:**
- 2-4 players (we'll support up to 4)
- Each player receives 10 cards
- Remaining cards form the stock pile
- Top card of stock is flipped to start discard pile

**Gameplay Flow:**
1. **Draw Phase:** Player draws from stock or discard pile
2. **Discard Phase:** Player discards one card to discard pile
3. **Optional Knock:** If deadwood ≤ 10 points, player can knock
4. **Gin:** If deadwood = 0, player declares "Gin!"

**Melds:**
- **Set:** 3-4 cards of same rank (e.g., 7♥ 7♠ 7♣)
- **Run:** 3+ consecutive cards of same suit (e.g., 4♠ 5♠ 6♠)

**Deadwood Points:**
- Ace = 1 point
- 2-10 = face value
- J, Q, K = 10 points

**Scoring (Single Round - Tutorial Focus):**
- **Gin:** Knocker gets opponent's deadwood + 25 bonus
- **Knock:** Knocker gets difference in deadwood
- **Undercut:** If opponent has less/equal deadwood, opponent gets difference + 25 bonus

### What We'll Build

This tutorial focuses on **single-round gameplay** to keep it manageable. At the end, we'll discuss extending it to full match scoring (first to 100 points).

---

## Part 1: Project Structure

### Step 1.1: Create Directory Structure

```bash
mkdir -p 3-Games/GinRummy/Core
mkdir -p 3-Games/GinRummy/Players
mkdir -p 3-Games/GinRummy/Rules
mkdir -p 3-Games/GinRummy/UI
mkdir -p 3-Games/GinRummy/AI
```

Your structure will look like:

```
3-Games/GinRummy/
├── Core/
│   ├── GinRummyCardGame.cs
│   ├── GinRummyGameState.cs
│   ├── Meld.cs
│   └── MeldDetector.cs
├── Players/
│   ├── GinRummyPlayer.cs
│   └── GinRummyNPCPlayer.cs
├── Rules/
│   ├── KnockRule.cs
│   ├── GinRule.cs
│   └── TurnCompleteRule.cs
├── UI/
│   └── HandOrganizer.cs
└── AI/
    └── GinRummyAI.cs
```

---

## Part 2: Core Data Structures

### Step 2.1: Define Game State

**Create:** `Core/Game/GinRummy/Game/GinRummyGameState.cs`

```csharp
namespace GinRummy
{
    /// <summary>
    /// States of a Gin Rummy game
    /// </summary>
    public enum GinRummyGameState
    {
        /// <summary>
        /// Dealing initial hands
        /// </summary>
        Dealing,

        /// <summary>
        /// Player's turn - drawing phase
        /// </summary>
        Drawing,

        /// <summary>
        /// Player's turn - discarding phase
        /// </summary>
        Discarding,

        /// <summary>
        /// Player knocked - showing hands
        /// </summary>
        Knocked,

        /// <summary>
        /// Player got Gin - showing hands
        /// </summary>
        Gin,

        /// <summary>
        /// Calculating scores
        /// </summary>
        Scoring,

        /// <summary>
        /// Round complete - showing results
        /// </summary>
        RoundEnd,

        /// <summary>
        /// Waiting between turns
        /// </summary>
        Waiting
    }
}
```

### Step 2.2: Define Meld Structure

**Create:** `Core/Game/GinRummy/Game/Meld.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using CardsFramework.Cards;

namespace GinRummy
{
    /// <summary>
    /// Types of melds in Gin Rummy
    /// </summary>
    public enum MeldType
    {
        /// <summary>
        /// 3+ cards of same rank (e.g., 7♥ 7♠ 7♣)
        /// </summary>
        Set,

        /// <summary>
        /// 3+ consecutive cards of same suit (e.g., 4♠ 5♠ 6♠)
        /// </summary>
        Run
    }

    /// <summary>
    /// Represents a meld (set or run) of cards
    /// </summary>
    public class Meld
    {
        /// <summary>
        /// Type of this meld
        /// </summary>
        public MeldType Type { get; set; }

        /// <summary>
        /// Cards in this meld
        /// </summary>
        public List<TraditionalCard> Cards { get; set; }

        /// <summary>
        /// Total point value of cards in this meld
        /// </summary>
        public int PointValue
        {
            get
            {
                return Cards.Sum(card => GetCardPoints(card));
            }
        }

        public Meld()
        {
            Cards = new List<TraditionalCard>();
        }

        /// <summary>
        /// Creates a meld from a list of cards
        /// </summary>
        public Meld(MeldType type, List<TraditionalCard> cards)
        {
            Type = type;
            Cards = new List<TraditionalCard>(cards);
        }

        /// <summary>
        /// Gets the point value of a card for deadwood calculation
        /// </summary>
        public static int GetCardPoints(TraditionalCard card)
        {
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

        /// <summary>
        /// Checks if this meld is valid
        /// </summary>
        public bool IsValid()
        {
            if (Cards.Count < 3)
                return false;

            if (Type == MeldType.Set)
                return IsValidSet();
            else
                return IsValidRun();
        }

        /// <summary>
        /// Validates a set (same rank)
        /// </summary>
        private bool IsValidSet()
        {
            if (Cards.Count < 3 || Cards.Count > 4)
                return false;

            CardValue firstValue = Cards[0].Value;

            // All cards must have the same value
            return Cards.All(card => card.Value == firstValue);
        }

        /// <summary>
        /// Validates a run (consecutive same suit)
        /// </summary>
        private bool IsValidRun()
        {
            if (Cards.Count < 3)
                return false;

            CardSuit suit = Cards[0].Type;

            // All cards must be same suit
            if (!Cards.All(card => card.Type == suit))
                return false;

            // Sort cards by value
            var sortedCards = Cards.OrderBy(card => GetCardNumericValue(card)).ToList();

            // Check consecutive values
            for (int i = 1; i < sortedCards.Count; i++)
            {
                int prevValue = GetCardNumericValue(sortedCards[i - 1]);
                int currValue = GetCardNumericValue(sortedCards[i]);

                if (currValue != prevValue + 1)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets numeric value for sorting (Ace=1, King=13)
        /// </summary>
        private static int GetCardNumericValue(TraditionalCard card)
        {
            switch (card.Value)
            {
                case CardValue.Ace: return 1;
                case CardValue.Two: return 2;
                case CardValue.Three: return 3;
                case CardValue.Four: return 4;
                case CardValue.Five: return 5;
                case CardValue.Six: return 6;
                case CardValue.Seven: return 7;
                case CardValue.Eight: return 8;
                case CardValue.Nine: return 9;
                case CardValue.Ten: return 10;
                case CardValue.Jack: return 11;
                case CardValue.Queen: return 12;
                case CardValue.King: return 13;
                default: return 0;
            }
        }

        public override string ToString()
        {
            string cardList = string.Join(", ", Cards.Select(c => $"{c.Value} of {c.Type}"));
            return $"{Type}: [{cardList}]";
        }
    }
}
```

**Key Features:**
- `IsValid()`: Validates sets (same rank) and runs (consecutive, same suit)
- `PointValue`: Sums card values for scoring
- `GetCardPoints()`: Standard Gin Rummy point values

### Step 2.3: Create Meld Detector

This is the brain of Gin Rummy - finding optimal melds to minimize deadwood.

**Create:** `Core/Game/GinRummy/Game/MeldDetector.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using CardsFramework.Cards;

namespace GinRummy
{
    /// <summary>
    /// Detects valid melds in a hand and calculates deadwood
    /// </summary>
    public class MeldDetector
    {
        /// <summary>
        /// Finds the optimal set of melds that minimizes deadwood
        /// </summary>
        public static List<Meld> FindOptimalMelds(List<TraditionalCard> cards)
        {
            // Find all possible melds
            List<Meld> allPossibleMelds = FindAllPossibleMelds(cards);

            // Find combination with minimum deadwood
            return FindBestMeldCombination(cards, allPossibleMelds);
        }

        /// <summary>
        /// Finds all possible valid melds in the hand
        /// </summary>
        private static List<Meld> FindAllPossibleMelds(List<TraditionalCard> cards)
        {
            List<Meld> melds = new List<Meld>();

            // Find all sets (3-4 of same rank)
            melds.AddRange(FindSets(cards));

            // Find all runs (3+ consecutive same suit)
            melds.AddRange(FindRuns(cards));

            return melds;
        }

        /// <summary>
        /// Finds all possible sets in the hand
        /// </summary>
        private static List<Meld> FindSets(List<TraditionalCard> cards)
        {
            List<Meld> sets = new List<Meld>();

            // Group by card value
            var grouped = cards.GroupBy(card => card.Value)
                               .Where(g => g.Count() >= 3);

            foreach (var group in grouped)
            {
                var cardList = group.ToList();

                // Sets of 3
                if (cardList.Count >= 3)
                {
                    sets.Add(new Meld(MeldType.Set, cardList.Take(3).ToList()));
                }

                // Sets of 4
                if (cardList.Count == 4)
                {
                    sets.Add(new Meld(MeldType.Set, cardList));
                }
            }

            return sets;
        }

        /// <summary>
        /// Finds all possible runs in the hand
        /// </summary>
        private static List<Meld> FindRuns(List<TraditionalCard> cards)
        {
            List<Meld> runs = new List<Meld>();

            // Group by suit
            var bySuit = cards.GroupBy(card => card.Type);

            foreach (var suitGroup in bySuit)
            {
                var sortedCards = suitGroup.OrderBy(card => GetCardNumericValue(card)).ToList();

                // Find consecutive sequences
                for (int i = 0; i < sortedCards.Count; i++)
                {
                    List<TraditionalCard> sequence = new List<TraditionalCard> { sortedCards[i] };

                    // Build consecutive sequence
                    for (int j = i + 1; j < sortedCards.Count; j++)
                    {
                        int prevValue = GetCardNumericValue(sortedCards[j - 1]);
                        int currValue = GetCardNumericValue(sortedCards[j]);

                        if (currValue == prevValue + 1)
                        {
                            sequence.Add(sortedCards[j]);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Add all possible runs of length 3+
                    if (sequence.Count >= 3)
                    {
                        // Add runs of different lengths
                        for (int length = 3; length <= sequence.Count; length++)
                        {
                            runs.Add(new Meld(MeldType.Run, sequence.Take(length).ToList()));
                        }
                    }
                }
            }

            return runs;
        }

        /// <summary>
        /// Finds the best combination of non-overlapping melds
        /// </summary>
        private static List<Meld> FindBestMeldCombination(
            List<TraditionalCard> allCards,
            List<Meld> possibleMelds)
        {
            List<Meld> bestCombination = new List<Meld>();
            int minDeadwood = CalculateDeadwood(allCards, new List<Meld>());

            // Try all combinations of melds
            FindBestCombinationRecursive(
                allCards,
                possibleMelds,
                new List<Meld>(),
                ref bestCombination,
                ref minDeadwood);

            return bestCombination;
        }

        /// <summary>
        /// Recursive helper to find best non-overlapping meld combination
        /// </summary>
        private static void FindBestCombinationRecursive(
            List<TraditionalCard> allCards,
            List<Meld> possibleMelds,
            List<Meld> currentCombination,
            ref List<Meld> bestCombination,
            ref int minDeadwood)
        {
            // Calculate deadwood for current combination
            int deadwood = CalculateDeadwood(allCards, currentCombination);

            // Update best if this is better
            if (deadwood < minDeadwood)
            {
                minDeadwood = deadwood;
                bestCombination = new List<Meld>(currentCombination);
            }

            // Try adding each remaining meld
            for (int i = 0; i < possibleMelds.Count; i++)
            {
                Meld meld = possibleMelds[i];

                // Check if this meld overlaps with current combination
                if (!OverlapsWithCombination(meld, currentCombination))
                {
                    // Add this meld and recurse
                    currentCombination.Add(meld);

                    FindBestCombinationRecursive(
                        allCards,
                        possibleMelds.Skip(i + 1).ToList(),
                        currentCombination,
                        ref bestCombination,
                        ref minDeadwood);

                    // Backtrack
                    currentCombination.RemoveAt(currentCombination.Count - 1);
                }
            }
        }

        /// <summary>
        /// Checks if a meld uses any cards already in the combination
        /// </summary>
        private static bool OverlapsWithCombination(Meld meld, List<Meld> combination)
        {
            var usedCards = new HashSet<TraditionalCard>();

            foreach (var existingMeld in combination)
            {
                foreach (var card in existingMeld.Cards)
                {
                    usedCards.Add(card);
                }
            }

            return meld.Cards.Any(card => usedCards.Contains(card));
        }

        /// <summary>
        /// Calculates deadwood (unmatched cards) point value
        /// </summary>
        public static int CalculateDeadwood(List<TraditionalCard> allCards, List<Meld> melds)
        {
            // Get all cards in melds
            var meldedCards = new HashSet<TraditionalCard>();
            foreach (var meld in melds)
            {
                foreach (var card in meld.Cards)
                {
                    meldedCards.Add(card);
                }
            }

            // Calculate deadwood points
            int deadwood = 0;
            foreach (var card in allCards)
            {
                if (!meldedCards.Contains(card))
                {
                    deadwood += Meld.GetCardPoints(card);
                }
            }

            return deadwood;
        }

        /// <summary>
        /// Gets numeric value for card ordering
        /// </summary>
        private static int GetCardNumericValue(TraditionalCard card)
        {
            return Meld.GetCardNumericValue(card);
        }
    }
}
```

**Algorithm Explanation:**
1. **Find All Possible Melds:** Identify every valid set and run
2. **Try Combinations:** Recursively try non-overlapping meld combinations
3. **Minimize Deadwood:** Keep the combination with lowest deadwood points
4. **Backtracking:** Classic combinatorial optimization problem

This is computationally intensive for 10 cards but acceptable for gameplay.

---

## Part 3: Player Classes

### Step 3.1: GinRummyPlayer

**Create:** `Core/Game/GinRummy/Players/GinRummyPlayer.cs`

```csharp
using System.Collections.Generic;
using CardsFramework.Cards;
using CardsFramework.Players;
using CardsFramework.Game;

namespace GinRummy
{
    /// <summary>
    /// Represents a player in Gin Rummy
    /// </summary>
    public class GinRummyPlayer : Player
    {
        #region Properties

        /// <summary>
        /// Current melds in player's hand
        /// </summary>
        public List<Meld> Melds { get; set; }

        /// <summary>
        /// Deadwood point value
        /// </summary>
        public int Deadwood { get; set; }

        /// <summary>
        /// Whether player has knocked
        /// </summary>
        public bool HasKnocked { get; set; }

        /// <summary>
        /// Whether player has gin
        /// </summary>
        public bool HasGin { get; set; }

        /// <summary>
        /// Score for this round
        /// </summary>
        public int RoundScore { get; set; }

        /// <summary>
        /// Total score across all rounds (for future multi-round support)
        /// </summary>
        public int TotalScore { get; set; }

        /// <summary>
        /// Whether it's currently this player's turn
        /// </summary>
        public bool IsMyTurn { get; set; }

        /// <summary>
        /// Whether player has drawn this turn
        /// </summary>
        public bool HasDrawn { get; set; }

        #endregion

        #region Initialization

        public GinRummyPlayer(string name, CardsGame game)
            : base(name, game)
        {
            Melds = new List<Meld>();
            Deadwood = 0;
            HasKnocked = false;
            HasGin = false;
            RoundScore = 0;
            TotalScore = 0;
            IsMyTurn = false;
            HasDrawn = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Analyzes hand to find optimal melds and calculate deadwood
        /// </summary>
        public void AnalyzeHand()
        {
            List<TraditionalCard> handCards = new List<TraditionalCard>();

            for (int i = 0; i < Hand.Count; i++)
            {
                handCards.Add(Hand[i]);
            }

            // Find optimal melds
            Melds = MeldDetector.FindOptimalMelds(handCards);

            // Calculate deadwood
            Deadwood = MeldDetector.CalculateDeadwood(handCards, Melds);

            // Check for Gin (no deadwood)
            HasGin = (Deadwood == 0 && handCards.Count == 10);
        }

        /// <summary>
        /// Checks if player can knock (deadwood <= 10)
        /// </summary>
        public bool CanKnock()
        {
            return Deadwood <= 10 && Hand.Count == 10;
        }

        /// <summary>
        /// Resets player state for new round
        /// </summary>
        public void ResetForNewRound()
        {
            Melds.Clear();
            Deadwood = 0;
            HasKnocked = false;
            HasGin = false;
            RoundScore = 0;
            IsMyTurn = false;
            HasDrawn = false;
        }

        #endregion
    }
}
```

**Key Methods:**
- `AnalyzeHand()`: Uses `MeldDetector` to find melds and deadwood
- `CanKnock()`: Checks if knock is legal
- `ResetForNewRound()`: Prepares for next round

### Step 3.2: GinRummyNPCPlayer

**Create:** `Core/Game/GinRummy/Players/GinRummyNPCPlayer.cs`

```csharp
using CardsFramework.Game;

namespace GinRummy
{
    /// <summary>
    /// NPC-controlled Gin Rummy player
    /// </summary>
    public class GinRummyNPCPlayer : GinRummyPlayer
    {
        /// <summary>
        /// NPC decision-making component
        /// </summary>
        public GinRummyNPC NPC { get; private set; }

        public GinRummyNPCPlayer(string name, CardsGame game)
            : base(name, game)
        {
            NPC = new GinRummyNPC(this);
        }
    }
}
```

Simple wrapper - the NPC logic will be in a separate class.

---

## Part 4: NPC Implementation

### Step 4.1: Intermediate NPC

**Create:** `Core/Game/GinRummy/AI/GinRummyNPC.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using CardsFramework.Cards;

namespace GinRummy
{
    /// <summary>
    /// Intermediat NPC for Gin Rummy
    /// </summary>
    public class GinRummyNPC
    {
        private GinRummyPlayer player;
        private Random random;

        public GinRummyNPC(GinRummyPlayer player)
        {
            this.player = player;
            this.random = new Random();
        }

        /// <summary>
        /// Decides whether to draw from stock or discard pile
        /// </summary>
        public bool ShouldDrawFromDiscard(TraditionalCard topDiscard)
        {
            if (topDiscard == null)
                return false;

            // Simulate adding this card to hand
            List<TraditionalCard> testHand = GetCurrentHandCards();
            testHand.Add(topDiscard);

            // Find melds with this card
            var meldsWithDiscard = MeldDetector.FindOptimalMelds(testHand);
            int deadwoodWithDiscard = MeldDetector.CalculateDeadwood(testHand, meldsWithDiscard);

            // Compare to current deadwood
            return deadwoodWithDiscard < player.Deadwood;
        }

        /// <summary>
        /// Decides which card to discard
        /// </summary>
        public TraditionalCard SelectCardToDiscard()
        {
            List<TraditionalCard> handCards = GetCurrentHandCards();

            // Analyze hand
            var melds = MeldDetector.FindOptimalMelds(handCards);
            var meldedCards = GetMeldedCards(melds);

            // Get deadwood cards
            var deadwoodCards = handCards.Where(card => !meldedCards.Contains(card)).ToList();

            if (deadwoodCards.Count > 0)
            {
                // Discard highest value deadwood card
                return deadwoodCards.OrderByDescending(card => Meld.GetCardPoints(card)).First();
            }
            else
            {
                // No deadwood - discard card that breaks the smallest meld
                return SelectCardFromMelds(melds);
            }
        }

        /// <summary>
        /// Decides whether to knock
        /// </summary>
        public bool ShouldKnock()
        {
            // Knock if deadwood is very low
            if (player.HasGin)
                return true;

            if (player.Deadwood <= 5)
                return true;

            // Knock with higher deadwood with some probability
            if (player.Deadwood <= 10)
            {
                // 50% chance if deadwood is 6-10
                return random.NextDouble() < 0.5;
            }

            return false;
        }

        /// <summary>
        /// Evaluates how useful a card is for melds
        /// </summary>
        private int EvaluateCardUsefulness(TraditionalCard card, List<TraditionalCard> hand)
        {
            int usefulness = 0;

            // Check for potential sets (same rank)
            int sameRankCount = hand.Count(c => c.Value == card.Value);
            usefulness += sameRankCount * 10;

            // Check for potential runs (consecutive same suit)
            var sameSuit = hand.Where(c => c.Type == card.Type).ToList();

            foreach (var otherCard in sameSuit)
            {
                int valueDiff = Math.Abs(
                    GetCardNumericValue(card) - GetCardNumericValue(otherCard));

                if (valueDiff == 1)
                    usefulness += 15; // Adjacent card
                else if (valueDiff == 2)
                    usefulness += 5;  // One card away
            }

            return usefulness;
        }

        /// <summary>
        /// Selects a card to discard from melds (when no deadwood)
        /// </summary>
        private TraditionalCard SelectCardFromMelds(List<Meld> melds)
        {
            // Find the smallest meld
            var smallestMeld = melds.OrderBy(m => m.Cards.Count).First();

            // Discard highest value card from smallest meld
            return smallestMeld.Cards.OrderByDescending(card => Meld.GetCardPoints(card)).First();
        }

        /// <summary>
        /// Gets all cards currently in melds
        /// </summary>
        private HashSet<TraditionalCard> GetMeldedCards(List<Meld> melds)
        {
            var meldedCards = new HashSet<TraditionalCard>();

            foreach (var meld in melds)
            {
                foreach (var card in meld.Cards)
                {
                    meldedCards.Add(card);
                }
            }

            return meldedCards;
        }

        /// <summary>
        /// Gets current hand as list
        /// </summary>
        private List<TraditionalCard> GetCurrentHandCards()
        {
            List<TraditionalCard> cards = new List<TraditionalCard>();

            for (int i = 0; i < player.Hand.Count; i++)
            {
                cards.Add(player.Hand[i]);
            }

            return cards;
        }

        /// <summary>
        /// Gets numeric value for card
        /// </summary>
        private int GetCardNumericValue(TraditionalCard card)
        {
            switch (card.Value)
            {
                case CardValue.Ace: return 1;
                case CardValue.Two: return 2;
                case CardValue.Three: return 3;
                case CardValue.Four: return 4;
                case CardValue.Five: return 5;
                case CardValue.Six: return 6;
                case CardValue.Seven: return 7;
                case CardValue.Eight: return 8;
                case CardValue.Nine: return 9;
                case CardValue.Ten: return 10;
                case CardValue.Jack: return 11;
                case CardValue.Queen: return 12;
                case CardValue.King: return 13;
                default: return 0;
            }
        }
    }
}
```

**NPC Intelligence Strategy:**
1. **Drawing:** Takes discard if it reduces deadwood
2. **Discarding:** Discards highest-value deadwood card
3. **Knocking:** Knocks with Gin or very low deadwood, probabilistic for medium deadwood

This creates a competent but not unbeatable opponent.

---

## Part 5: Game Rules

### Step 5.1: Knock Rule

**Create:** `Core/Game/GinRummy/Rules/KnockRule.cs`

```csharp
using System;
using CardsFramework.Rules;

namespace GinRummy
{
    public class KnockEventArgs : EventArgs
    {
        public GinRummyPlayer Player { get; set; }
    }

    /// <summary>
    /// Rule that fires when a player knocks
    /// </summary>
    public class KnockRule : GameRule
    {
        private readonly GinRummyCardGame game;

        public KnockRule(GinRummyCardGame game)
        {
            this.game = game;
        }

        public override void Check()
        {
            foreach (var player in game.Players)
            {
                if (player is GinRummyPlayer ginPlayer)
                {
                    if (ginPlayer.HasKnocked && !ginPlayer.HasGin)
                    {
                        FireRuleMatch(new KnockEventArgs { Player = ginPlayer });
                        return;
                    }
                }
            }
        }
    }
}
```

### Step 5.2: Gin Rule

**Create:** `Core/Game/GinRummy/Rules/GinRule.cs`

```csharp
using System;
using CardsFramework.Rules;

namespace GinRummy
{
    public class GinEventArgs : EventArgs
    {
        public GinRummyPlayer Player { get; set; }
    }

    /// <summary>
    /// Rule that fires when a player gets Gin
    /// </summary>
    public class GinRule : GameRule
    {
        private readonly GinRummyCardGame game;

        public GinRule(GinRummyCardGame game)
        {
            this.game = game;
        }

        public override void Check()
        {
            foreach (var player in game.Players)
            {
                if (player is GinRummyPlayer ginPlayer)
                {
                    if (ginPlayer.HasGin)
                    {
                        FireRuleMatch(new GinEventArgs { Player = ginPlayer });
                        return;
                    }
                }
            }
        }
    }
}
```

### Step 5.3: Turn Complete Rule

**Create:** `Core/Game/GinRummy/Rules/TurnCompleteRule.cs`

```csharp
using System;
using CardsFramework.Rules;

namespace GinRummy
{
    public class TurnCompleteEventArgs : EventArgs
    {
        public GinRummyPlayer Player { get; set; }
    }

    /// <summary>
    /// Rule that fires when a player completes their turn
    /// </summary>
    public class TurnCompleteRule : GameRule
    {
        private readonly GinRummyCardGame game;
        private int previousHandCount = -1;

        public TurnCompleteRule(GinRummyCardGame game)
        {
            this.game = game;
        }

        public override void Check()
        {
            var currentPlayer = game.GetCurrentGinRummyPlayer();

            if (currentPlayer != null && currentPlayer.IsMyTurn)
            {
                // Turn is complete when player has drawn and discarded
                // (hand back to 10 cards after temporarily having 11)
                if (currentPlayer.HasDrawn && currentPlayer.Hand.Count == 10)
                {
                    if (previousHandCount == 11)
                    {
                        FireRuleMatch(new TurnCompleteEventArgs { Player = currentPlayer });
                        previousHandCount = 10;
                    }
                }
                else if (currentPlayer.Hand.Count == 11)
                {
                    previousHandCount = 11;
                }
            }
        }
    }
}
```

**How It Works:**
- Detects when hand goes from 11 cards (after draw) to 10 cards (after discard)
- Signals turn completion to advance to next player

---

## Part 6: Main Game Class (Part 1/3)

### Step 6.1: Fields and Initialization

**Create:** `Core/Game/GinRummy/Game/GinRummyCardGame.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CardsFramework.Cards;
using CardsFramework.Game;
using CardsFramework.Players;
using CardsFramework.UI;
using CardsFramework.Rules;
using CardsFramework.Core;

namespace GinRummy
{
    public class GinRummyCardGame : CardsGame
    {
        #region Fields

        // Game state
        private GinRummyGameState currentState;
        public GinRummyGameState State
        {
            get { return currentState; }
            set { currentState = value; }
        }

        // Stock and discard piles
        private List<TraditionalCard> discardPile;
        public TraditionalCard TopDiscard
        {
            get { return discardPile.Count > 0 ? discardPile[discardPile.Count - 1] : null; }
        }

        // UI Components
        private List<AnimatedHandGameComponent> animatedHands;
        private AnimatedCardsGameComponent discardPileComponent;
        private DeckDisplayComponent stockPileComponent;

        private Button buttonDrawStock;
        private Button buttonDrawDiscard;
        private Button buttonKnock;
        private Button buttonGin;
        private Button buttonNewRound;

        // Current turn management
        private int currentPlayerIndex;

        // Game rules
        private KnockRule knockRule;
        private GinRule ginRule;
        private TurnCompleteRule turnCompleteRule;

        // Display
        private SpriteFont gameFont;
        private string statusText;

        // Screen management
        private ScreenManager screenManager;

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new Gin Rummy game
        /// </summary>
        /// <param name="gameTable">The game table for layout</param>
        /// <param name="screenManager">The screen manager for SpriteBatch and Content</param>
        public GinRummyCardGame(GameTable gameTable, ScreenManager screenManager)
            : base(
                decks: 1,
                jokersInDeck: 0,
                suits: CardSuit.AllSuits,
                cardValues: CardValue.NonJokers,
                minimumPlayers: 2,
                maximumPlayers: 4,
                gameTable: gameTable,
                theme: "Default")
        {
            this.screenManager = screenManager;
            discardPile = new List<TraditionalCard>();
            animatedHands = new List<AnimatedHandGameComponent>();
            currentPlayerIndex = 0;
            statusText = "";
        }

        public override void Initialize()
        {
            base.Initialize();
            currentState = GinRummyGameState.Dealing;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // Use font from ScreenManager
            gameFont = screenManager.Font;

            // Get input state for buttons
            InputState input = new InputState();

            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;
            int buttonWidth = 200;
            int buttonHeight = 60;

            // Create buttons using the actual Button constructor
            // Buttons use texture names, not Texture2D objects

            // Draw Stock button
            buttonDrawStock = new Button(
                "ButtonRegular",
                "ButtonPressed",
                input,
                this,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation
            );
            buttonDrawStock.Text = "Draw from Stock";
            buttonDrawStock.Font = gameFont;
            buttonDrawStock.Bounds = new Rectangle(screenWidth / 2 - buttonWidth - 120, screenHeight - 150, buttonWidth, buttonHeight);
            buttonDrawStock.Click += ButtonDrawStock_Click;
            buttonDrawStock.Visible = false;
            Game.Components.Add(buttonDrawStock);

            // Draw Discard button
            buttonDrawDiscard = new Button(
                "ButtonRegular",
                "ButtonPressed",
                input,
                this,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation
            );
            buttonDrawDiscard.Text = "Draw from Discard";
            buttonDrawDiscard.Font = gameFont;
            buttonDrawDiscard.Bounds = new Rectangle(screenWidth / 2 + 120 - buttonWidth, screenHeight - 150, buttonWidth, buttonHeight);
            buttonDrawDiscard.Click += ButtonDrawDiscard_Click;
            buttonDrawDiscard.Visible = false;
            Game.Components.Add(buttonDrawDiscard);

            // Knock button
            buttonKnock = new Button(
                "ButtonRegular",
                "ButtonPressed",
                input,
                this,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation
            );
            buttonKnock.Text = "Knock";
            buttonKnock.Font = gameFont;
            buttonKnock.Bounds = new Rectangle(screenWidth / 2 - buttonWidth / 2, screenHeight - 230, buttonWidth, buttonHeight);
            buttonKnock.Color = Color.Yellow;
            buttonKnock.Click += ButtonKnock_Click;
            buttonKnock.Visible = false;
            Game.Components.Add(buttonKnock);

            // Gin button
            buttonGin = new Button(
                "ButtonRegular",
                "ButtonPressed",
                input,
                this,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation
            );
            buttonGin.Text = "Gin!";
            buttonGin.Font = gameFont;
            buttonGin.Bounds = new Rectangle(screenWidth / 2 - buttonWidth / 2, screenHeight - 310, buttonWidth, buttonHeight);
            buttonGin.Color = Color.Green;
            buttonGin.Click += ButtonGin_Click;
            buttonGin.Visible = false;
            Game.Components.Add(buttonGin);

            // New Round button
            buttonNewRound = new Button(
                "ButtonRegular",
                "ButtonPressed",
                input,
                this,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation
            );
            buttonNewRound.Text = "New Round";
            buttonNewRound.Font = gameFont;
            buttonNewRound.Bounds = new Rectangle(screenWidth / 2 - buttonWidth / 2, screenHeight - 150, buttonWidth, buttonHeight);
            buttonNewRound.Color = Color.LightBlue;
            buttonNewRound.Click += ButtonNewRound_Click;
            buttonNewRound.Visible = false;
            Game.Components.Add(buttonNewRound);

            // Create stock pile display
            stockPileComponent = new DeckDisplayComponent(dealer, gameTable, Game);
            stockPileComponent.LoadContent();
            Game.Components.Add(stockPileComponent);

            // Initialize rules
            knockRule = new KnockRule(this);
            knockRule.RuleMatch += KnockRule_RuleMatch;
            Rules.Add(knockRule);

            ginRule = new GinRule(this);
            ginRule.RuleMatch += GinRule_RuleMatch;
            Rules.Add(ginRule);

            turnCompleteRule = new TurnCompleteRule(this);
            turnCompleteRule.RuleMatch += TurnCompleteRule_RuleMatch;
            Rules.Add(turnCompleteRule);
        }

        #endregion
```

### Step 6.2: Player Management

```csharp
        #region Player Management

        public override void AddPlayer(Player newPlayer)
        {
            if (!(newPlayer is GinRummyPlayer))
            {
                throw new ArgumentException("Player must be of type GinRummyPlayer");
            }

            base.AddPlayer(newPlayer);

            // Create animated hand for this player
            AnimatedHandGameComponent animatedHand = new AnimatedHandGameComponent(
                Players.Count - 1,           // Place/position index
                newPlayer.Hand,
                this,                        // CardsGame
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation
            );
            animatedHand.LoadContent();
            animatedHands.Add(animatedHand);
            Game.Components.Add(animatedHand);
        }

        public override Player GetCurrentPlayer()
        {
            if (Players.Count == 0)
                return null;

            return Players[currentPlayerIndex];
        }

        public GinRummyPlayer GetCurrentGinRummyPlayer()
        {
            return GetCurrentPlayer() as GinRummyPlayer;
        }

        private void NextPlayer()
        {
            var currentPlayer = GetCurrentGinRummyPlayer();
            if (currentPlayer != null)
            {
                currentPlayer.IsMyTurn = false;
                currentPlayer.HasDrawn = false;
            }

            currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;

            var nextPlayer = GetCurrentGinRummyPlayer();
            if (nextPlayer != null)
            {
                nextPlayer.IsMyTurn = true;
            }

            statusText = $"{nextPlayer.Name}'s turn";
        }

        #endregion
```

### Step 6.3: Dealing

```csharp
        #region Card Management

        public override void Deal()
        {
            // Shuffle deck
            dealer.Shuffle();

            // Deal 10 cards to each player
            for (int i = 0; i < 10; i++)
            {
                foreach (var player in Players)
                {
                    dealer[0].MoveToHand(player.Hand);
                }
            }

            // Flip top card to start discard pile
            TraditionalCard topCard = dealer[0];
            discardPile.Add(topCard);

            // Create discard pile component
            Vector2 discardPosition = new Vector2(
                GraphicsDevice.Viewport.Width / 2 + 100,
                GraphicsDevice.Viewport.Height / 2
            );

            discardPileComponent = new AnimatedCardsGameComponent(
                topCard,
                this,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation
            );
            discardPileComponent.CurrentPosition = discardPosition;
            discardPileComponent.IsFaceDown = false;
            discardPileComponent.LoadContent();
            Game.Components.Add(discardPileComponent);

            // Analyze all hands
            foreach (var player in Players)
            {
                if (player is GinRummyPlayer ginPlayer)
                {
                    ginPlayer.AnalyzeHand();
                }
            }

            // Start first player's turn
            var firstPlayer = GetCurrentGinRummyPlayer();
            if (firstPlayer != null)
            {
                firstPlayer.IsMyTurn = true;
                statusText = $"{firstPlayer.Name}'s turn";
            }

            currentState = GinRummyGameState.Drawing;
        }

        public void DrawFromStock()
        {
            var currentPlayer = GetCurrentGinRummyPlayer();
            if (currentPlayer == null || !currentPlayer.IsMyTurn)
                return;

            if (dealer.Count > 0)
            {
                TraditionalCard card = dealer[0];
                card.MoveToHand(currentPlayer.Hand);
                currentPlayer.HasDrawn = true;
                currentPlayer.AnalyzeHand();

                currentState = GinRummyGameState.Discarding;
            }
        }

        public void DrawFromDiscard()
        {
            var currentPlayer = GetCurrentGinRummyPlayer();
            if (currentPlayer == null || !currentPlayer.IsMyTurn)
                return;

            if (discardPile.Count > 0)
            {
                TraditionalCard card = discardPile[discardPile.Count - 1];
                discardPile.RemoveAt(discardPile.Count - 1);
                card.MoveToHand(currentPlayer.Hand);
                currentPlayer.HasDrawn = true;
                currentPlayer.AnalyzeHand();

                // Update discard pile visual
                UpdateDiscardPileVisual();

                currentState = GinRummyGameState.Discarding;
            }
        }

        public void DiscardCard(TraditionalCard card)
        {
            var currentPlayer = GetCurrentGinRummyPlayer();
            if (currentPlayer == null || !currentPlayer.IsMyTurn || !currentPlayer.HasDrawn)
                return;

            // Remove from hand
            currentPlayer.Hand.LostCard -= null; // Detach events if any
            discardPile.Add(card);

            // Update visual
            UpdateDiscardPileVisual();

            // Analyze hand after discard
            currentPlayer.AnalyzeHand();

            // Check if player can knock or has gin
            if (currentPlayer.HasGin)
            {
                currentState = GinRummyGameState.Gin;
            }
            else
            {
                currentState = GinRummyGameState.Waiting;
            }
        }

        private void UpdateDiscardPileVisual()
        {
            if (discardPileComponent != null)
            {
                Game.Components.Remove(discardPileComponent);
            }

            if (TopDiscard != null)
            {
                Vector2 discardPosition = new Vector2(
                    GraphicsDevice.Viewport.Width / 2 + 100,
                    GraphicsDevice.Viewport.Height / 2
                );

                discardPileComponent = new AnimatedCardsGameComponent(TopDiscard, Game);
                discardPileComponent.CurrentPosition = discardPosition;
                discardPileComponent.IsFaceDown = false;
                discardPileComponent.LoadContent();
                Game.Components.Add(discardPileComponent);
            }
        }

        #endregion
```

---

## Part 7: Main Game Class (Part 2/3) - Game Flow

```csharp
        #region Game Flow

        public override void StartPlaying()
        {
            currentState = GinRummyGameState.Dealing;
            Deal();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            CheckRules();
            UpdateUIForState();
            ProcessAITurns(gameTime);
        }

        private void UpdateUIForState()
        {
            // Hide all buttons initially
            buttonDrawStock.Visible = false;
            buttonDrawDiscard.Visible = false;
            buttonKnock.Visible = false;
            buttonGin.Visible = false;
            buttonNewRound.Visible = false;

            var currentPlayer = GetCurrentGinRummyPlayer();

            switch (currentState)
            {
                case GinRummyGameState.Drawing:
                    // Only show draw buttons for human player
                    if (currentPlayer != null && !(currentPlayer is GinRummyNPCPlayer))
                    {
                        buttonDrawStock.Visible = true;
                        buttonDrawDiscard.Visible = (TopDiscard != null);
                    }
                    break;

                case GinRummyGameState.Discarding:
                    // Show knock/gin buttons if eligible
                    if (currentPlayer != null && !(currentPlayer is GinRummyNPCPlayer))
                    {
                        if (currentPlayer.HasGin)
                        {
                            buttonGin.Visible = true;
                        }
                        else if (currentPlayer.CanKnock())
                        {
                            buttonKnock.Visible = true;
                        }

                        statusText = $"{currentPlayer.Name}: Select a card to discard";
                    }
                    break;

                case GinRummyGameState.RoundEnd:
                    buttonNewRound.Visible = true;
                    break;
            }
        }

        private void ProcessAITurns(GameTime gameTime)
        {
            var currentPlayer = GetCurrentGinRummyPlayer();

            if (currentPlayer == null || !(currentPlayer is GinRummyNPCPlayer))
                return;

            GinRummyNPCPlayer NPCPlayer = (GinRummyNPCPlayer)currentPlayer;

            // NPC drawing phase
            if (currentState == GinRummyGameState.Drawing)
            {
                // Small delay for realism
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
                {
                    if (NPCPlayer.AI.ShouldDrawFromDiscard(TopDiscard))
                    {
                        DrawFromDiscard();
                    }
                    else
                    {
                        DrawFromStock();
                    }
                });
            }
            // NPC discarding phase
            else if (currentState == GinRummyGameState.Discarding)
            {
                // Check if NPC should knock
                if (NPCPlayer.AI.ShouldKnock())
                {
                    if (NPCPlayer.HasGin)
                    {
                        NPCPlayer.HasGin = true;
                        currentState = GinRummyGameState.Gin;
                    }
                    else
                    {
                        NPCPlayer.HasKnocked = true;
                        currentState = GinRummyGameState.Knocked;
                    }
                    return;
                }

                // Select card to discard
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
                {
                    TraditionalCard cardToDiscard = NPCPlayer.AI.SelectCardToDiscard();
                    DiscardCard(cardToDiscard);
                });
            }
        }

        #endregion
```

---

## Part 8: Main Game Class (Part 3/3) - Event Handlers and Scoring

```csharp
        #region Event Handlers

        private void ButtonDrawStock_Click(object sender, EventArgs e)
        {
            DrawFromStock();
        }

        private void ButtonDrawDiscard_Click(object sender, EventArgs e)
        {
            DrawFromDiscard();
        }

        private void ButtonKnock_Click(object sender, EventArgs e)
        {
            var currentPlayer = GetCurrentGinRummyPlayer();
            if (currentPlayer != null && currentPlayer.CanKnock())
            {
                currentPlayer.HasKnocked = true;
                currentState = GinRummyGameState.Knocked;
            }
        }

        private void ButtonGin_Click(object sender, EventArgs e)
        {
            var currentPlayer = GetCurrentGinRummyPlayer();
            if (currentPlayer != null && currentPlayer.HasGin)
            {
                currentState = GinRummyGameState.Gin;
            }
        }

        private void ButtonNewRound_Click(object sender, EventArgs e)
        {
            ResetForNewRound();
            StartPlaying();
        }

        private void KnockRule_RuleMatch(object sender, EventArgs e)
        {
            KnockEventArgs args = (KnockEventArgs)e;
            statusText = $"{args.Player.Name} knocked!";

            currentState = GinRummyGameState.Scoring;
            CalculateScores();
        }

        private void GinRule_RuleMatch(object sender, EventArgs e)
        {
            GinEventArgs args = (GinEventArgs)e;
            statusText = $"{args.Player.Name} got Gin!";

            currentState = GinRummyGameState.Scoring;
            CalculateScores();
        }

        private void TurnCompleteRule_RuleMatch(object sender, EventArgs e)
        {
            NextPlayer();
            currentState = GinRummyGameState.Drawing;
        }

        #endregion

        #region Scoring

        private void CalculateScores()
        {
            var knocker = Players.OfType<GinRummyPlayer>().FirstOrDefault(p => p.HasKnocked || p.HasGin);

            if (knocker == null)
                return;

            // Get opponents
            var opponents = Players.OfType<GinRummyPlayer>().Where(p => p != knocker).ToList();

            if (knocker.HasGin)
            {
                // Gin: Knocker gets all opponents' deadwood + 25 bonus
                int totalOpponentDeadwood = opponents.Sum(opp => opp.Deadwood);
                knocker.RoundScore = totalOpponentDeadwood + 25;

                statusText = $"{knocker.Name} wins with Gin!\n" +
                            $"Score: {knocker.RoundScore} points";
            }
            else
            {
                // Regular knock: Check for undercut
                bool undercut = false;

                foreach (var opponent in opponents)
                {
                    if (opponent.Deadwood <= knocker.Deadwood)
                    {
                        // Undercut! Opponent wins
                        undercut = true;
                        int difference = knocker.Deadwood - opponent.Deadwood;
                        opponent.RoundScore = difference + 25;

                        statusText = $"{opponent.Name} undercuts {knocker.Name}!\n" +
                                    $"{opponent.Name} scores {opponent.RoundScore} points";
                        break;
                    }
                }

                if (!undercut)
                {
                    // Knocker wins
                    int totalDifference = opponents.Sum(opp => opp.Deadwood) - knocker.Deadwood;
                    knocker.RoundScore = totalDifference;

                    statusText = $"{knocker.Name} wins!\n" +
                                $"Score: {knocker.RoundScore} points";
                }
            }

            currentState = GinRummyGameState.RoundEnd;
        }

        private void ResetForNewRound()
        {
            // Clear discard pile
            discardPile.Clear();

            // Remove visual components
            if (discardPileComponent != null)
            {
                Game.Components.Remove(discardPileComponent);
            }

            // Reset all players
            foreach (var player in Players)
            {
                if (player is GinRummyPlayer ginPlayer)
                {
                    // Clear hand
                    while (ginPlayer.Hand.Count > 0)
                    {
                        ginPlayer.Hand[0].MoveToHand(dealer);
                    }

                    ginPlayer.ResetForNewRound();
                }
            }

            // Reshuffle dealer
            dealer.Shuffle();

            currentPlayerIndex = 0;
            statusText = "";
        }

        #endregion

        #region Utilities

        public override int CardValue(TraditionalCard card)
        {
            return Meld.GetCardPoints(card);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw status text
            if (!string.IsNullOrEmpty(statusText) && gameFont != null)
            {
                SpriteBatch spriteBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));

                if (spriteBatch != null)
                {
                    Vector2 position = new Vector2(20, 20);

                    // Draw with shadow
                    spriteBatch.DrawString(gameFont, statusText, position + new Vector2(2, 2), Color.Black);
                    spriteBatch.DrawString(gameFont, statusText, position, Color.White);
                }
            }
        }

        #endregion
    }
}
```

---

## Part 9: UI Enhancement - Hand Organizer

**Create:** `Core/Game/GinRummy/UI/HandOrganizer.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using CardsFramework.Cards;

namespace GinRummy
{
    /// <summary>
    /// Organizes cards in hand for better visualization
    /// </summary>
    public class HandOrganizer
    {
        /// <summary>
        /// Sorts and groups cards for display
        /// </summary>
        public static List<TraditionalCard> OrganizeHand(List<TraditionalCard> cards, List<Meld> melds)
        {
            List<TraditionalCard> organized = new List<TraditionalCard>();

            // Get cards in melds
            var meldedCards = new HashSet<TraditionalCard>();
            foreach (var meld in melds)
            {
                foreach (var card in meld.Cards)
                {
                    meldedCards.Add(card);
                }
            }

            // Add melded cards first (grouped by meld)
            foreach (var meld in melds)
            {
                organized.AddRange(meld.Cards);
            }

            // Add remaining cards (deadwood) sorted
            var deadwood = cards.Where(c => !meldedCards.Contains(c))
                               .OrderBy(c => c.Type)
                               .ThenBy(c => GetCardNumericValue(c))
                               .ToList();

            organized.AddRange(deadwood);

            return organized;
        }

        private static int GetCardNumericValue(TraditionalCard card)
        {
            switch (card.Value)
            {
                case CardValue.Ace: return 1;
                case CardValue.Two: return 2;
                case CardValue.Three: return 3;
                case CardValue.Four: return 4;
                case CardValue.Five: return 5;
                case CardValue.Six: return 6;
                case CardValue.Seven: return 7;
                case CardValue.Eight: return 8;
                case CardValue.Nine: return 9;
                case CardValue.Ten: return 10;
                case CardValue.Jack: return 11;
                case CardValue.Queen: return 12;
                case CardValue.King: return 13;
                default: return 0;
            }
        }

        /// <summary>
        /// Calculates card positions for visual display
        /// </summary>
        public static List<Vector2> CalculateCardPositions(
            int cardCount,
            Vector2 startPosition,
            float cardSpacing,
            List<int> meldBreaks = null)
        {
            List<Vector2> positions = new List<Vector2>();

            float currentX = startPosition.X;

            for (int i = 0; i < cardCount; i++)
            {
                positions.Add(new Vector2(currentX, startPosition.Y));

                // Add extra space between melds
                if (meldBreaks != null && meldBreaks.Contains(i))
                {
                    currentX += cardSpacing + 20; // Extra gap
                }
                else
                {
                    currentX += cardSpacing;
                }
            }

            return positions;
        }
    }
}
```

This helper organizes cards visually, grouping melds together and separating deadwood.

---

## Part 10: Screen Integration

### Step 10.1: Create Gameplay Screen

**Create:** `Core/Game/Screens/GinRummyGameplayScreen.cs`

```csharp
using System;
using Microsoft.Xna.Framework;
using GinRummy;
using CardsFramework.UI;
using CardsFramework.Core;

namespace CardsStarterKit
{
    public class GinRummyGameplayScreen : GameScreen
    {
        private GinRummyCardGame ginRummyGame;

        public GinRummyGameplayScreen()
        {
            EnabledGestures = Microsoft.Xna.Framework.Input.Touch.GestureType.Tap;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // Create game table (4 player positions)
            GameTable gameTable = new GameTable(ScreenManager.Game, 4);

            // Create game
            ginRummyGame = new GinRummyCardGame(gameTable, ScreenManager);
            ScreenManager.Game.Components.Add(ginRummyGame);
            ginRummyGame.Initialize();
            ginRummyGame.LoadContent();

            // Add players
            GinRummyPlayer humanPlayer = new GinRummyPlayer("You", ginRummyGame);
            ginRummyGame.AddPlayer(humanPlayer);

            GinRummyNPCPlayer npc1 = new GinRummyNPCPlayer("NPC 1", ginRummyGame);
            ginRummyGame.AddPlayer(npc1);

            GinRummyNPCPlayer npc2 = new GinRummyNPCPlayer("NPC 2", ginRummyGame);
            ginRummyGame.AddPlayer(npc2);

            // Start game
            ginRummyGame.StartPlaying();
        }

        public override void HandleInput(InputState input)
        {
            base.HandleInput(input);

            if (input.IsPauseGame(null))
            {
                ScreenManager.AddScreen(new PauseScreen(), null);
            }

            // Handle card selection for discarding
            HandleCardSelection(input);
        }

        private void HandleCardSelection(InputState input)
        {
            if (ginRummyGame.State != GinRummyGameState.Discarding)
                return;

            var currentPlayer = ginRummyGame.GetCurrentGinRummyPlayer();

            if (currentPlayer == null || currentPlayer is GinRummyNPCPlayer)
                return;

            // Check for tap on cards in hand
            // (Implementation depends on your touch/mouse handling)
            // You would iterate through animated hand components and check bounds
        }

        public override void UnloadContent()
        {
            if (ginRummyGame != null)
            {
                ScreenManager.Game.Components.Remove(ginRummyGame);
            }

            base.UnloadContent();
        }
    }
}
```

### Step 10.2: Add Menu Entry

**Modify:** `Core/Game/Screens/MainMenuScreen.cs`

```csharp
// Add in constructor
MenuEntry ginRummyMenuEntry = new MenuEntry("Gin Rummy");
ginRummyMenuEntry.Selected += GinRummyMenuEntrySelected;
menuEntries.Add(ginRummyMenuEntry);

// Add event handler
private void GinRummyMenuEntrySelected(object sender, EventArgs e)
{
    ScreenManager.AddScreen(new GinRummyGameplayScreen(), null);
}
```

---

## Part 11: Testing

### Step 11.1: Build and Run

```bash
dotnet build
# For macOS/Linux
dotnet run --project Platforms/DesktopGL/CardsStarterKit.DesktopGL.csproj
# For Windows
dotnet run --project Platforms/WindowsDX/CardsStarterKit.WindowsDX.csproj
```

### Step 11.2: Test Scenarios

1. **Basic Gameplay:**
   - Draw from stock/discard
   - Discard cards
   - Verify turn rotation

2. **Meld Detection:**
   - Check that sets are recognized (3 7s, etc.)
   - Check that runs are recognized (4♠ 5♠ 6♠)
   - Verify deadwood calculation

3. **Knocking:**
   - Get deadwood ≤ 10 and test knock
   - Verify scoring

4. **Gin:**
   - Form all melds (0 deadwood)
   - Verify Gin declaration and bonus

5. **NPC Behavior:**
   - Watch NPC draw and discard decisions
   - Verify NPC knocks appropriately

---

## Part 12: Enhancements and Next Steps

### Enhancement 1: Card Clicking for Human Player

Add touch/mouse handling in the screen to let human players click cards to discard:

```csharp
private void HandleCardSelection(InputState input)
{
    // Get tap position
    foreach (var gesture in input.Gestures)
    {
        if (gesture.GestureType == GestureType.Tap)
        {
            // Check which card was tapped
            // (Iterate through animated hand components and check bounds)
        }
    }
}
```

### Enhancement 2: Visual Meld Highlighting

Highlight cards that are part of melds:

```csharp
// In AnimatedHandGameComponent or custom renderer:
foreach (var card in meldedCards)
{
    // Draw green border or glow effect
}
```

### Enhancement 3: Multi-Round Scoring

To extend to full matches (first to 100 points):

```csharp
// Add to GinRummyPlayer:
public int MatchScore { get; set; }

// Add game state:
public enum GinRummyGameState
{
    // ... existing states
    MatchEnd  // When someone reaches 100
}

// After each round:
knocker.MatchScore += knocker.RoundScore;

if (knocker.MatchScore >= 100)
{
    currentState = GinRummyGameState.MatchEnd;
}
```

### Enhancement 4: Laying Off Cards

After a knock, allow opponents to add cards to knocker's melds:

```csharp
public class LayOffPhase
{
    public static List<TraditionalCard> FindLayOffCards(
        List<TraditionalCard> hand,
        List<Meld> opponentMelds)
    {
        // Check each hand card to see if it extends opponent's melds
        // ...
    }
}
```

### Enhancement 5: Better NPC Intelligence

Advanced NPC improvements:
- Track discarded cards (card counting)
- Infer opponent hands from discards
- Calculate probability of completing melds
- Defensive discarding (don't give opponent useful cards)

---

## Key Takeaways

### What You Learned

1. **Complex State Management:**
   - Multi-phase turns (draw → discard)
   - Turn rotation among players
   - Handling knocking and gin conditions

2. **Meld Detection Algorithm:**
   - Finding all possible melds
   - Combinatorial optimization to minimize deadwood
   - Backtracking algorithm

3. **NPC Implementation:**
   - Evaluating card usefulness
   - Making draw/discard decisions
   - Probabilistic knocking strategy

4. **Scoring Logic:**
   - Standard knock scoring
   - Gin bonuses
   - Undercut detection

5. **Multi-Player Support:**
   - Turn management
   - Player indexing
   - Mixed human/NPC Players

### Design Patterns Used

1. **State Machine:** GinRummyGameState controls game flow
2. **Strategy Pattern:** NPC decision-making in separate class
3. **Rule Pattern:** Knock, Gin, TurnComplete rules
4. **Component Pattern:** Animated hands, cards, deck display
5. **Observer Pattern:** Event-driven rule matching

---

## Troubleshooting

### Melds Not Detected
- Debug `MeldDetector.FindOptimalMelds()`
- Print all possible melds before optimization
- Check card sorting logic

### NPC Makes Bad Moves
- Add logging to NPC decision methods
- Print deadwood calculations
- Verify card evaluation logic

### Turn Doesn't Advance
- Check `TurnCompleteRule` hand count tracking
- Verify `NextPlayer()` is called
- Debug state transitions

### Scoring Incorrect
- Print each player's deadwood
- Verify meld point calculations
- Check undercut logic

---

## Extending to Full Matches

To implement full Gin Rummy matches to 100 points:

**1. Add Match Tracking:**
```csharp
public int MatchScore { get; set; }
public int RoundsWon { get; set; }
```

**2. Add Match State:**
```csharp
public enum MatchPhase
{
    InProgress,
    Complete
}
```

**3. Check After Each Round:**
```csharp
if (winner.MatchScore >= 100)
{
    currentState = GinRummyGameState.MatchEnd;
    ShowMatchResults();
}
else
{
    PrepareNextRound();
}
```

**4. Add Bonuses:**
- Game bonus: +100 for winning match
- Shutout bonus: Additional points if opponent scored 0

---

## Related Gin Rummy Variants

Want to explore other variants? Here are some links:

**Oklahoma Gin:**
- First upcard determines knock limit (not always 10)
- https://en.wikipedia.org/wiki/Oklahoma_gin

**Hollywood Gin:**
- Multiple simultaneous games
- Wins count toward different game tracks
- https://www.pagat.com/rummy/ginrummy.html#hollywood

**Straight Gin:**
- Can only knock with Gin (0 deadwood)
- Higher scoring, faster games
- https://www.pagat.com/rummy/ginrummy.html#straight

---

## Complete File Checklist

- [ ] `Core/Game/GinRummy/Game/GinRummyGameState.cs`
- [ ] `Core/Game/GinRummy/Game/Meld.cs`
- [ ] `Core/Game/GinRummy/Game/MeldDetector.cs`
- [ ] `Core/Game/GinRummy/Game/GinRummyCardGame.cs`
- [ ] `Core/Game/GinRummy/Players/GinRummyPlayer.cs`
- [ ] `Core/Game/GinRummy/Players/GinRummyNPCPlayer.cs`
- [ ] `Core/Game/GinRummy/AI/GinRummyNPC.cs`
- [ ] `Core/Game/GinRummy/Rules/KnockRule.cs`
- [ ] `Core/Game/GinRummy/Rules/GinRule.cs`
- [ ] `Core/Game/GinRummy/Rules/TurnCompleteRule.cs`
- [ ] `Core/Game/GinRummy/UI/HandOrganizer.cs`
- [ ] `Core/Game/Screens/GinRummyGameplayScreen.cs`
- [ ] Modified `Core/Game/Screens/MainMenuScreen.cs`

---

## Conclusion

Congratulations! You've built a complete Gin Rummy implementation with:

- Full game rules (melds, knocking, gin)
- Intelligent NPC opponents
- Multi-player support
- Proper scoring
- Turn-based gameplay

This tutorial demonstrated advanced card game concepts including meld detection algorithms, NPC decision-making, and complex game state management. You're now equipped to build any card game using the CardsStarterKit framework!

**Next challenges to try:**
- Implement other Rummy variants (Rummy 500, Canasta)
- Build trick-taking games (Hearts, Spades, Bridge)
- Create shedding games (Crazy Eights, Uno-style)

Happy coding!
