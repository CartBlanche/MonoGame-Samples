using System;

namespace PacMan.iOS
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new PacManGame())
            {
                game.Run();
            }
        }
    }
}
