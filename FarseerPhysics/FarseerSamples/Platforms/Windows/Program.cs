using System;

namespace FarseerPhysics.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new FarseerPhysicsGame())
                game.Run();
        }
    }
}
