// Entry point for Android platform
using Android.App;
using Android.OS;
using Android.Content.PM;
using Microsoft.Xna.Framework;

namespace StarWarrior.Android
{
    [Activity(Label = "StarWarrior", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@android:style/Theme.NoTitleBar", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize, ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var g = new StarWarriorGame();
            SetContentView((View)g.Services.GetService(typeof(View)));
            g.Run();
        }
    }
}
