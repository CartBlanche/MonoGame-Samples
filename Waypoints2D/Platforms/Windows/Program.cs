using System;

namespace Waypoint.Windows
{
    /// <summary>
    /// The main class for Windows platform.
    /// </summary>
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new WaypointGame())
                game.Run();
        }
    }
}
