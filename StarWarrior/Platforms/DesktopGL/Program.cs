// Entry point for DesktopGL platform
using System;

namespace StarWarrior.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new StarWarriorGame())
                game.Run();
        }
    }
}
