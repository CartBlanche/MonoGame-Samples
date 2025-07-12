using System;

namespace Aiming.DesktopGL
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
