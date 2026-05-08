using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net.Steam;

namespace NetworkStateManagement.DesktopGL
{
    /// <summary>
    /// Desktop wrapper that pumps Steam callbacks every frame.
    /// </summary>
    public sealed class DesktopSteamGame : NetworkStateManagementGame
    {
        protected override void Update(GameTime gameTime)
        {
            SteamRuntime.RunCallbacks();
            base.Update(gameTime);
        }
    }
}
