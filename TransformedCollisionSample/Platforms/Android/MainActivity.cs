using System;
using TransformedCollision;
using Android.App;
using Android.OS;

namespace TransformedCollision.Android
{
    [Activity(Label = "TransformedCollision", MainLauncher = true, Icon = "@mipmap/icon", Theme = "@style/MainTheme")]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new TransformedCollisionGame();
            SetContentView((game.Services.GetService(typeof(View))) as View);
            game.Run();
        }
    }
}
