using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace CatapultGame
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
    )]
    public class MainActivity : AndroidGameActivity
    {
        private CatapultGame _game;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new CatapultGame();
            SetContentView((View)_game.Services.GetService(typeof(View)));
            _game.Run();
        }
    }
}