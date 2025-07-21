using Android.App;
using Android.Content.PM;
using Android.OS;
using MonoGame.Framework;

namespace NetworkPrediction.Android
{
    [Activity(Label = "NetworkPrediction", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new NetworkPredictionGame();
            SetContentView((game.Services.GetService(typeof(Android.Views.View)) as Android.Views.View));
            game.Run();
        }
    }
}
