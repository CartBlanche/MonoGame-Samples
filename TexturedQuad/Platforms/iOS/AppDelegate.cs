// Entry point for iOS platform
using Foundation;
using UIKit;
using TexturedQuad.Core;

namespace TexturedQuad.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }
        TexturedQuadGame game;
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new TexturedQuadGame();
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
