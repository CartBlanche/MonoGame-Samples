using System;

namespace LensFlare.DesktopGL
{
    /// <summary>
    /// The main class for DesktopGL.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new LensFlareGame())
                game.Run();
        }
    }
}
