using System;

namespace Colored3DCube.Windows
{
    public static class Platform
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new Colored3DCube.Game1())
                game.Run();
        }
    }
}
