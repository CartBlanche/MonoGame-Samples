using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Samples.MultiTouch
{
    [Activity(
        Label = "MultiTouch",
        MainLauncher = true,
        Icon = "@drawable/icon",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.FullUser,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout)]
    public class MainActivity : AndroidGameActivity
    {
        private Game1 _game;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new Game1();
            var frameLayout = new FrameLayout(this);
            frameLayout.AddView((View)_game.Services.GetService(typeof(View)));

            SetContentView(frameLayout);
            _game.Run();
        }
    }
}
