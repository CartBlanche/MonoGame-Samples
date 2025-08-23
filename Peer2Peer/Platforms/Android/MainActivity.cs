using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;

namespace PeerToPeer.Android
{
    [Activity(Label = "Peer2Peer", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
        Icon = "@drawable/icon",
        Theme = "@style/Theme.Splash",
        NoHistory = true)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var game = new PeerToPeerGame();
            SetContentView(game.Window);
            game.Run();
        }
    }
}