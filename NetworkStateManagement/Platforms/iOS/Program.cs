using Foundation;
using UIKit;

namespace NetworkStateManagement.iOS
{
    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private NetworkStateManagementGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new NetworkStateManagementGame();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}