using System;

namespace VideoPlayer
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (var game = new VideoPlayerGame())
                game.Run();
        }
    }
}
