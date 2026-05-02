//-----------------------------------------------------------------------------
// GameZone.cs
//
// Represents one of the four playing zones
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace WarlordsFramework
{
    /// <summary>
    /// Represents a zone on the playing field
    /// </summary>
    public class GameZone
    {
        public ZoneType Type { get; set; }
        public PlayerSide Owner { get; set; }
        public List<CharacterCard> Characters { get; private set; }
        public TerrainCard ActiveTerrain { get; set; }

        // ── Zone items (obstacle turrets, etc.) ──────────────────────────
        public List<ItemCard> ZoneItems { get; private set; }

        /// <summary>
        /// True once terrain has been formally placed on this zone via the
        /// terrain-contest mechanic. Characters may not advance from their
        /// Home Base into this zone until this flag is set.
        /// </summary>
        public bool HasTerrainBeenSet { get; set; }

        public GameZone(ZoneType type, PlayerSide owner)
        {
            Type = type;
            Owner = owner;
            Characters = new List<CharacterCard>();
            ZoneItems = new List<ItemCard>();
        }

        /// <summary>Add a character to this zone.</summary>
        public void AddCharacter(CharacterCard card)
        {
            Characters.Add(card);
        }

        /// <summary>Remove a character from this zone.</summary>
        public void RemoveCharacter(CharacterCard card)
        {
            Characters.Remove(card);
        }

        /// <summary>Deploy an item as a zone obstacle.</summary>
        public void AddZoneItem(ItemCard item)
        {
            ZoneItems.Add(item);
        }

        /// <summary>Remove a defeated or removed zone-obstacle item.</summary>
        public void RemoveZoneItem(ItemCard item)
        {
            ZoneItems.Remove(item);
        }

        /// <summary>Get total power of all characters in zone.</summary>
        public int GetTotalPower()
        {
            return Characters.Sum(c => c.CurrentSoulEssence);
        }

        /// <summary>True if any characters are present.</summary>
        public bool HasCharacters => Characters.Count > 0;

        /// <summary>
        /// True if there is at least one undestroyed zone-obstacle item
        /// (DeployedSE > 0) in this zone.
        /// </summary>
        public bool HasObstacles => ZoneItems.Any(i => i.IsZoneObstacle && i.DeployedSE > 0);

        /// <summary>
        /// True when the zone has no characters AND no active obstacle items —
        /// i.e. an enemy character may advance into this zone.
        /// </summary>
        public bool IsClearForAdvance => !HasCharacters && !HasObstacles;
    }

    public enum ZoneType
    {
        HomeBase,
        Battlefield,
        EnemyBattlefield,
        EnemyBase
    }

    public enum PlayerSide
    {
        Player,
        Opponent
    }
}
