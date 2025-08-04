using Foundation;
using UIKit;

namespace BatteryStatus.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        BatteryStatusGame? game;
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var powerStatus = new PowerStatus();
            game = new BatteryStatusGame(powerStatus);
            game.Run();
            return true;
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}