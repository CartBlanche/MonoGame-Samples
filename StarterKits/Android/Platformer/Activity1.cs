using Android.App;
using Android.OS;

namespace Platformer
{
    [Activity(Label = "Platformer", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            PlatformerGame g = new PlatformerGame(this);
            SetContentView(g.Window);
            g.Run();
        }
    }
}