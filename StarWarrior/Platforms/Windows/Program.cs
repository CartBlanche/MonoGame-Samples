// Entry point for Windows platform
using System;

namespace StarWarrior.Windows
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
