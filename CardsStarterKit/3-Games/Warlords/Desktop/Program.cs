//-----------------------------------------------------------------------------
// Program.cs
//
// Entry point for Warlords DesktopGL platform
//-----------------------------------------------------------------------------

using System;

namespace Warlords.DesktopGL
{
    /// <summary>
    /// The main class - entry point for the DesktopGL platform
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new WarlordsGame())
                game.Run();
        }
    }
}
