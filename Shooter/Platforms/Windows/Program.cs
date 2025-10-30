using Shooter;

namespace Shooter.Windows
{
    internal static class Program
    {
        private static void Main()
        {
            using (var game = new ShooterGame())
                game.Run();
        }
    }
}
