//-----------------------------------------------------------------------------
// ItemCard.cs
//
// Represents an item/artifact card
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace WarlordsFramework
{
    /// <summary>
    /// Items can be equipped to characters or placed on terrain
    /// </summary>
    public class ItemCard : WarlordsCard
    {
        public bool RequiresCharacter { get; set; }
        public List<string> EquipRestrictions { get; set; }
        
        // Future expansion - actual effects (null for minimal prototype)
        public object Effect { get; set; }
        
        public ItemCard()
        {
            EquipRestrictions = new List<string>();
        }
    }
}
