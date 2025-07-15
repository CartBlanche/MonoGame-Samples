using System;

namespace Flocking.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new FlockingSample())
                game.Run();
        }
    }
}
