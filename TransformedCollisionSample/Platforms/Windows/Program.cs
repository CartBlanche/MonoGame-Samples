using System;

namespace TransformedCollision.Windows
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
