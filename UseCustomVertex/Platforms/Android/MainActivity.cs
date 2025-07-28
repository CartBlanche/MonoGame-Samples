using Android.App;
using Android.OS;
using Microsoft.Xna.Framework;

namespace UseCustomVertex.Android
{
    [Activity(Label = "UseCustomVertex", MainLauncher = true, Icon = "@mipmap/icon", Theme = "@style/MainTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.ScreenSize | Android.Content.PM.ConfigChanges.Orientation)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var g = new .UseCustomVertexGame();
            SetContentView((g.Services.GetService(typeof(View))) as View);
            g.Run();
        }
    }
}
