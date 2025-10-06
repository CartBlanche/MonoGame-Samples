using System;
using Microsoft.Xna.Framework;

namespace Particle3DSample.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Particle3DSampleGame())
                game.Run();
        }
    }
}
