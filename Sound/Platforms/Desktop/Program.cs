using System;

namespace SoundSample.DesktopGL
{
    static class Program
    {
        private static SoundGame game;

        static void Main()
        {
            game = new SoundGame();
            game.Run();
        }
    }
}
