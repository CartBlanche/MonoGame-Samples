using System;

namespace GameStateManagement.WindowsDX
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
