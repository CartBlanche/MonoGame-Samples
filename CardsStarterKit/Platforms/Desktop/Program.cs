using System;

namespace Blackjack.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new BlackjackGame())
                game.Run();
        }
    }
}
