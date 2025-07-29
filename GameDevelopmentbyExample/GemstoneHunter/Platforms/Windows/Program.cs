
using System;

namespace GemstoneHunter.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new GemstoneHunterGame())
                game.Run();
        }
    }
}