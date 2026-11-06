using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Helpers;
using RacingGame.Sounds;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// Shown when "Start Race" is chosen on the main menu: Single player, LAN race or Back.
    /// </summary>
    class PlayModeScreen : IGameScreen
    {
        static readonly string[] Entries = { "Single player", "Multiplayer" };

        // remembered for the session, so the cursor returns to Multiplayer after a multiplayer race
        static int lastSelection = 0;

        readonly PanelButtonGroup buttons = new PanelButtonGroup(lastSelection);

        public void Update(GameTime gameTime)
        {
        }

        void Choose(int index)
        {
            Sound.Play(Sound.Sounds.ButtonClick);
            buttons.Select(index);
            lastSelection = index;

            switch (index)
            {
                case 0:
                    RacingGameManager.AddGameScreen(new CarSelection());
                    break;
                case 1:
                    RacingGameManager.AddGameScreen(new LanBrowserScreen());
                    break;
            }
        }

        bool leave;

        public bool Render()
        {
            if (BaseGame.UsePostScreenShaders)
                BaseGame.UI.PostScreenMenuShader.Start();

            BaseGame.UI.RenderMenuBackground();

            int centerX = BaseGame.Width / 2;
            TextureFont.WriteTextCentered(centerX, BaseGame.YToRes(110), "Start Race", Color.White, 1.5f);

            // two big buttons (outline proportions 212 x 92), one above the other
            int buttonWidth = BaseGame.XToRes(300);
            int buttonHeight = buttonWidth * 92 / 212;
            Rectangle[] rects = new Rectangle[Entries.Length];
            int y = BaseGame.YToRes(215);
            for (int i = 0; i < Entries.Length; i++)
            {
                rects[i] = new Rectangle(centerX - buttonWidth / 2, y - buttonHeight / 2, buttonWidth, buttonHeight);
                y += buttonHeight + BaseGame.YToRes(30);
            }
            int clicked = buttons.Render(rects, Entries, 1.4f);

            // Shared bottom buttons: Back (left), Select (right)
            bool selectClicked = BaseGame.UI.RenderBottomButtons(false);

            if (Input.KeyboardUpJustPressed || Input.GamePadUpJustPressed)
            {
                Sound.Play(Sound.Sounds.Highlight);
                buttons.Move(-1, Entries.Length);
            }
            if (Input.KeyboardDownJustPressed || Input.GamePadDownJustPressed)
            {
                Sound.Play(Sound.Sounds.Highlight);
                buttons.Move(1, Entries.Length);
            }

            if (clicked >= 0)
                Choose(clicked);
            else if (Input.KeyboardSpaceJustPressed || Input.GamePadAJustPressed ||
                Input.KeyboardKeyJustPressed(Keys.Enter) || selectClicked)
                Choose(buttons.Selection);

            if (Input.KeyboardEscapeJustPressed || Input.GamePadBJustPressed ||
                Input.GamePadBackJustPressed || BaseGame.UI.backButtonPressed)
                leave = true;

            return leave;
        }
    }
}
