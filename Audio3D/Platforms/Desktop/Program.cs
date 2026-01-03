using System;

namespace Audio3D.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Audio3DGame())
                game.Run();
        }
    }
}
