using Android.App;
using Android.OS;


namespace ShaderTest.Android
{
    /// <summary>
    /// The main entry point for the Android application.
    /// </summary>
    [Activity(Label = "2DShaderSample", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        /// <summary>
        /// Called when the activity is first created.
        /// </summary>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new ShaderTestGame();
            game.Run();
        }
    }
}
