using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;

namespace Aiming.Android
{
    [Activity (Label = "Aiming Sample", MainLauncher = true
	          , Icon = "@drawable/icon", Theme = "@style/Theme.Splash",ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]	
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new AimingGame();
            SetContentView((game.Services.GetService(typeof(Android.Views.View))) as Android.Views.View);
            game.Run();
        }
    }
}
