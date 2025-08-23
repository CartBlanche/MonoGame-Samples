using Foundation;
using UIKit;

namespace ShaderTest.iOS
{
    /// <summary>
    /// The main entry point for the iOS application.
    /// </summary>
    public class Application
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }

    public class AppDelegate : UIApplicationDelegate
    {
        ShaderTestGame game;
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new ShaderTestGame();
            game.Run();
            return true;
        }
    }
}
