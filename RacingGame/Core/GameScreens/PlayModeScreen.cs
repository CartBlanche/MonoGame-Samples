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
            int y = BaseGame.Height / 4;
            TextureFont.WriteTextCentered(centerX, y, "Start Race", Color.White, 1.5f);
            y += BaseGame.YToRes(100);

            if (Input.HasMouseMoved || Input.MouseLeftButtonJustPressed)
                ignoreMouse = false;

            int mouseOver = -1;
            int rowHeight = BaseGame.YToRes(45);
            for (int i = 0; i < Entries.Length; i++)
            {
                Rectangle row = new Rectangle(centerX - BaseGame.XToRes(250),
                    y - rowHeight / 2, BaseGame.XToRes(500), rowHeight);
                if (Input.MouseInBox(row))
                    mouseOver = i;

                bool selected = i == selection;
                TextureFont.WriteTextCentered(centerX, y,
                    selected ? "> " + Entries[i] + " <" : Entries[i],
                    selected ? Color.Yellow : Color.White, 1.2f);
                y += rowHeight + BaseGame.YToRes(10);
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
