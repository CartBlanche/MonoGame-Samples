using System;

namespace RenderTarget2DSample.Windows
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using var game = new RenderTarget2DSampleGame();
            game.Run();
        }
    }
}