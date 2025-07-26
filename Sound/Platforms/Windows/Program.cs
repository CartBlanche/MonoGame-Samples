using System;

namespace SoundSample.Windows
{
    static class Program
    {
        private static SoundGame game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            game = new SoundGame();
            game.Run();
        }
    }
}
