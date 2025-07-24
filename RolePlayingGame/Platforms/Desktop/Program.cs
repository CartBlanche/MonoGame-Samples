using Microsoft.Xna.Framework;

namespace RolePlaying.Desktop
{
    internal static class Program
    {
        private static void Main()
        {
            using (var game = new RolePlayingGame())
                game.Run();
        }
    }
}
