//-----------------------------------------------------------------------------
// TurnPhase.cs
//
// Turn phase enum and per-turn action tracker for Warlords rules engine.
//-----------------------------------------------------------------------------

namespace WarlordsFramework
{
    /// <summary>
    /// The four phases of a Warlords turn, in order.
    /// </summary>
    public enum TurnPhase
    {
        /// <summary>Optional draw of 1 card.</summary>
        Draw,

        /// <summary>
        /// Play cards (1 Character, 1 Item, 1 Event, 1 Terrain per turn).
        /// Perform character actions (advance/retreat/attack/defend).
        /// Sacrifice cards.
        /// </summary>
        Main,

        /// <summary>
        /// Apply passive regen, terrain regen bonuses, degen above 20,000 SE,
        /// and Overburden tier-1 degen.
        /// </summary>
        RegenDegen,

        /// <summary>
        /// Overburden tier-2/3 penalties, end-of-turn card effects, win check.
        /// </summary>
        End
    }

    /// <summary>
    /// Tracks which card types have already been played, and which phase
    /// the current turn is in. Instantiated fresh at the start of each turn.
    /// </summary>
    public class TurnTracker
    {
        // ── Card play flags ───────────────────────────────────────────────
        public bool HasPlayedCharacter { get; set; }
        public bool HasPlayedItem      { get; set; }
        public bool HasPlayedEvent     { get; set; }
        public bool HasPlayedTerrain   { get; set; }

        // ── Draw flag ────────────────────────────────────────────────────
        public bool HasDrawnThisTurn   { get; set; }

        // ── Overburden usage counters ────────────────────────────────────
        public int CardsPlayedThisTurn      { get; set; }
        public int CharacterActionsThisTurn { get; set; }

        // ── Active phase ─────────────────────────────────────────────────
        public TurnPhase CurrentPhase  { get; set; }

        public TurnTracker()
        {
            Reset();
        }

        /// <summary>
        /// Reset all flags and return to Draw phase, ready for a new turn.
        /// </summary>
        public void Reset()
        {
            HasPlayedCharacter      = false;
            HasPlayedItem           = false;
            HasPlayedEvent          = false;
            HasPlayedTerrain        = false;
            HasDrawnThisTurn        = false;
            CardsPlayedThisTurn     = 0;
            CharacterActionsThisTurn = 0;
            CurrentPhase            = TurnPhase.Draw;
        }

        /// <summary>
        /// Advance to the next phase. Returns the new phase.
        /// </summary>
        public TurnPhase AdvancePhase()
        {
            CurrentPhase = CurrentPhase switch
            {
                TurnPhase.Draw      => TurnPhase.Main,
                TurnPhase.Main      => TurnPhase.RegenDegen,
                TurnPhase.RegenDegen => TurnPhase.End,
                _                   => TurnPhase.Draw   // wraps back (new turn)
            };
            return CurrentPhase;
        }
    }
}
