using Foundation;
using UIKit;

namespace SoundSample.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow? Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // Initialize and run the MonoGame game
            var game = new SoundGame();
            game.Run();
            return true;
        }
    }
}
