using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net.Steam
{
    /// <summary>
    /// Composition-root helper for wiring the Steam backend into GamerServices + Net.
    /// </summary>
    public static class SteamPlatformBootstrap
    {
        private static readonly object Gate = new();
        private static Func<ILeaderboardProvider> liveProviderFactory = () => new SteamLeaderboardProvider();

        /// <summary>
        /// Configures Steam-backed services for networking and guide sign-in.
        /// Call once during app startup.
        /// </summary>
        public static void Configure(
            string gameName,
            IGuideSignInProvider signInProvider = null,
            INetworkSessionFactory sessionFactory = null,
            Func<ILeaderboardProvider> liveProviderFactoryOverride = null)
        {
            if (!string.IsNullOrWhiteSpace(gameName))
            {
                LeaderboardService.UsePersistentLocalStorage(gameName.Trim());
            }

            Guide.SignInProvider = signInProvider ?? new SteamSignInProvider();
            NetworkServiceProvider.SetSessionFactory(sessionFactory ?? new SteamNetworkSessionFactory());

            lock (Gate)
            {
                liveProviderFactory = liveProviderFactoryOverride ?? (() => new SteamLeaderboardProvider());
            }
        }

        /// <summary>
        /// Runs Guide sign-in and, on success, enables the Steam live leaderboard provider.
        /// </summary>
        public static async Task<bool> TrySignInAndEnableLiveAsync(
            int paneCount = 1,
            bool onlineOnly = false,
            CancellationToken cancellationToken = default)
        {
            await Guide.ShowSignInAsync(paneCount, onlineOnly, cancellationToken).ConfigureAwait(false);

            if (!SignedInGamer.Current.IsSignedInToLive)
            {
                LeaderboardService.LiveProvider = null;
                return false;
            }

            Func<ILeaderboardProvider> providerFactory;
            lock (Gate)
            {
                providerFactory = liveProviderFactory;
            }

            LeaderboardService.LiveProvider = providerFactory();
            return true;
        }
    }
}
