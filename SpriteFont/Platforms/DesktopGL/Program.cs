using System;
using SpriteFontSample.Core;

namespace SpriteFontSample.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new SpriteFontGame())
                game.Run();
        }
    }
}
