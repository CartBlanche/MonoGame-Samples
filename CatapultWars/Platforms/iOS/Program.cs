using System;

namespace CatapultGame
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new CatapultGame())
            {
                game.Run();
            }
        }
    }
}
