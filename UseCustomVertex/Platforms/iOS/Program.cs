using Foundation;
using UIKit;

namespace UseCustomVertex.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }

    public class AppDelegate : UIKit.UIApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var game = new UseCustomVertexGame();
            game.Run();
            return true;
        }
    }
}
