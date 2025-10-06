using Android.App;
using Android.OS;
using Microsoft.Xna.Framework;

namespace NetRumble.Android
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@drawable/icon",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
    )]
    public class MainActivity : AndroidGameActivity
    {
        private NetRumbleGame _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new NetRumbleGame();

            _view = _game.Services.GetService(typeof(View)) as View;
            SetContentView(_view);

            _game.Run();
        }
    }
}