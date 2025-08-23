using System;

namespace ShatterEffect.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new ShatterEffectGame())
                game.Run();
        }
    }
}
