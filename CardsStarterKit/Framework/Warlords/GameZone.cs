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
        
        public GameZone(ZoneType type, PlayerSide owner)
        {
            Type = type;
            Owner = owner;
            Characters = new List<CharacterCard>();
        }
        
        /// <summary>
        /// Add a character to this zone
        /// </summary>
        public void AddCharacter(CharacterCard card)
        {
            Characters.Add(card);
        }
        
        /// <summary>
        /// Remove a character from this zone
        /// </summary>
        public void RemoveCharacter(CharacterCard card)
        {
            Characters.Remove(card);
        }
        
        /// <summary>
        /// Get total power of all characters in zone
        /// </summary>
        public int GetTotalPower()
        {
            return Characters.Sum(c => c.CurrentSoulEssence);
        }
        
        /// <summary>
        /// Check if zone has any characters
        /// </summary>
        public bool HasCharacters => Characters.Count > 0;
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
