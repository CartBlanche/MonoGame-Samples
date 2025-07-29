using Foundation;
using UIKit;

namespace RolePlaying.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private static RolePlayingGame _game;

        internal static void RunGame()
        {
            _game = new RolePlayingGame();
            _game.Run();
        }

        public override void FinishedLaunching(UIApplication app)
        {
            RunGame();
        }

        public static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
