using AppKit;

namespace ShipGame
{
    static class MainClass
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            NSApplication.Init();

            using (var game = new ShipGameGame())
            {
                game.Run();
            }
        }
    }
}
