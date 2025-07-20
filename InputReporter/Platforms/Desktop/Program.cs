using System;

namespace InputReporter
{
    /// <summary>
    /// The main class for DesktopGL platform.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using var game = new InputReporterGame();
            game.Run();
        }
    }
}