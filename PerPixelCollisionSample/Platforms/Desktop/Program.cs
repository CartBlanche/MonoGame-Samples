using System;

namespace PerPixelCollision.Desktop
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new PerPixelCollisionGame())
                game.Run();
        }
    }
}
