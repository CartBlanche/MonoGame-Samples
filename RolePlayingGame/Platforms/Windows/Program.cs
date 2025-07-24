using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RolePlaying.Windows
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
