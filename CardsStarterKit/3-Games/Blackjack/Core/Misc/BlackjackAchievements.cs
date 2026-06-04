using System;

namespace Blackjack
{
    /// <summary>
    /// Placeholder achievement bridge for Blackjack.
    /// Gameplay code can call RaiseUnlocked when achievements are introduced.
    /// </summary>
    internal static class BlackjackAchievements
    {
        internal static event Action<string, string> AchievementUnlocked;

        internal static void RegisterCatalogDefinitions()
        {
            // Placeholder for future catalog registrations.
        }

        internal static void RaiseUnlocked(string achievementKey, string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return;

            AchievementUnlocked?.Invoke(achievementKey ?? string.Empty, displayName.Trim());
        }
    }
}