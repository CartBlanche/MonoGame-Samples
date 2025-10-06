using System;

namespace CatapultGame.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new CatapultGame())
                game.Run();
        }
    }
}
