#if __ANDROID__
using Android.OS;
#endif
#if __IOS__
using UIKit;
#endif

namespace Microsoft.Xna.Framework.GamerServices
{
	/// <summary>
	/// Represents a signed-in gamer.
	/// </summary>
	public class SignedInGamer : Gamer
    {
        private static SignedInGamer current;
        private bool isSignedInToLive;

        /// <summary>
        /// Gets the current signed-in gamer.
        /// </summary>
        public static SignedInGamer Current
        {
            get
            {
                if (current == null)
                {
                    current = new SignedInGamer();
                    current.SetGamertag(GetPlatformUsername());
                }
                return current;
            }
            internal set => current = value;
        }

        /// <summary>
        /// Gets a platform-appropriate username for the device.
        /// Attempts to retrieve the actual user identity on each platform with appropriate fallbacks.
        /// </summary>
        private static string GetPlatformUsername()
        {
            // Desktop platforms: Use OS username (most reliable)
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                return GetDesktopUsername();
            }

            // Mobile platforms: Use device-specific methods
            if (OperatingSystem.IsAndroid())
            {
                return GetAndroidUsername();
            }

            if (OperatingSystem.IsIOS())
            {
                return GetIOSUsername();
            }

            // Ultimate fallback
            return "Player";
        }

        /// <summary>
        /// Gets username on desktop platforms (Windows, macOS, Linux).
        /// </summary>
        private static string GetDesktopUsername()
        {
            try
            {
                // Try Environment.UserName first (most reliable)
                string userName = Environment.UserName;
                if (!string.IsNullOrWhiteSpace(userName) && !IsSystemUID(userName))
                {
                    return userName;
                }

                // Fallback to USER environment variable (macOS/Linux)
                string envUser = Environment.GetEnvironmentVariable("USER");
                if (!string.IsNullOrWhiteSpace(envUser) && !IsSystemUID(envUser))
                {
                    return envUser;
                }

                // Windows alternative: USERNAME environment variable
                string envUsername = Environment.GetEnvironmentVariable("USERNAME");
                if (!string.IsNullOrWhiteSpace(envUsername))
                {
                    return envUsername;
                }
            }
            catch { }

            return "Player";
        }

        /// <summary>
        /// Gets username on Android.
        /// Tries device name (user-configurable), then brand+model, then Android ID hash.
        /// </summary>
        private static string GetAndroidUsername()
        {
#if __ANDROID__
            try
            {
                // Try 1: Device name from Settings (user-configurable, most meaningful)
                try
                {
                    string deviceName = Android.Provider.Settings.Secure.GetString(
                        Android.App.Application.Context.ContentResolver,
                        Android.Provider.Settings.Secure.AndroidDeviceName
                    );
                    if (!string.IsNullOrWhiteSpace(deviceName))
                    {
                        return deviceName;
                    }
                }
                catch { }

                // Try 2: Brand + Model (e.g., "Samsung Galaxy S21")
                try
                {
                    string brand = Android.OS.Build.Brand ?? "";
                    string model = Android.OS.Build.Model ?? "";

                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        if (!string.IsNullOrWhiteSpace(brand) && !model.StartsWith(brand, StringComparison.OrdinalIgnoreCase))
                        {
                            return $"{brand} {model}".Trim();
                        }
                        return model;
                    }
                }
                catch { }

                // Try 3: Android ID (unique but less user-friendly)
                try
                {
                    string androidId = Android.Provider.Settings.Secure.GetString(
                        Android.App.Application.Context.ContentResolver,
                        Android.Provider.Settings.Secure.AndroidId
                    );
                    if (!string.IsNullOrWhiteSpace(androidId) && androidId.Length >= 8)
                    {
                        return $"Device_{androidId.Substring(0, 8).ToUpperInvariant()}";
                    }
                }
                catch { }
            }
            catch { }
#endif

            return "AndroidPlayer";
        }

        /// <summary>
        /// Gets username on iOS.
        /// Tries device name (user-configurable), then creates/retrieves persistent UUID.
        /// </summary>
        private static string GetIOSUsername()
        {
#if __IOS__
            try
            {
                // Try 1: Device name (user-configured in Settings > General > About > Name)
                try
                {
                    string deviceName = UIKit.UIDevice.CurrentDevice.Name;
                    if (!string.IsNullOrWhiteSpace(deviceName) && deviceName != "iPhone" && deviceName != "iPad")
                    {
                        return deviceName;
                    }
                }
                catch { }

                // Try 2: Persistent UUID stored in UserDefaults (survives reinstalls if iCloud synced)
                try
                {
                    var defaults = Foundation.NSUserDefaults.StandardUserDefaults;
                    string uuid = defaults.StringForKey("game_player_uuid");

                    if (string.IsNullOrEmpty(uuid))
                    {
                        // Generate new UUID if not present
                        uuid = Foundation.NSProcessInfo.ProcessInfo.GloballyUniqueString;
                        defaults.SetString(uuid, "game_player_uuid");
                        defaults.Synchronize();
                    }

                    if (!string.IsNullOrWhiteSpace(uuid) && uuid.Length >= 8)
                    {
                        return $"Player_{uuid.Substring(0, 8).ToUpperInvariant()}";
                    }
                }
                catch { }

                // Try 3: UIDevice model as fallback
                try
                {
                    string model = UIKit.UIDevice.CurrentDevice.Model;
                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        return model;
                    }
                }
                catch { }
            }
            catch { }
