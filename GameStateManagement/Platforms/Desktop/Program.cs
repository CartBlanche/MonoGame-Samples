using System;

namespace GameStateManagement.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new GameStateManagementGame())
                game.Run();
        }
    }
}
