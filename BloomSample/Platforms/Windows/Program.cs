using System;
using BloomSample.Core;

namespace BloomSample.Windows
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
}

// Windows-specific entry point will be added here.
