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
        /// Check if a character can move from one zone to an adjacent zone.
        /// This is the legacy execution-layer helper. All new callers should
        /// use <see cref="RulesEngine.CanMoveCharacter"/> for full validation.
        /// </summary>
        public bool CanAdvance(CharacterCard character, GameZone fromZone, GameZone toZone)
        {
            var tracker = new TurnTracker { CurrentPhase = TurnPhase.Main };
            var result = RulesEngine.CanMoveCharacter(character, fromZone, toZone, tracker);
            return result.IsLegal;
        }
    }
}
