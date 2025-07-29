using System;

namespace GooCursor.Platforms.Desktop
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var game = new GooCursor.Game1();
            game.Run();
        }
    }
}
