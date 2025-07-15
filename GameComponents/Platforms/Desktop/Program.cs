using System;
using Microsoft.Xna.Framework;

namespace GameComponents.DesktopGL
{
    public static class Program
    {
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
}
