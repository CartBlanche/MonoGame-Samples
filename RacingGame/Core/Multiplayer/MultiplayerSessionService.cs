using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Net;

namespace RacingGame.Multiplayer
{
    /// <summary>
    /// Creates, finds and joins LAN (UDP / SystemLink) race sessions.
    /// </summary>
    public static class MultiplayerSessionService
    {
        // Bump when packet formats change so mismatched builds do not join each other.
        public const int ProtocolVersion = 2;

        public const int MaxPlayers = 4;

        const string GameName = "RacingGame";
        const string GamePropertyKey = "Game";
        const string ProtocolPropertyKey = "Protocol";
        const string InProgressPropertyKey = "InProgress";

        static bool initialized;

        /// <summary>
        /// The name this player is shown with in a race: the Name setting, or the gamertag without
        /// its "_id" suffix when no name has been set. (The unique gamertag is still what the
        /// network uses to tell players apart.)
        /// </summary>
        public static string LocalName(string gamertag)
        {
            string name = RacingGame.Properties.GameSettings.Default.PlayerName;
            if (string.IsNullOrWhiteSpace(name))
                return DisplayName(gamertag);
            name = name.Trim();
            return name.Length > 20 ? name.Substring(0, 20) : name;
        }

        /// <summary>
        /// A gamertag without the "_id" suffix the network library adds to keep names unique.
        /// </summary>
        public static string DisplayName(string gamertag)
        {
            if (string.IsNullOrEmpty(gamertag))
                return gamertag;
            // only a trailing "_" + 8 hex digits is removed, so names with their own underscores survive
            int i = gamertag.Length - 9;
            if (i <= 0 || gamertag[i] != '_')
                return gamertag;
            for (int k = i + 1; k < gamertag.Length; k++)
                if (!Uri.IsHexDigit(gamertag[k]))
                    return gamertag;
            return gamertag.Substring(0, i);
        }


        /// <summary>
        /// Select the default UDP / SystemLink transport and keep this game's sessions apart
        /// from other games' that use the library. Safe to call more than once.
        /// </summary>
        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            NetworkServiceProvider.ResetToDefault();
            NetworkSession.GameId = GameName;
        }

        public static async Task<NetworkSession> CreateHostSessionAsync()
        {
            Initialize();
            Debug.WriteLine("[NET] Hosting a race for up to " + MaxPlayers + " players");

            // one local gamer per machine
            NetworkSession session = await NetworkSession.CreateAsync(
                NetworkSessionType.SystemLink, 1, MaxPlayers, 0, null);

            // set after creation (CreateAsync does not copy the properties it is given), and all
            // keys added now: the advertisement reads them on another thread
            session.SessionProperties[GamePropertyKey] = GameName;
            session.SessionProperties[ProtocolPropertyKey] = ProtocolVersion;
            session.SessionProperties[InProgressPropertyKey] = false;
            return session;
        }

        /// <summary>
        /// Host: stop advertising the session as joinable once the race has started
        /// </summary>
        public static void MarkInProgress(NetworkSession session)
        {
            session.SessionProperties[InProgressPropertyKey] = true;
        }

        public static Task<AvailableNetworkSessionCollection> FindSessionsAsync()
        {
            Initialize();
            return NetworkSession.FindAsync(NetworkSessionType.SystemLink, 1, null);
        }

        public static Task<NetworkSession> JoinSessionAsync(AvailableNetworkSession session)
        {
            return NetworkSession.JoinAsync(session);
        }

        /// <summary>
        /// True if an advertised session is a race we can join
        /// </summary>
        public static bool IsCompatible(AvailableNetworkSession session)
        {
            object game, protocol;
            if (session.SessionProperties == null ||
                !session.SessionProperties.TryGetValue(GamePropertyKey, out game) ||
                !session.SessionProperties.TryGetValue(ProtocolPropertyKey, out protocol))
                return false;

            if (!GameName.Equals(game) || !(protocol is int) || (int)protocol != ProtocolVersion)
                return false;

            // a race that has started cannot be joined
            object inProgress;
            return !(session.SessionProperties.TryGetValue(InProgressPropertyKey, out inProgress) &&
                     inProgress is bool && (bool)inProgress);
        }
    }
}
