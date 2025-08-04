using System;

namespace InputSamples.Windows
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