#endif

            return "iOSPlayer";
        }

        /// <summary>
        /// Checks if a username represents a system user rather than a real person.
        /// </summary>
        private static bool IsSystemUID(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            // Android app UID format (e.g., "U0_A123" or "u0_a123")
            if (name.StartsWith("U0_A", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("u0_a", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Linux system UIDs (typically < 1000)
            if (int.TryParse(name, out int uid) && uid < 1000)
            {
                return true;
            }

            // Common system users across Unix-like systems
            string[] systemUsers = new[]
            {
                "root", "bin", "sys", "daemon", "adm", "lp", "sync", "shutdown",
                "halt", "mail", "news", "uucp", "operator", "nobody", "games",
                "postgres", "mysql", "www-data", "apache", "nginx"
            };

            return Array.Exists(systemUsers, user => user.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private string gamertag;

        /// <summary>
        /// Gets or sets the gamertag for this gamer.
        /// </summary>
        public override string Gamertag 
        { 
            get => gamertag;
        }

        /// <summary>
        /// Sets the gamertag for this gamer.
        /// </summary>
        internal void SetGamertag(string value)
        {
            gamertag = value;
        }

        /// <summary>
        /// Gets whether this gamer is signed in to a live service.
        /// </summary>
        public bool IsSignedInToLive => isSignedInToLive;

        internal void SetSignedInToLive(bool value)
        {
            isSignedInToLive = value;
        }

        /// <summary>
        /// Gets whether this gamer is a guest.
        /// </summary>
        public bool IsGuest => false;

        /// <summary>
        /// Gets the display name for this gamer.
        /// </summary>
        public new string DisplayName => Gamertag;

        /// <summary>
        /// Gets the presence information for this gamer.
        /// </summary>
        public GamerPresence Presence { get; } = new GamerPresence();

        /// <summary>
        /// Gets the player index for this gamer.
        /// </summary>
        public PlayerIndex PlayerIndex { get; internal set; } = PlayerIndex.One;

        internal SignedInGamer() { }

		public GamerPrivileges Privileges
		{
			get;
			private set;
		}

        /// <summary>
        /// Asynchronously writes a score to a leaderboard.
        /// </summary>
        public async Task WriteLeaderboardAsync(LeaderboardWriter writer, CancellationToken cancellationToken = default)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (!ReferenceEquals(writer.Gamer, this))
                throw new InvalidOperationException("LeaderboardWriter.Gamer must match this SignedInGamer.");

            cancellationToken.ThrowIfCancellationRequested();
            var provider = LeaderboardService.ResolveProvider(this);
            await provider.SubmitAsync(writer, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronous wrapper for WriteLeaderboardAsync.
        /// </summary>
        public void WriteLeaderboard(LeaderboardWriter writer)
        {
            WriteLeaderboardAsync(writer).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously reads leaderboard rows from a page range.
        /// </summary>
        public Task<LeaderboardReader> GetLeaderboardAsync(
            LeaderboardIdentity leaderboard,
            int pageStart,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var provider = LeaderboardService.ResolveProvider(this);
            return provider.ReadAsync(leaderboard, pageStart, pageSize, null, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads leaderboard rows centered around this gamer.
        /// </summary>
        public Task<LeaderboardReader> GetLeaderboardAsync(
            LeaderboardIdentity leaderboard,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var provider = LeaderboardService.ResolveProvider(this);
            return provider.ReadAsync(leaderboard, 0, pageSize, this, cancellationToken);
        }

        /// <summary>
        /// Synchronous wrapper for GetLeaderboardAsync.
        /// </summary>
        public LeaderboardReader GetLeaderboard(LeaderboardIdentity leaderboard, int pageStart, int pageSize)
        {
            return GetLeaderboardAsync(leaderboard, pageStart, pageSize).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronous wrapper for gamer-centered GetLeaderboardAsync.
        /// </summary>
        public LeaderboardReader GetLeaderboard(LeaderboardIdentity leaderboard, int pageSize)
        {
            return GetLeaderboardAsync(leaderboard, pageSize).GetAwaiter().GetResult();
        }
	}
}
