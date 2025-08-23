using System;
using MonoGame.Framework;

namespace ParticleSample.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new ParticleSampleGame())
                game.Run();
        }
    }
}
