//-----------------------------------------------------------------------------
// WarlordsCard.cs
//
// Base class for all Warlords cards
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace WarlordsFramework
{
    /// <summary>
    /// Base class for all Warlords card types
    /// </summary>
    public abstract class WarlordsCard
    {
        public string Name { get; set; }
        public string LoreDescription { get; set; }
        public CardRarity Rarity { get; set; }
        public int SoulEssenceCost { get; set; }
        public List<string> Tags { get; set; }

        protected WarlordsCard()
        {
            Tags = new List<string>();
            SoulEssenceCost = 0; // Default cost
        }
    }

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
        Mythic
    }
}
