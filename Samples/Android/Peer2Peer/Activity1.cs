using Android.App;
using Android.Content.PM;
using Android.OS;
using PeerToPeer;

namespace MonoGame.Samples.PeerToPeerGame.Droid
{
    [Activity(Label = "Peer2Peer", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , NoHistory = true)]
    public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
			PeerToPeer.PeerToPeerGame.Activity = this;
            var g = new PeerToPeer.PeerToPeerGame();
            SetContentView(g.Window);
            g.Run();
        }
    }
}

