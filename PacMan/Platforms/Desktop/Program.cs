using System;

namespace PacMan.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new PacManGame())
            {
                game.Run();
            }
        }
    }
}
