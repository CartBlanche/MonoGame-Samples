//-----------------------------------------------------------------------------
// CharacterCard.cs
//
// Represents a character card in Warlords
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace WarlordsFramework
{
    /// <summary>
    /// Represents a character card with Soul Essence and combat capabilities
    /// </summary>
    public class CharacterCard : WarlordsCard
    {
        // Core properties
        public int MaxSoulEssence { get; set; }
        public int CurrentSoulEssence { get; set; }
        public int AttackPower { get; set; }
        
        // Classification and affiliations
        public Classification Classification { get; set; }
        public List<string> Affiliations { get; set; }
        
        // Movement/action tracking
        /// <summary>The action taken this turn. None = has not acted yet.</summary>
        public CharacterAction ActionThisTurn { get; set; }

        /// <summary>Computed convenience: true if any action has been taken this turn.</summary>
        public bool HasActedThisTurn => ActionThisTurn != CharacterAction.None;

        public bool CanRetreat { get; set; }

        /// <summary>
        /// Default damage reduction applied when this character is in Defend stance.
        /// Card text may override this per-card by subclassing.
        /// </summary>
        public const float DefendDamageReduction = 0.25f;
        
        // Equipment
        public ItemCard EquippedItem { get; set; }
        
        // Future expansion - abilities (null for minimal prototype)
        public object WishGene { get; set; }
        public object SoulSkill { get; set; }
        public object PatchworkReality { get; set; }
        
        public CharacterCard()
        {
            Affiliations = new List<string>();
            CanRetreat = true;
            ActionThisTurn = CharacterAction.None;
        }
        
        /// <summary>
        /// Perform a simple attack on a target character.
        /// If the target is in Defend stance the damage is reduced by
        /// <see cref="DefendDamageReduction"/> (or the target's override).
        /// </summary>
        public void Attack(CharacterCard target)
        {
            if (HasActedThisTurn) return;

            int damage = AttackPower;
            if (target.ActionThisTurn == CharacterAction.Defend)
                damage = (int)(damage * (1f - DefendDamageReduction));

            target.TakeDamage(damage);
            ActionThisTurn = CharacterAction.Attack;
        }
        
        /// <summary>
        /// Take damage and reduce Soul Essence
        /// </summary>
        public void TakeDamage(int damage)
        {
            CurrentSoulEssence -= damage;
            if (CurrentSoulEssence < 0) CurrentSoulEssence = 0;
        }
        
        /// <summary>
        /// Check if character is defeated
        /// </summary>
        public bool IsDefeated => CurrentSoulEssence <= 0;
        
        /// <summary>
        /// Reset action state at the start of a new turn.
        /// </summary>
        public void ResetTurnState()
        {
            ActionThisTurn = CharacterAction.None;
        }
    }

    public enum Classification
    {
        Eternal,
        Demon,
        Vessel,
        Human,
        Dwarf,
        Ghost,
        Neutral
    }
}
