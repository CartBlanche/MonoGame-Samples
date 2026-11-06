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

        // remembered for the session, so the cursor returns to LAN race after a LAN race
        static int lastSelection = 0;

        int selection = lastSelection;
        bool ignoreMouse = true;

        public void Update(GameTime gameTime)
        {
        }

        void Choose(int index)
        {
            Sound.Play(Sound.Sounds.ButtonClick);
            selection = index;
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

            if (Input.HasMouseMoved || Input.MouseLeftButtonJustPressed)
                ignoreMouse = false;

            // two big buttons (outline proportions 212 x 92), one above the other
            int mouseOver = -1;
            int buttonWidth = BaseGame.XToRes(300);
            int buttonHeight = buttonWidth * 92 / 212;
            int y = BaseGame.YToRes(215);
            for (int i = 0; i < Entries.Length; i++)
            {
                Rectangle box = new Rectangle(centerX - buttonWidth / 2, y - buttonHeight / 2,
                    buttonWidth, buttonHeight);
                if (Input.MouseInBox(box))
                    mouseOver = i;
                BaseGame.UI.RenderPanelButton(box, Entries[i], i == selection, 1.4f);
                y += buttonHeight + BaseGame.YToRes(30);
            }

            if (!ignoreMouse && mouseOver >= 0)
                selection = mouseOver;

            // Shared bottom buttons: Back (left), Select (right)
            bool selectClicked = BaseGame.UI.RenderBottomButtons(false);

            if (Input.KeyboardUpJustPressed || Input.GamePadUpJustPressed)
            {
                Sound.Play(Sound.Sounds.Highlight);
                selection = (selection + Entries.Length - 1) % Entries.Length;
                ignoreMouse = true;
            }
            if (Input.KeyboardDownJustPressed || Input.GamePadDownJustPressed)
            {
                Sound.Play(Sound.Sounds.Highlight);
                selection = (selection + 1) % Entries.Length;
                ignoreMouse = true;
            }

            if (Input.KeyboardSpaceJustPressed || Input.GamePadAJustPressed ||
                Input.KeyboardKeyJustPressed(Keys.Enter) || selectClicked)
                Choose(selection);
            else if (mouseOver >= 0 && Input.MouseLeftButtonJustPressed)
                Choose(mouseOver);

            if (Input.KeyboardEscapeJustPressed || Input.GamePadBJustPressed ||
                Input.GamePadBackJustPressed || BaseGame.UI.backButtonPressed)
                leave = true;

            return leave;
        }
    }
}
