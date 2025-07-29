using System;
using Microsoft.Xna.Framework;
using System.Windows.Forms;

namespace ShadowMapping.Windows
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