// Entry point for Android platform
using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;
using TexturedQuad.Core;

namespace TexturedQuad.Android
{
    [Activity(Label = "TexturedQuad", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation | ConfigChanges.UiMode, ScreenOrientation = ScreenOrientation.FullSensor)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var g = new TexturedQuadGame();
            SetContentView((g.Services.GetService(typeof(View))) as View);
            g.Run();
        }
    }
}
