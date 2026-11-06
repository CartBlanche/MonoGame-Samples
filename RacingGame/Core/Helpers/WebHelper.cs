using System;
using System.Diagnostics;

namespace RacingGame.Helpers
{
    /// <summary>
    /// Opening web pages from the game (the editor page now, a credits screen later).
    /// </summary>
    public static class WebHelper
    {
        /// <summary>
        /// Launch the default browser at the given URL. Never throws: a problem is logged.
        /// </summary>
        public static void OpenUrl(string url)
        {
            try
            {
                // UseShellExecute makes the OS open the URL with the default browser
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Write("Could not open " + url + ": " + ex.Message);
            }
        }
    }
}
