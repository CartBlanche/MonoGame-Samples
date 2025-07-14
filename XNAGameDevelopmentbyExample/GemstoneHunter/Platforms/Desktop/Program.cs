using Gemstone_Hunter;

namespace GemstoneHunter.DesktopGL
{
    public static class Program
    {
        public static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
}
