using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace FarseerPhysics
{
    [Activity(
        Label = "Farseer Samples",
        MainLauncher = true,
        Icon = "@drawable/icon",
        Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
    )]
    public class MainActivity : AndroidGameActivity
    {
        private FarseerPhysicsGame _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new FarseerPhysicsGame();
            _view = _game.Services.GetService(typeof(View)) as View;

            SetContentView(_view);
            _game.Run();
        }
    }
}
