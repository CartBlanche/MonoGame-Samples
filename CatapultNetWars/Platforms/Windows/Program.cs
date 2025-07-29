using System;

namespace CatapultGame
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
