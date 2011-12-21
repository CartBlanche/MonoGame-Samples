using Android.App;
using Android.OS;
using Android.Content.PM;

namespace Platformer
{
    [Activity(Label = "Platformer", MainLauncher = true, Icon = "@drawable/icon",ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
    public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            PlatformerGame.Activity = this;
            PlatformerGame g = new PlatformerGame();
            SetContentView(g.Window);
            g.Run();
        }
    }
}