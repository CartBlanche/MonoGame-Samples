using System;

namespace Flocking.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new FlockingSample())
                game.Run();
        }
    }
}
