//-----------------------------------------------------------------------------
// RulesEngine.cs
//
// Pure static validation layer. All methods return a RulesResult describing
// whether an action is legal and, if not, why.
//
// RulesEngine never mutates game state — it only reads it.
// Every execution method in WarlordsCardGame calls the corresponding Can*()
// guard and aborts if !IsLegal.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace WarlordsFramework
{
    // ─────────────────────────────────────────────────────────────────────────
    // Supporting types
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returned by every Rules Engine validation method.
    /// </summary>
    public readonly struct RulesResult
    {
        public bool   IsLegal { get; }
        public string Reason  { get; }

        public RulesResult(bool isLegal, string reason = "")
        {
            IsLegal = isLegal;
            Reason  = reason;
        }

        public static RulesResult Legal()                    => new RulesResult(true);
        public static RulesResult Illegal(string reason)     => new RulesResult(false, reason);
    }

    /// <summary>
    /// Hand-size overburden tier, evaluated at the end of each turn.
    /// </summary>
    public enum OverburdenLevel
    {
        /// <summary>Hand ≤ 13 cards — no penalty.</summary>
        None,

        /// <summary>
        /// Hand 14–15 cards — end-of-turn degen equals full regen value
        /// (passive regen + active card-skill bonuses). Both regen and degen
        /// apply in the same RegenDegen phase and do NOT cancel each other.
        /// </summary>
        Tier1_14_15,

        /// <summary>
        /// Hand 16–17 cards — Tier-1 penalty plus the player may play only
        /// one additional card and take only one character action this turn.
        /// </summary>
        Tier2_16_17,

        /// <summary>
        /// Hand ≥ 18 cards — Tier-2 penalty plus all characters that enter
        /// play this turn do so at half their maximum Soul Essence.
        /// </summary>
        Tier3_18Plus
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RulesEngine — pure static validation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stateless rules validator. Pass in snapshot values; never stored.
    /// </summary>
    public static class RulesEngine
    {
        // ── Deck / hand constants ────────────────────────────────────────
        public const int DeckSize            = 35;
        public const int OpeningHandSize     = 7;
        public const int MaxHandSize         = 13;
        public const int OverburdenTier1Min  = 14;
        public const int OverburdenTier2Min  = 16;
        public const int OverburdenTier3Min  = 18;

        // ── Hand size ────────────────────────────────────────────────────

        /// <summary>
        /// Determine the current overburden tier based on hand size.
        /// Called at start of End phase to apply penalties.
        /// </summary>
        public static OverburdenLevel EvaluateOverburden(int handSize)
        {
            if (handSize >= OverburdenTier3Min) return OverburdenLevel.Tier3_18Plus;
            if (handSize >= OverburdenTier2Min) return OverburdenLevel.Tier2_16_17;
            if (handSize >= OverburdenTier1Min) return OverburdenLevel.Tier1_14_15;
            return OverburdenLevel.None;
        }

        // ── Phase guards ─────────────────────────────────────────────────

        private static RulesResult RequirePhase(TurnTracker tracker, TurnPhase required)
        {
            if (tracker.CurrentPhase != required)
                return RulesResult.Illegal(
                    $"This action requires the {required} phase " +
                    $"(currently {tracker.CurrentPhase}).");
            return RulesResult.Legal();
        }

        // ── Card play ────────────────────────────────────────────────────

        /// <summary>
        /// Validate playing a Character card from hand into a zone.
        /// </summary>
        /// <param name="card">Card being played.</param>
        /// <param name="targetZone">Destination zone.</param>
        /// <param name="playerSide">Side of the player playing the card.</param>
        /// <param name="tracker">Current turn tracker.</param>
        /// <param name="playerSE">Player's current Soul Essence (to check cost).</param>
        /// <param name="overburden">Current overburden tier.</param>
        public static RulesResult CanPlayCharacter(
            CharacterCard  card,
            GameZone       targetZone,
            PlayerSide     playerSide,
            TurnTracker    tracker,
            int            playerSE,
            OverburdenLevel overburden = OverburdenLevel.None)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (tracker.HasPlayedCharacter)
                return RulesResult.Illegal("You may only play one Character card per turn.");

            if (targetZone.Owner != playerSide)
                return RulesResult.Illegal("Characters must be played into your own zones.");

            // By default, characters enter from the Home Base (or EnemyBase for AI side).
            // Special skills that bypass this can be layered above this rule later.
            bool isOwnHomeBase =
                (playerSide == PlayerSide.Player   && targetZone.Type == ZoneType.HomeBase) ||
                (playerSide == PlayerSide.Opponent && targetZone.Type == ZoneType.EnemyBase);
            if (!isOwnHomeBase)
                return RulesResult.Illegal("Characters must be deployed to your Home Base.");

            if (card.SoulEssenceCost > playerSE)
                return RulesResult.Illegal(
                    $"Insufficient Soul Essence (need {card.SoulEssenceCost}, have {playerSE}).");

            return RulesResult.Legal();
        }

        /// <summary>
        /// Validate playing an Item card (to a character or as a zone obstacle).
        /// </summary>
        /// <param name="item">Item being played.</param>
        /// <param name="targetCharacter">Character to equip to, or null if deploying as zone obstacle.</param>
        /// <param name="tracker">Current turn tracker.</param>
        /// <param name="playerSE">Player's current Soul Essence.</param>
        public static RulesResult CanPlayItem(
            ItemCard       item,
            CharacterCard  targetCharacter,   // pass null if deploying as zone obstacle
            TurnTracker    tracker,
            int            playerSE)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (tracker.HasPlayedItem)
                return RulesResult.Illegal("You may only play one Item card per turn.");

            if (item.SoulEssenceCost > playerSE)
                return RulesResult.Illegal(
                    $"Insufficient Soul Essence (need {item.SoulEssenceCost}, have {playerSE}).");

            if (item.RequiresCharacter && targetCharacter == null)
                return RulesResult.Illegal("This Item must be equipped to a Character.");

            return RulesResult.Legal();
        }

        /// <summary>
        /// Validate playing an Event card.
        /// </summary>
        public static RulesResult CanPlayEvent(
            EventCard   card,
            TurnTracker tracker,
            int         playerSE)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (tracker.HasPlayedEvent)
                return RulesResult.Illegal("You may only play one Event card per turn.");

            if (card.SoulEssenceCost > playerSE)
                return RulesResult.Illegal(
                    $"Insufficient Soul Essence (need {card.SoulEssenceCost}, have {playerSE}).");

            return RulesResult.Legal();
        }

        /// <summary>
        /// Validate attempting to place / contest a Terrain card on the battlefield.
        /// The opponent has an opportunity to play a counter-Terrain from hand before
        /// the terrain is confirmed; that counter window is handled by game flow, not here.
        /// </summary>
        /// <param name="terrain">Terrain card being played.</param>
        /// <param name="targetZone">Zone where terrain is being contested.</param>
        /// <param name="playerSide">Side initiating the terrain attempt.</param>
        /// <param name="tracker">Current turn tracker.</param>
        /// <param name="playerSE">Player's current Soul Essence.</param>
        public static RulesResult CanAttemptTerrainSet(
            TerrainCard terrain,
            GameZone    targetZone,
            PlayerSide  playerSide,
            TurnTracker tracker,
            int         playerSE)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (tracker.HasPlayedTerrain)
                return RulesResult.Illegal("You may only attempt to set terrain once per turn.");

            if (terrain.SoulEssenceCost > playerSE)
                return RulesResult.Illegal(
                    $"Insufficient Soul Essence (need {terrain.SoulEssenceCost}, have {playerSE}).");

            // Terrain must target the shared Battlefield zones, not Home Bases.
            bool targetIsSharedBattlefield =
                targetZone.Type == ZoneType.Battlefield ||
                targetZone.Type == ZoneType.EnemyBattlefield;

            if (!targetIsSharedBattlefield)
                return RulesResult.Illegal("Terrain may only be placed on the Battlefield.");

            return RulesResult.Legal();
        }

        // ── Character actions ────────────────────────────────────────────

        /// <summary>
        /// Validate a character moving from one zone to an adjacent zone.
        /// Zone clearance (chars + zone-obstacle items with DeployedSE > 0) is
        /// already captured in <see cref="GameZone.IsClearForAdvance"/>.
        /// </summary>
        /// <param name="character">Character attempting to move.</param>
        /// <param name="fromZone">Zone the character is currently in.</param>
        /// <param name="toZone">Zone the character wants to move into.</param>
        /// <param name="tracker">Current turn tracker.</param>
        public static RulesResult CanMoveCharacter(
            CharacterCard character,
            GameZone      fromZone,
            GameZone      toZone,
            TurnTracker   tracker)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (character.ActionThisTurn != CharacterAction.None)
                return RulesResult.Illegal(
                    $"{character.Name} has already acted this turn " +
                    $"({character.ActionThisTurn}).");

            if (!fromZone.Characters.Contains(character))
                return RulesResult.Illegal(
                    $"{character.Name} is not in the specified source zone.");

            // Determine if this is an advance or a retreat.
            bool isAdvance  = IsAdvanceMove(character, fromZone, toZone);
            bool isRetreat  = IsRetreatMove(character, fromZone, toZone);

            if (!isAdvance && !isRetreat)
                return RulesResult.Illegal("That is not a valid adjacent zone for movement.");

            // Characters may never enter the Enemy Home Base zone directly.
            if (toZone.Type == ZoneType.EnemyBase && fromZone.Owner == PlayerSide.Player)
                return RulesResult.Illegal(
                    "Characters cannot enter the Enemy Home Base. " +
                    "Attack the Warlord directly from the Enemy Battlefield.");

            if (toZone.Type == ZoneType.HomeBase && fromZone.Owner == PlayerSide.Opponent)
                return RulesResult.Illegal(
                    "Characters cannot enter the Player Home Base. " +
                    "Attack the Warlord directly from the Player Battlefield.");

            if (isAdvance)
            {
                // Advancing into an opponent-controlled zone requires it to be clear.
                bool advancingIntoOpponentZone =
                    (fromZone.Owner == PlayerSide.Player   && toZone.Owner == PlayerSide.Opponent) ||
                    (fromZone.Owner == PlayerSide.Opponent && toZone.Owner == PlayerSide.Player);

                if (advancingIntoOpponentZone && !toZone.IsClearForAdvance)
                    return RulesResult.Illegal(
                        "The target zone must be clear of characters and obstacle items " +
                        "before you can advance into it.");

                // Advancing from own Home Base to own Battlefield requires terrain to be set.
                bool advancingFromHomeBaseToOwnBattlefield =
                    (fromZone.Type == ZoneType.HomeBase     && toZone.Type == ZoneType.Battlefield      && fromZone.Owner == PlayerSide.Player) ||
                    (fromZone.Type == ZoneType.EnemyBase    && toZone.Type == ZoneType.EnemyBattlefield && fromZone.Owner == PlayerSide.Opponent);

                if (advancingFromHomeBaseToOwnBattlefield && !toZone.HasTerrainBeenSet)
                    return RulesResult.Illegal(
                        "Terrain must be established on the Battlefield before " +
                        "characters can advance from the Home Base.");
            }

            if (isRetreat && !character.CanRetreat)
                return RulesResult.Illegal($"{character.Name} cannot retreat.");

            return RulesResult.Legal();
        }

        /// <summary>
        /// Validate a character attacking another character.
        /// Attacker must be in a zone adjacent to (and facing) the target's zone.
        /// </summary>
        public static RulesResult CanAttackCharacter(
            CharacterCard attacker,
            CharacterCard target,
            PlayingField  field,
            TurnTracker   tracker)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (attacker.ActionThisTurn != CharacterAction.None)
                return RulesResult.Illegal(
                    $"{attacker.Name} has already acted this turn.");

            if (target.IsDefeated)
                return RulesResult.Illegal("Target character is already defeated.");

            var attackerZone = field.GetZoneContaining(attacker);
            var targetZone   = field.GetZoneContaining(target);

            if (attackerZone == null)
                return RulesResult.Illegal("Attacker is not on the field.");
            if (targetZone == null)
                return RulesResult.Illegal("Target is not on the field.");

            if (!AreOpposingAdjacentZones(attackerZone, targetZone))
                return RulesResult.Illegal(
                    "Attacker and target must be in opposing adjacent zones.");

            return RulesResult.Legal();
        }

        /// <summary>
        /// Validate a character attacking the opponent's Warlord directly.
        /// Requires attacker to be in the Enemy Battlefield zone AND the
        /// Enemy Home Base to be fully clear (no characters, no obstacle items).
        /// </summary>
        public static RulesResult CanAttackWarlord(
            CharacterCard attacker,
            PlayerSide    attackerSide,
            PlayingField  field,
            TurnTracker   tracker)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (attacker.ActionThisTurn != CharacterAction.None)
                return RulesResult.Illegal(
                    $"{attacker.Name} has already acted this turn.");

            var attackerZone = field.GetZoneContaining(attacker);
            if (attackerZone == null)
                return RulesResult.Illegal("Attacker is not on the field.");

            GameZone enemyHomeBase;
            if (attackerSide == PlayerSide.Player)
            {
                if (attackerZone.Type != ZoneType.EnemyBattlefield)
                    return RulesResult.Illegal(
                        "Player characters must be in the Enemy Battlefield to attack the Warlord.");
                enemyHomeBase = field.OpponentBase;
            }
            else
            {
                if (attackerZone.Type != ZoneType.Battlefield)
                    return RulesResult.Illegal(
                        "Opponent characters must be in the Player Battlefield to attack the Warlord.");
                enemyHomeBase = field.PlayerHomeBase;
            }

            if (!enemyHomeBase.IsClearForAdvance)
                return RulesResult.Illegal(
                    "The enemy Home Base must be clear of characters and obstacle items " +
                    "before you can attack the Warlord directly.");

            return RulesResult.Legal();
        }

        /// <summary>
        /// Validate a character setting its action to Defend.
        /// </summary>
        public static RulesResult CanDefend(CharacterCard character, TurnTracker tracker)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (character.ActionThisTurn != CharacterAction.None)
                return RulesResult.Illegal(
                    $"{character.Name} has already acted this turn ({character.ActionThisTurn}).");

            return RulesResult.Legal();
        }

        // ── Sacrifice ────────────────────────────────────────────────────

        /// <summary>
        /// Validate sacrificing a card for its current Soul Essence.
        /// Only Character cards yield SE on sacrifice; other card types yield 0.
        /// All card types may be sacrificed during Main phase.
        /// </summary>
        public static RulesResult CanSacrifice(WarlordsCard card, TurnTracker tracker)
        {
            var phase = RequirePhase(tracker, TurnPhase.Main);
            if (!phase.IsLegal) return phase;

            if (card == null)
                return RulesResult.Illegal("No card specified for sacrifice.");

            return RulesResult.Legal();
        }

        // ── Draw ─────────────────────────────────────────────────────────

        /// <summary>
        /// Validate drawing a card during the Draw phase.
        /// Drawing is optional (once per turn).
        /// </summary>
        public static RulesResult CanDraw(TurnTracker tracker, int deckSize)
        {
            if (tracker.CurrentPhase != TurnPhase.Draw)
                return RulesResult.Illegal("You may only draw during the Draw phase.");

            if (tracker.HasDrawnThisTurn)
                return RulesResult.Illegal("You may only draw once per turn.");

            if (deckSize == 0)
                return RulesResult.Illegal("Your deck is empty.");

            return RulesResult.Legal();
        }

        // ── Deck validation ──────────────────────────────────────────────

        /// <summary>
        /// Validate that a deck satisfies construction rules before a game starts.
        /// </summary>
        public static RulesResult ValidateDeck(IReadOnlyList<WarlordsCard> deck)
        {
            if (deck.Count != DeckSize)
                return RulesResult.Illegal(
                    $"Deck must contain exactly {DeckSize} cards (has {deck.Count}).");

            int terrainCount    = deck.Count(c => c is TerrainCard);
            int characterCount  = deck.Count(c => c is CharacterCard);

            if (terrainCount < 2)
                return RulesResult.Illegal(
                    $"Deck must contain at least 2 Terrain cards (has {terrainCount}).");

            if (characterCount < 1)
                return RulesResult.Illegal(
                    $"Deck must contain at least 1 Character card (has {characterCount}).");

            return RulesResult.Legal();
        }

        // ── Private helpers ──────────────────────────────────────────────

        /// <summary>Returns true if the move is an advance (toward the enemy).</summary>
        private static bool IsAdvanceMove(CharacterCard character, GameZone from, GameZone to)
        {
            if (from.Owner == PlayerSide.Player)
                return (from.Type == ZoneType.HomeBase     && to.Type == ZoneType.Battlefield)     ||
                       (from.Type == ZoneType.Battlefield   && to.Type == ZoneType.EnemyBattlefield);

            if (from.Owner == PlayerSide.Opponent)
                return (from.Type == ZoneType.EnemyBase    && to.Type == ZoneType.EnemyBattlefield) ||
                       (from.Type == ZoneType.EnemyBattlefield && to.Type == ZoneType.Battlefield);

            return false;
        }

        /// <summary>Returns true if the move is a retreat (toward own Home Base).</summary>
        private static bool IsRetreatMove(CharacterCard character, GameZone from, GameZone to)
        {
            if (from.Owner == PlayerSide.Player)
                return (from.Type == ZoneType.Battlefield      && to.Type == ZoneType.HomeBase)     ||
                       (from.Type == ZoneType.EnemyBattlefield  && to.Type == ZoneType.Battlefield);

            if (from.Owner == PlayerSide.Opponent)
                return (from.Type == ZoneType.EnemyBattlefield && to.Type == ZoneType.EnemyBase)    ||
                       (from.Type == ZoneType.Battlefield       && to.Type == ZoneType.EnemyBattlefield);

            return false;
        }

        /// <summary>
        /// Returns true if two zones are on opposite sides and directly adjacent
        /// (i.e., valid melee attack range).
        /// </summary>
        private static bool AreOpposingAdjacentZones(GameZone a, GameZone b)
        {
            // Player Battlefield ↔ Opponent Battlefield
            return (a.Type == ZoneType.Battlefield      && b.Type == ZoneType.EnemyBattlefield) ||
                   (a.Type == ZoneType.EnemyBattlefield && b.Type == ZoneType.Battlefield);
        }
    }
}
