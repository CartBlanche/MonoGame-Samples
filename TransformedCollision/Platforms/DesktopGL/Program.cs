using System;
using TransformedCollision;

namespace TransformedCollision.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new TransformedCollisionGame())
                game.Run();
        }
    }
}
