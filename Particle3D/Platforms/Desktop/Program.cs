using System;
using Microsoft.Xna.Framework;

namespace Particle3DSample.DesktopGL
{
    public static class Program
    {
        static void Main()
        {
            using (var game = new Particle3DSampleGame())
                game.Run();
        }
    }
}
