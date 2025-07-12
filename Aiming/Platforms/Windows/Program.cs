using System;

namespace Aiming.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new Aiming.AimingGame();
            game.Run();
        }
    }
}
