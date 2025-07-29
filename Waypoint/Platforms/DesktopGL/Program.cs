using System;

namespace Waypoint.DesktopGL
{
    /// <summary>
    /// The main class for DesktopGL platform.
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
