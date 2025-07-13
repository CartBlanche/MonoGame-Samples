using System;

namespace Flood_Control
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new Game1())
            {
                game.Run();
            }
        }
    }
}
