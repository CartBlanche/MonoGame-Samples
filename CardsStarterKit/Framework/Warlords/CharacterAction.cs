//-----------------------------------------------------------------------------
// CharacterAction.cs
//
// The mutually-exclusive per-turn actions a Character card may perform.
//-----------------------------------------------------------------------------

namespace WarlordsFramework
{
    /// <summary>
    /// Each Character may take exactly one of these actions per turn unless
    /// a card skill explicitly grants additional actions.
    /// </summary>
    public enum CharacterAction
    {
        /// <summary>No action has been taken yet this turn.</summary>
        None,

        /// <summary>Move one zone toward the enemy (Home Base → Battlefield → Enemy Battlefield).</summary>
        Advance,

        /// <summary>Move one zone toward own Home Base (Enemy Battlefield → Battlefield → Home Base).</summary>
        Retreat,

        /// <summary>Deal damage to a valid target character or Warlord.</summary>
        Attack,

        /// <summary>
        /// Enter a defensive stance granting flat 25% damage reduction
        /// (or as specified by card text) until start of next turn.
        /// </summary>
        Defend
    }
}
