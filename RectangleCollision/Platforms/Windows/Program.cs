using System;

namespace RectangleCollision.Windows
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var game = new RectangleCollisionGame();
            game.Run();
        }
    }
}