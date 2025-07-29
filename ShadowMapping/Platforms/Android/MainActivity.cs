using Android.App;
using Android.OS;
using Microsoft.Xna.Framework;

namespace ShadowMapping.Android
{
    [Activity(Label = "ShadowMapping", MainLauncher = true, Icon = "@mipmap/icon", Theme = "@style/MainTheme", ConfigurationChanges = Android.Content.PM.ConfigChanges.ScreenSize | Android.Content.PM.ConfigChanges.Orientation)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var game = new ShadowMappingGame();
            SetContentView((View)game.Services.GetService(typeof(View)));
            game.Run();
        }
    }
}
