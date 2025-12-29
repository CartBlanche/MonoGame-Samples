using System;

namespace Blank.Windows
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
