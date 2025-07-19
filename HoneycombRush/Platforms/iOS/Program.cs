using System;

namespace HoneycombRush.iOS
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
