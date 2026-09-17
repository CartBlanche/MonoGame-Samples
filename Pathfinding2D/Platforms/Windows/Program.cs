using System;
using MonoGame.Framework;
using Pathfinding;

namespace PathfindingSample.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new PathfindingSampleGame())
                game.Run();
        }
    }
}
