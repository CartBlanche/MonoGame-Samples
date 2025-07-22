using System;

namespace OrientationSample.Desktop
{
    public static class Program
    {
        static void Main()
        {
            using (var game = new OrientationSampleGame())
                game.Run();
        }
    }
}
