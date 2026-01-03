using System;

namespace StencilCraters.DesktopGL
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new StencilCratersGame())
            {
                game.Run();
            }
        }
    }
}
