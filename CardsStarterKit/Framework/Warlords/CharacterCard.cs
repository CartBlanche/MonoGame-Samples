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
        public bool HasActedThisTurn { get; set; }
        public bool CanRetreat { get; set; }
        
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
        }
        
        /// <summary>
        /// Perform a simple attack on a target character
        /// </summary>
        public void Attack(CharacterCard target)
        {
            if (HasActedThisTurn) return;
            
            target.TakeDamage(AttackPower);
            HasActedThisTurn = true;
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
        /// Reset action state at end of turn
        /// </summary>
        public void ResetTurnState()
        {
            HasActedThisTurn = false;
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
