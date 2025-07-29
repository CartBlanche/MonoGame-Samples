using Android.App;
using Android.OS;
using Microsoft.Xna.Framework;

namespace NetRumble.Android
{
    [Activity(Label = "NetRumble", MainLauncher = true, Icon = "@mipmap/icon", Theme = "@style/MainTheme")]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new NetRumbleGame();
            SetContentView((game.Services.GetService(typeof(Android.Views.View)) as Android.Views.View));
            game.Run();
        }
    }
}
