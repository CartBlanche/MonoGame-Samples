using Android.App;
using Android.Content.PM;
using Android.OS;
using ChaseAndEvade;

namespace MonoGame.Samples.ChaseAndEvade.Droid
{
    [Activity(Label = "ChaseAndEvade", MainLauncher = true,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden,Icon = "@drawable/icon", Theme = "@style/Theme.Splash", NoHistory = true)]
    public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ChaseAndEvadeGame.Activity = this;
            ChaseAndEvadeGame g = new ChaseAndEvadeGame();
            SetContentView(g.Window);
            g.Run();
        }
    }
}

