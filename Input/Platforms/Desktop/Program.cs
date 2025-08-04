using System;

namespace InputSample.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new InputGame())
                game.Run();
        }
    }
}
