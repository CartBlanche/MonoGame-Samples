using System;

namespace OrientationSample.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new OrientationSampleGame())
                game.Run();
        }
    }
}
