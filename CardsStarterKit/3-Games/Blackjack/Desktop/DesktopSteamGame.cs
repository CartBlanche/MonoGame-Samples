using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net.Steam;

namespace Blackjack.DesktopGL
{
    public sealed class DesktopSteamGame : BlackjackGame
    {
        protected override void Update(GameTime gameTime)
        {
            SteamRuntime.RunCallbacks();
            base.Update(gameTime);
        }
    }
}