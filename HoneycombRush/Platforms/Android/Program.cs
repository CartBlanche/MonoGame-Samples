using System;

namespace HoneycombRush.Android
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
