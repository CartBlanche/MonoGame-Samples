using Foundation;
using UIKit;

namespace InputSample.iOS
{
    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private InputGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new InputGame();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
