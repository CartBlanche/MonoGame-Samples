using Foundation;
using UIKit;

namespace Tetris.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }
        TetrisGame game;
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new TetrisGame();
            game.Run();
            return true;
        }
    }

    public class Application
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
