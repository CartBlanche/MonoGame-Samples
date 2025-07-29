using System;
using Microsoft.Xna.Framework;

namespace ShadowMapping.Desktop
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new ShadowMappingGame())
                game.Run();
        }
    }
}