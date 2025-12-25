using System;

namespace Blackjack.Windows
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
