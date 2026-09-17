using System;
using MonoGame.Framework;
using Pathfinding;

namespace PathfindingSample.iOS
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new PathfindingSampleGame())
                game.Run();
        }
    }
}
