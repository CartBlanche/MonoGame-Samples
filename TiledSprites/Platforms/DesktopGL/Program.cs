using System;

namespace TiledSprites.DesktopGL
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
