using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.GamerServices;

namespace CardsFramework.Core
{
    /// <summary>
    /// Builds a canonical ordered achievement list by combining catalog metadata
    /// with the active provider's player state when available.
    /// </summary>
    public static class AchievementRowsProjector
    {
        public static IReadOnlyList<Achievement> BuildRows(SignedInGamer gamer)
        {
            if (gamer == null)
                throw new ArgumentNullException(nameof(gamer));

            AchievementCollection achievements = null;
            try
            {
                achievements = AchievementService.Provider.GetAchievementsAsync(gamer).GetAwaiter().GetResult();
            }
            catch
            {
                achievements = null;
            }

            var rows = new List<Achievement>();
            foreach (var definition in AchievementCatalog.GetAll())
            {
                Achievement row = achievements?[definition.Key] ?? new Achievement(
                    definition.Key,
                    definition.DisplayName,
                    definition.Description,
                    definition.HowToEarn,
                    definition.GamerScore,
                    0f,
                    false,
                    null,
                    definition.IsHidden,
                    definition.IconKey,
                    definition.IconUri);

                rows.Add(row);
            }

            return rows;
        }
    }
}