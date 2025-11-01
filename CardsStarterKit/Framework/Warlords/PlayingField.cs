//-----------------------------------------------------------------------------
// PlayingField.cs
//
// Manages the four-zone playing field
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace WarlordsFramework
{
    /// <summary>
    /// The playing field consisting of four zones
    /// </summary>
    public class PlayingField
    {
        // Player zones (bottom two)
        public GameZone PlayerHomeBase { get; set; }
        public GameZone PlayerBattlefield { get; set; }
        
        // Opponent zones (top two)
        public GameZone OpponentBattlefield { get; set; }
        public GameZone OpponentBase { get; set; }
        
        public PlayingField()
        {
            PlayerHomeBase = new GameZone(ZoneType.HomeBase, PlayerSide.Player);
            PlayerBattlefield = new GameZone(ZoneType.Battlefield, PlayerSide.Player);
            OpponentBattlefield = new GameZone(ZoneType.EnemyBattlefield, PlayerSide.Opponent);
            OpponentBase = new GameZone(ZoneType.EnemyBase, PlayerSide.Opponent);
        }
        
        /// <summary>
        /// Get all zones as a list
        /// </summary>
        public List<GameZone> GetAllZones()
        {
            return new List<GameZone>
            {
                OpponentBase,
                OpponentBattlefield,
                PlayerBattlefield,
                PlayerHomeBase
            };
        }
        
        /// <summary>
        /// Find which zone contains a specific character
        /// </summary>
        public GameZone GetZoneContaining(CharacterCard character)
        {
            return GetAllZones().FirstOrDefault(z => z.Characters.Contains(character));
        }
        
        /// <summary>
        /// Check if a character can advance from one zone to another
        /// </summary>
        public bool CanAdvance(CharacterCard character, GameZone fromZone, GameZone toZone)
        {
            // Can't advance if already acted
            if (character.HasActedThisTurn) return false;
            
            // Can't move to battlefield if no terrain
            if (toZone.Type == ZoneType.Battlefield && toZone.ActiveTerrain == null)
                return false;
                
            // Can't advance to enemy battlefield if enemies present
            if (toZone.Type == ZoneType.EnemyBattlefield && toZone.HasCharacters)
                return false;
                
            return true;
        }
    }
}
