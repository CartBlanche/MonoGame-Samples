using System;

namespace RobotRampage.DesktopGL
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (var game = new RobotRampageGame())
            {
                game.Run();
            }
        }
    }
}

