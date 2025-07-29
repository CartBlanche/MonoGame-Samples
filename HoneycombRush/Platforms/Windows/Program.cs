using System;

namespace HoneycombRush.Windows
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new HoneycombRushGame())
            {
                game.Run();
            }
        }
    }
}
