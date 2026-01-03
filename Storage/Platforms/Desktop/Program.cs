using System;

namespace Storage.Platforms.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new StorageGame())
                game.Run();
        }
    }
}
