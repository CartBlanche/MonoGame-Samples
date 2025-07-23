using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace RectangleCollision.Android
{
    [Activity(
        Label = "RectangleCollisionSample",
        MainLauncher = true,
        Icon = "@mipmap/icon",
        Theme = "@android:style/Theme.NoTitleBar",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize,
        ScreenOrientation = ScreenOrientation.SensorLandscape
    )]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new RectangleCollisionGame();
            SetContentView((game.Services.GetService(typeof(View)) as View));
            game.Run();
        }
    }
}
