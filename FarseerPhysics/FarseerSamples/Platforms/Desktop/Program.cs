using System;

namespace FarseerPhysics.DesktopGL
{
    public static class Program
    {
        static void Main()
        {
            using (var game = new FarseerPhysicsGame())
                game.Run();
        }
    }
}
