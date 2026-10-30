//-----------------------------------------------------------------------------
// PlayMenuScreen.cs
//
// Chooses between a game against the AI and a game on the local network.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace CatapultGame
{
    class PlayMenuScreen : MenuScreen
    {
        public PlayMenuScreen()
            : base(String.Empty)
        {
            // Not a popup, so the main menu behind it fades out instead of showing through.

            MenuEntry singlePlayerMenuEntry = new MenuEntry("Single Player vs AI");
            MenuEntry multiPlayerMenuEntry = new MenuEntry("Multi Player on LAN");
            MenuEntry backMenuEntry = new MenuEntry(Resources.Back);

            singlePlayerMenuEntry.Selected += SinglePlayerMenuEntrySelected;
            multiPlayerMenuEntry.Selected += MultiPlayerMenuEntrySelected;
            backMenuEntry.Selected += OnCancel;

            MenuEntries.Add(singlePlayerMenuEntry);
            MenuEntries.Add(multiPlayerMenuEntry);
            MenuEntries.Add(backMenuEntry);
        }

        protected override void UpdateMenuEntryLocations()
        {
            base.UpdateMenuEntryLocations();

            foreach (var entry in MenuEntries)
            {
                Vector2 position = entry.Position;
                position.Y += 60;
                entry.Position = position;
            }
        }

        void SinglePlayerMenuEntrySelected(object sender, EventArgs e)
        {
            // Lets make sure we get rid of our network session
            // so we can start up clean
            ScreenManager.Game.Services.RemoveService(typeof(NetworkSession));
            MatchSettings.WinScore = MatchSettings.DefaultWinScore;
            ScreenManager.AddScreen(new InstructionsScreen(), null);
        }

        void MultiPlayerMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // First, we need to make sure a suitable gamer profile is signed in.
            ProfileSignInScreen profileSignIn = new ProfileSignInScreen(NetworkSessionType.SystemLink);

            // Once the ProfileSignInScreen is happy, offer to create or find a session.
            profileSignIn.ProfileSignedIn += delegate
            {
                ScreenManager.AddScreen(new CreateOrFindSessionScreen(NetworkSessionType.SystemLink), e.PlayerIndex);
            };

            ScreenManager.AddScreen(profileSignIn, e.PlayerIndex);
        }
    }
}
