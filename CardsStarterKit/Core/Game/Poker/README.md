# Poker Game Implementation Guide

This guide will help you implement a Texas Hold 'Em Poker game using the Cards Framework provided in this repository. It is intended for new developers who want to extend the existing codebase with a new card game, leveraging the modular, event-driven, and component-based architecture already in place.

## Directory Structure

- `Game/` — Core Poker game logic (game state, table, dealing, betting rounds)
- `Players/` — Player classes (human, AI, seat management)
- `Rules/` — Poker-specific rules (hand evaluation, betting logic, round progression)
- `UI/` — User interface components (screens, menus, in-game UI)
- `Misc/` — Utilities, helpers, and shared resources

## Getting Started

1. **Familiarize Yourself with the Cards Framework**
   - Review `Framework/Cards/TraditionalCard.cs`, `Framework/Cards/Hand.cs`, and `Framework/Game/CardsGame.cs`.
   - Understand how the framework models cards, hands, decks, and game flow.

2. **Set Up the Poker Game Class**
   - Create `PokerGame.cs` in `Poker/Game/` inheriting from `CardsGame`.
   - Implement game state management: lobby, dealing, betting, showdown, etc.
   - Use the event-driven model for state transitions.

3. **Implement Player Logic**
   - Create `PokerPlayer.cs` in `Poker/Players/` inheriting from `Player`.
   - Add properties for chips, current bet, folded status, etc.
   - Support both human and AI players.

4. **Define Poker Rules**
   - Create `PokerRules.cs` in `Poker/Rules/`.
   - Implement hand evaluation (pair, flush, straight, etc.).
   - Handle betting rounds: pre-flop, flop, turn, river.
   - Use the Strategy pattern for rule variations if needed.

5. **Build the UI**
   - Create screens in `Poker/UI/` (e.g., `PokerTableScreen.cs`, `PokerMenuScreen.cs`).
   - Use the existing ScreenManager for navigation.
   - Display player hands, community cards, bets, and pot.

6. **Utilities and Helpers**
   - Place shared logic (e.g., pot calculation, seat rotation) in `Poker/Misc/`.

## Key Concepts

- **Component-Based Design:** Extend or compose game components for animations, transitions, and UI.
- **Event-Driven Flow:** Use events to trigger state changes and UI updates.
- **Separation of Concerns:** Keep game logic, rules, UI, and utilities in their respective folders.

## Example: PokerGame Class Skeleton

```csharp
using Cards.Framework.Game;

namespace Poker.Game {
    public class PokerGame : CardsGame {
        // Game state, table, pot, etc.
        // ...
        public override void StartGame() {
            // Initialize table, shuffle, seat players
        }
        // ...
    }
}
```

## Tips

- Reuse as much as possible from the framework (deck, hand, player base classes).
- Study the Blackjack implementation for patterns and best practices.
- Use the MonoGame Content Pipeline for assets (cards, chips, UI elements).
- Test incrementally: start with basic dealing and round flow, then add betting and hand evaluation.

## Resources

- [MonoGame Documentation](https://docs.monogame.net/)
- [Texas Hold 'Em Rules](https://en.wikipedia.org/wiki/Texas_hold_%27em)
- [Cards Framework Overview](../Framework/)

---

Happy coding! If you have questions, check the existing Blackjack code or ask for help.
