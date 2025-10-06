using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace PerPixelCollision.Android
{
    [Activity(
        Label = "Per Pixel Collision Sample", 
        MainLauncher = true,
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.UserLandscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
    )]
    public class MainActivity : AndroidGameActivity
    {
        private PerPixelCollisionGame _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new PerPixelCollisionGame();
            _view = _game.Services.GetService(typeof(View)) as View;
            SetContentView(_view);
            _game.Run();
        }
    }
}
