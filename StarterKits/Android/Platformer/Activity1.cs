using Android.App;
using Android.OS;

namespace Platformer
{
    [Activity(Label = "Platformer", MainLauncher = true, Icon = "@drawable/icon")]
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