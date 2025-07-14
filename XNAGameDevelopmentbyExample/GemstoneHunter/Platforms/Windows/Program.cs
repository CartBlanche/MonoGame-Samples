
using System;
using Gemstone_Hunter;

namespace GemstoneHunter.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
}
