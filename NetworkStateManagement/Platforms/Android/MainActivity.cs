using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using Microsoft.Xna.Framework;

namespace NetworkStateManagement
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
        private NetworkStateManagementGame _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new NetworkStateManagementGame();

            _view = _game.Services.GetService(typeof(View)) as View;
            SetContentView(_view);

            _game.Run();
        }
    }
}
