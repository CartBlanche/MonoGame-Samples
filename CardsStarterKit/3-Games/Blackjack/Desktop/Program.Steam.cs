using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Blackjack.Networking;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Net.Steam;

namespace Blackjack.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            MultiplayerSessionService.InitializeBackend(MultiplayerBackend.Steam, "Blackjack");
            LeaderboardPlatformStatus.ConfigureFromBackend(MultiplayerSessionService.ActiveBackend);

            var steamInitialized = SteamRuntime.Initialize();
            SteamPlatformBootstrap.Configure("Blackjack");

            bool steamSignedIn = false;
            if (steamInitialized)
            {
                try
                {
                    // Steam sign-in also wires the live leaderboard provider when available.
                    steamSignedIn = Task.Run(() => SteamPlatformBootstrap.TrySignInAndEnableLiveAsync()).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Steam] Auto sign-in failed with exception: {ex}");
                    Console.WriteLine($"[Steam] Auto sign-in failed: {ex.Message}");
                    steamSignedIn = false;
                }
            }
            else
            {
                NetworkServiceProvider.ResetToDefault(); // UDP/SystemLink fallback
                Debug.WriteLine("[Steam] Steam runtime failed to initialize.");
                Console.WriteLine("[Steam] Steam runtime failed to initialize.");
            }

            LeaderboardPlatformStatus.SetAutoSignInResult(steamSignedIn);
            Debug.WriteLine($"[Steam] Signed in to Live: {steamSignedIn}");
            Console.WriteLine($"[Steam] Signed in to Live: {steamSignedIn}");

            try
            {
                using var game = new DesktopSteamGame();
                game.Run();
            }
            finally
            {
                if (steamInitialized)
                {
                    SteamRuntime.Shutdown();
                }
            }
        }
    }
}