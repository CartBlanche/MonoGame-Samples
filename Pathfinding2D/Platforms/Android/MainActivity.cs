using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;
using Pathfinding;

namespace PathfindingSample.Android
{
    [Activity(
        Label = "PathfindingSample",
        MainLauncher = true,
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new PathfindingSampleGame();
            SetContentView((game.Services.GetService(typeof(Android.Views.View)) as Android.Views.View));
            game.Run();
        }
    }
}
