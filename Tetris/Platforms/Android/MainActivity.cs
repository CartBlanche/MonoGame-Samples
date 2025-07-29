using Android.App;
using Android.OS;
using Microsoft.Xna.Framework;

namespace Tetris.Android
{
    [Activity(Label = "Tetris", MainLauncher = true, Icon = "@mipmap/icon", Theme = "@style/MainTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var g = new TetrisGame();
            SetContentView((g.Services.GetService(typeof(View))) as View);
            g.Run();
        }
    }
}
