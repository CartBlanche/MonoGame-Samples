using System;
using MonoGame.Framework;

namespace ParticleSample.iOS
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new ParticleSampleGame())
                game.Run();
        }
    }
}
