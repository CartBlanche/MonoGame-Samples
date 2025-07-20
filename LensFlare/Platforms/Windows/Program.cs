using System;

namespace LensFlare.Windows
{
    /// <summary>
    /// The main class for Windows.
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
