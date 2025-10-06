using Android.App;
using Android.Content.PM;
using Android.OS;
using MonoGame.Framework;

namespace ParticleSample.Android
{
    [Activity(
        Label = "ParticleSample",
        MainLauncher = true,
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize,
        ScreenOrientation = ScreenOrientation.FullSensor
    )]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new ParticleSampleGame();
            SetContentView((game.Services.GetService(typeof(Android.Views.View)) as Android.Views.View));
            game.Run();
        }
    }
}
