using System;

namespace ParticleSample
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (ParticleSampleGame game = new ParticleSampleGame())
            {
                game.Run();
            }
        }
    }
}

