

namespace GemstoneHunter.DesktopGL
{
    public static class Program
    {
        public static void Main()
        {
            using (var game = new GemstoneHunterGame())
                game.Run();
        }
    }
}
