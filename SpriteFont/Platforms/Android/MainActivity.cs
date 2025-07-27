using Android.App;
using Android.Content.PM;
using Android.OS;
using SpriteFontSample.Core;

namespace SpriteFontSample.Android
{
    [Activity(Label = "SpriteFont", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var g = new SpriteFontGame();
            SetContentView((g.Services.GetService(typeof(Android.Views.View))) as Android.Views.View);
            g.Run();
        }
    }
}
