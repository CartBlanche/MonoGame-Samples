using Android.App;
using Android.OS;
using Android.Content.PM;
using Microsoft.Xna.Framework;

namespace GameStateManagement.Android
{
    [Activity(Label = "GameStateManagement", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var g = new GameStateManagementGame();
            SetContentView((View)g.Services.GetService(typeof(View)));
            g.Run();
        }
    }
}
