using System;

namespace Blank.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new BlankGame())
                game.Run();
        }
    }
}
