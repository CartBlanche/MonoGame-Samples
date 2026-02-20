using System;

namespace RacingGame
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new RacingGameManager())
                game.Run();
        }
    }
}
