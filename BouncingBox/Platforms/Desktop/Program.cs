using System;
using Microsoft.Xna.Samples.BouncingBox;

namespace BouncingBox.Desktop
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
