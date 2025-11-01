//-----------------------------------------------------------------------------
// TerrainCard.cs
//
// Represents a terrain card with dual Home Base and Battlefield effects
//-----------------------------------------------------------------------------

namespace WarlordsFramework
{
    /// <summary>
    /// Terrain cards have different effects when played as Home Base vs Battlefield
    /// </summary>
    public class TerrainCard : WarlordsCard
    {
        public string HomeBaseEffectDescription { get; set; }
        public string BattlefieldEffectDescription { get; set; }
        
        // Numeric bonuses
        public int SEBonus { get; set; }
        public int AttackBonus { get; set; }
        public int RegenBonus { get; set; }
        
        // Future expansion - actual effect objects (null for minimal prototype)
        public object HomeBaseEffect { get; set; }
        public object BattlefieldEffect { get; set; }
        
        public TerrainCard()
        {
        }
    }
}
