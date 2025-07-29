using System;

namespace HoneycombRush.DesktopGL
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new HoneycombRushGame())
            {
                game.Run();
            }
        }
    }
}
