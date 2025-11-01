//-----------------------------------------------------------------------------
// SoulEssenceManager.cs
//
// Manages Soul Essence for a player
//-----------------------------------------------------------------------------

namespace WarlordsFramework
{
    /// <summary>
    /// Manages Soul Essence (SE) for a player including regen/degen
    /// </summary>
    public class SoulEssenceManager
    {
        public int CurrentSE { get; private set; }
        public int PassiveRegen { get; set; }
        public int MaxSEBeforeDegen { get; set; }
        
        public SoulEssenceManager(int startingSE = 10000)
        {
            CurrentSE = startingSE;
            PassiveRegen = 1500;
            MaxSEBeforeDegen = 20000;
        }
        
        /// <summary>
        /// Apply passive regeneration and degen if over cap
        /// </summary>
        public void ApplyRegen()
        {
            CurrentSE += PassiveRegen;
            
            // Apply 20% degen on surplus if over cap
            if (CurrentSE > MaxSEBeforeDegen)
            {
                int surplus = CurrentSE - MaxSEBeforeDegen;
                int degen = (int)(surplus * 0.20f);
                CurrentSE -= degen;
            }
        }
        
        /// <summary>
        /// Spend Soul Essence
        /// </summary>
        public bool SpendSE(int amount)
        {
            if (CurrentSE >= amount)
            {
                CurrentSE -= amount;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Gain Soul Essence
        /// </summary>
        public void GainSE(int amount)
        {
            CurrentSE += amount;
        }
        
        /// <summary>
        /// Take damage
        /// </summary>
        public void TakeDamage(int amount)
        {
            CurrentSE -= amount;
        }
        
        /// <summary>
        /// Check if player is defeated
        /// </summary>
        public bool IsDefeated => CurrentSE <= 0;
    }
}
