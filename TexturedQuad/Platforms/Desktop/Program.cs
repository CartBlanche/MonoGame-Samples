// Entry point for DesktopGL platform
using System;
using TexturedQuad.Core;

namespace TexturedQuad.DesktopGL
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
