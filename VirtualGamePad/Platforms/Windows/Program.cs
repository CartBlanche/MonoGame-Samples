using System;
using Microsoft.Xna.Framework;

namespace VirtualGamePad.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new VirtualGamePadGame())
                game.Run();
        }
    }
}
