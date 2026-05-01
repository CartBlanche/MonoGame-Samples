using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.GamerServices;
using Steamworks;

namespace Microsoft.Xna.Framework.Net.Steam
{
    /// <summary>
    /// Steam implementation of IGuideSignInProvider.
    ///
    /// Prerequisites (composition root responsibility):
    ///   1. Place a valid steam_appid.txt beside the executable (or call SteamAPI_RestartAppIfNecessary).
    ///   2. Call SteamAPI.Init() once at application startup before the game loop begins.
    ///   3. Call SteamAPI.RunCallbacks() each frame (typically in Game.Update).
    ///   4. Call SteamAPI.Shutdown() on application exit.
    ///
    /// Usage:
    ///   Guide.SignInProvider = new SteamSignInProvider();
    ///   // later, when triggering sign-in:
    ///   if (await Guide.ShowSignInAsync(1, onlineOnly: false))
    ///       LeaderboardService.LiveProvider = new SteamLeaderboardProvider();
    /// </summary>
    public sealed class SteamSignInProvider : IGuideSignInProvider
    {
        /// <summary>
        /// Checks whether the Steam user is currently logged on and, if so, updates
        /// SignedInGamer.Current with the Steam persona name and returns true.
        ///
        /// Returns false if Steam is not running, not initialized, or the user is not logged in.
        /// This method does NOT call SteamAPI.Init() — that is the composition root's responsibility.
        /// </summary>
        public Task<bool> ShowSignInAsync(
            int paneCount,
            bool onlineOnly,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(SteamRuntime.RefreshSignedInGamerIdentity());
        }
    }
}
