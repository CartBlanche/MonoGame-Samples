// Entry point for Windows platform
using System;
using TexturedQuad.Core;

namespace TexturedQuad.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new TexturedQuadGame())
                game.Run();
        }
    }
}
