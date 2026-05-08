using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Net.Steam;

namespace NetworkStateManagement.DesktopGL
{
    // Steam entry point with runtime initialization and sign-in bootstrap.
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // SteamAPI looks for steam_appid.txt from the current working directory.
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var steamInitialized = SteamRuntime.Initialize();
            SteamPlatformBootstrap.Configure("NetworkStateManagement");

            if (steamInitialized)
            {
                try
                {
                    Task.Run(() => SteamPlatformBootstrap.TrySignInAndEnableLiveAsync()).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Steam] Auto sign-in failed: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[Steam] Steam runtime failed to initialize.");
            }

            try
            {
                using (var game = new DesktopSteamGame())
                    game.Run();
            }
            finally
            {
                if (steamInitialized)
                    SteamRuntime.Shutdown();
            }
        }
    }
}
