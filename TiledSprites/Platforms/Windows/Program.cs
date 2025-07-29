using System;

namespace TiledSprites.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new TiledSprites.TiledSpritesGame())
                game.Run();
        }
    }
}
