using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Main menu: PLAY | SETTINGS | QUIT (QUIT omitted on iOS). PLAY opens a sub-list: MULTIPLAYER (disabled, coming soon; will later host/join like
    /// RacingGame) and SINGLEPLAYER. Esc/B on the sub-list goes back; on the root it does nothing (use QUIT). Layout from menu.tscn (buttons at x=100, 100 px apart).
    /// </summary>
    public sealed class MainMenuScreen : UiScreen
    {
        public enum Page { Root, PlayList }
        readonly UiFocus _root = new(), _playList = new();
        public Page Current { get; private set; } = Page.Root;
        public UiButton MultiplayerButton, SingleplayerButton, PlayButton;

        public MainMenuScreen(Texture2D logo, Action singleplayer, Action settings, Action quit)
        {
            if (logo != null) { var img = new UiImage(logo, new Point(20, 5)); _root.Add(img); _playList.Add(new UiImage(logo, new Point(20, 5))); } // menu.tscn TextureRect offset (20.1, 5.0)
            int y = UiTheme.ButtonY0;
            PlayButton = _root.Add(new UiButton("PLAY", UiTheme.ButtonX, y, clicked: () => Show(Page.PlayList))); y += UiTheme.ButtonStep;
            _root.Add(new UiButton("SETTINGS", UiTheme.ButtonX, y, clicked: settings)); y += UiTheme.ButtonStep;
            if (!OperatingSystem.IsIOS()) // iOS apps must not offer Quit
                _root.Add(new UiButton("QUIT", UiTheme.ButtonX, y, clicked: quit));
            _root.BackPressed = () => { if (!OperatingSystem.IsIOS()) quit(); }; // Esc/B on the root list asks to quit, like the QUIT button

            y = UiTheme.ButtonY0;
            MultiplayerButton = _playList.Add(new UiButton("MULTIPLAYER", UiTheme.ButtonX, y) { Enabled = false, Note = "COMING SOON" }); y += UiTheme.ButtonStep;
            SingleplayerButton = _playList.Add(new UiButton("SINGLEPLAYER", UiTheme.ButtonX, y, clicked: singleplayer)); y += UiTheme.ButtonStep;
            _playList.Add(new UiButton("BACK", UiTheme.ButtonX, y, clicked: () => Show(Page.Root)));
            _playList.BackPressed = () => Show(Page.Root);
            _root.FocusFirst(); _playList.FocusFirst();
            Focus = _root;
        }

        public void Show(Page p) { Current = p; Focus = p == Page.Root ? _root : _playList; if (Focus.Focused == null) Focus.FocusFirst(); }
        /// <summary>Back to the root list with focus on PLAY (after leaving the game).</summary>
        public void Reset() { Show(Page.Root); _root.SetFocus(PlayButton); _playList.FocusFirst(); }
        public UiFocus PlayListFocus => _playList;
    }
}
