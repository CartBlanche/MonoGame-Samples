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
            
            // Can't move if character not in fromZone
            if (!fromZone.Characters.Contains(character)) return false;
            
            // Player movement rules
            if (fromZone.Owner == PlayerSide.Player)
            {
                // Player can advance from Home Base to Battlefield
                if (fromZone.Type == ZoneType.HomeBase && toZone.Type == ZoneType.Battlefield)
                    return true;
                    
                // Player can retreat from Battlefield to Home Base
                if (fromZone.Type == ZoneType.Battlefield && toZone.Type == ZoneType.HomeBase)
                    return true;
                    
                // Player can advance from Battlefield to Enemy Battlefield (attack)
                if (fromZone.Type == ZoneType.Battlefield && toZone.Type == ZoneType.EnemyBattlefield)
                    return true;
            }
            
            // Opponent movement rules
            if (fromZone.Owner == PlayerSide.Opponent)
            {
                // Opponent can advance from Base to Battlefield
                if (fromZone.Type == ZoneType.EnemyBase && toZone.Type == ZoneType.EnemyBattlefield)
                    return true;
                    
                // Opponent can retreat from Battlefield to Base
                if (fromZone.Type == ZoneType.EnemyBattlefield && toZone.Type == ZoneType.EnemyBase)
                    return true;
                    
                // Opponent can advance from Battlefield to Player Battlefield (attack)
                if (fromZone.Type == ZoneType.EnemyBattlefield && toZone.Type == ZoneType.Battlefield)
                    return true;
            }
                
            return false;
        }
    }
}
