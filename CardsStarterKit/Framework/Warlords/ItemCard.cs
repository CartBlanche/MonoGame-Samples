//-----------------------------------------------------------------------------
// ItemCard.cs
//
// Represents an item/artifact card
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace WarlordsFramework
{
    /// <summary>
    /// Items can be equipped to characters or deployed as zone obstacles.
    /// </summary>
    public class ItemCard : WarlordsCard
    {
        public bool RequiresCharacter { get; set; }
        public List<string> EquipRestrictions { get; set; }

        // ── Zone obstacle support ─────────────────────────────────────────
        /// <summary>
        /// True if this item can be deployed into a zone as an obstacle
        /// (e.g. a turret). Zone obstacles block enemy advancement and must
        /// be destroyed before the zone can be entered.
        /// </summary>
        public bool IsZoneObstacle { get; set; }

        /// <summary>
        /// Current Soul Essence (hit points) of this item while deployed as
        /// a zone obstacle. Starts equal to <see cref="MaxDeployedSE"/>.
        /// </summary>
        public int DeployedSE { get; set; }

        /// <summary>
        /// Maximum Soul Essence this item has when deployed as a zone obstacle.
        /// </summary>
        public int MaxDeployedSE { get; set; }

        /// <summary>
        /// True when this obstacle has been defeated (DeployedSE reduced to 0 or below).
        /// Only meaningful when <see cref="IsZoneObstacle"/> is true.
        /// </summary>
        public bool IsObstacleDefeated => IsZoneObstacle && DeployedSE <= 0;

        // Future expansion - actual effects (null for minimal prototype)
        public object Effect { get; set; }

        public ItemCard()
        {
            EquipRestrictions = new List<string>();
        }

        /// <summary>
        /// Apply damage to this zone obstacle.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (!IsZoneObstacle) return;
            DeployedSE -= amount;
        }
    }
}
