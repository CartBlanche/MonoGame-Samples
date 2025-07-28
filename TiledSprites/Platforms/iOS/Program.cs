using UIKit;

namespace TiledSprites.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }

    public class AppDelegate : UIApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            using (var game = new TiledSprites.TiledSpritesGame())
            {
                game.Run();
            }
            return true;
        }
    }
}
