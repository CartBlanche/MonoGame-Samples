using Android.App;
using Android.OS;
using Android.Content.PM;

namespace Storage.Platforms.Android
{
    [Activity(Label = "Storage", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var game = new StorageGame();
            SetContentView(game.Window.Handle);
            game.Run();
        }
    }
}
