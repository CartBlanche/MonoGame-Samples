using Android.App;
using Android.Content.PM;
using Android.OS;

namespace ShatterEffect.Android
{
    [Activity(Label = "ShatterEffectSample", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var g = new ShatterEffectGame();
            SetContentView((View)g.Services.GetService(typeof(View)));
            g.Run();
        }
    }
}
