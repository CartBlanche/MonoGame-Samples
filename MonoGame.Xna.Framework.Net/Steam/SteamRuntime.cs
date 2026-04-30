using Steamworks;

namespace Microsoft.Xna.Framework.Net.Steam
{
    /// <summary>
    /// Steamworks runtime wrapper so app projects can initialize and pump Steam
    /// without directly depending on Steamworks APIs.
    /// </summary>
    public static class SteamRuntime
    {
        private static bool isInitialized;

        public static bool IsInitialized => isInitialized;

        public static bool Initialize()
        {
            if (isInitialized)
            {
                return true;
            }

            isInitialized = SteamAPI.Init();
            return isInitialized;
        }

        public static void RunCallbacks()
        {
            if (!isInitialized)
            {
                return;
            }

            SteamAPI.RunCallbacks();
        }

        public static void Shutdown()
        {
            if (!isInitialized)
            {
                return;
            }

            SteamAPI.Shutdown();
            isInitialized = false;
        }
    }
}
