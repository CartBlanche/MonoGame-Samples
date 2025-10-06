// DesktopGL entry point
namespace Graphics3DSample.DesktopGL
{
    public static class Program
    {
        public static void Main()
        {
            using (var game = new Graphics3DSampleGame())
                game.Run();
        }
    }
}
