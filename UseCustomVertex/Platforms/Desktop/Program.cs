using System;

namespace UseCustomVertex.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new UseCustomVertexGame())
                game.Run();
        }
    }
}
