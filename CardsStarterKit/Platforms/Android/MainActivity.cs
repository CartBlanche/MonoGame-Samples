using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Blackjack;
using Microsoft.Xna.Framework;

namespace BlackJack.Android
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
        private BlackjackGame _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new BlackjackGame();

            _view = _game.Services.GetService(typeof(View)) as View;
            SetContentView(_view);

            _game.Run();
        }
    }
}