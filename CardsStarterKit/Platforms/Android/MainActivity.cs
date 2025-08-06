using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;

namespace BlackJack.Android
{
    [Activity(Label = "Blackjack", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize, ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var g = new BlackjackGame();
            SetContentView((g.Services.GetService(typeof(View))) as View);
            g.Run();
        }
    }
}
