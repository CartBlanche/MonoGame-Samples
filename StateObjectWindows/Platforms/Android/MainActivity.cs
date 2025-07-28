using Android.Views;
using StateObject.Core;
using Android.App;
using Android.OS;
using Android.Content.PM;
using Microsoft.Xna.Framework;


namespace StateObject.Android
{
    [Activity(Label = "StateObjectAndroid", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new StateObjectGame();
            SetContentView((game.Services.GetService(typeof(View))) as View);
            game.Run();
        }
    }
}
