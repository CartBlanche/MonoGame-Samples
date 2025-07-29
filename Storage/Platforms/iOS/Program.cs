using Foundation;
using UIKit;

namespace Storage.Platforms.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private StorageGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new StorageGame();
            game.Run();
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
