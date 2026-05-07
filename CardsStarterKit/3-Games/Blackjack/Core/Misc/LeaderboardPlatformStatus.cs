using Blackjack.Networking;

namespace Blackjack
{
    /// <summary>
    /// Tracks storefront login state for lightweight main menu status text.
    /// </summary>
    public static class LeaderboardPlatformStatus
    {
        private const string DefaultPlatform = "Steam";

        public static string PlatformName { get; private set; } = DefaultPlatform;
        public static bool IsLeaderboardPlatformBuild { get; private set; }
        public static bool AutoSignInAttempted { get; private set; }
        public static bool AutoSignInSucceeded { get; private set; }

        public static void ConfigureLeaderboardPlatformBuild(string platformName)
        {
            PlatformName = string.IsNullOrWhiteSpace(platformName) ? DefaultPlatform : platformName;
            IsLeaderboardPlatformBuild = true;
            AutoSignInAttempted = false;
            AutoSignInSucceeded = false;
        }

        public static void ConfigureBuildWithoutLeaderboards()
        {
            PlatformName = DefaultPlatform;
            IsLeaderboardPlatformBuild = false;
            AutoSignInAttempted = false;
            AutoSignInSucceeded = false;
        }

        public static void ConfigureFromBackend(MultiplayerBackend backend)
        {
            switch (backend)
            {
                case MultiplayerBackend.Steam:
                    ConfigureLeaderboardPlatformBuild("Steam");
                    break;
                case MultiplayerBackend.Epic:
                    ConfigureLeaderboardPlatformBuild("Epic");
                    break;
                case MultiplayerBackend.GooglePlay:
                    ConfigureLeaderboardPlatformBuild("Google Play");
                    break;
                case MultiplayerBackend.IOS:
                    ConfigureLeaderboardPlatformBuild("Game Center");
                    break;
                case MultiplayerBackend.UdpSystemLink:
                default:
                    ConfigureBuildWithoutLeaderboards();
                    break;
            }
        }

        public static void SetAutoSignInResult(bool success)
        {
            AutoSignInAttempted = true;
            AutoSignInSucceeded = success;
        }

        public static string GetLeaderboardStatusMessage()
        {
            if (!IsLeaderboardPlatformBuild)
            {
                return string.Empty;
            }

            if (AutoSignInAttempted && AutoSignInSucceeded)
            {
                return string.Format(GetString("LoggedIntoPlatform", "Logged into {0}"), PlatformName);
            }

            if (AutoSignInAttempted && !AutoSignInSucceeded)
            {
                return string.Format(GetString("UnableToLogIntoPlatform", "Unable to log into {0}"), PlatformName);
            }

            return string.Empty;
        }

        private static string GetString(string key, string fallback)
        {
            var value = Resources.ResourceManager.GetString(key, Resources.Culture);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}