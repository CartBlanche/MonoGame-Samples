using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Sounds;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// A reusable [No] [Yes] question (a "message box") over the menu background, in a framed
    /// panel. Use it with AddGameScreen. Choosing an answer closes it; Yes runs onYes first and
    /// No runs onNo (if given). Esc, B and the gamepad Back button choose No.
    /// Mouse, tap, keyboard (Left/Right, Enter, Space) and gamepad.
    ///
    /// Examples:
    ///   new ConfirmDialog("Quit", "Are you sure you want to quit?", () => quit, defaultYes: true)
    ///   new ConfirmDialog("Custom Tracks", "There are no custom tracks in", () => open,
    ///       defaultYes: true, detail: path, question: "Would you like to create some?")
    /// </summary>
    class ConfirmDialog : IGameScreen
    {
        readonly string title;
        readonly string[] message;
        readonly string detail;
        readonly string question;
        readonly string[] labels;
        readonly Action onYes, onNo;
        readonly PanelButtonGroup buttons;      // 0 = No, 1 = Yes

        /// <param name="title">Heading</param>
        /// <param name="message">The message; use '\n' for several lines</param>
        /// <param name="onYes">Run when Yes is chosen</param>
        /// <param name="defaultYes">Start with Yes highlighted (Enter then answers Yes), else No</param>
        /// <param name="detail">A long value such as a folder path: wrapped and drawn smaller (optional)</param>
        /// <param name="question">Text under the detail (optional)</param>
        /// <param name="onNo">Run when No is chosen (optional)</param>
        /// <param name="noLabel">Text of the No button</param>
        /// <param name="yesLabel">Text of the Yes button</param>
        public ConfirmDialog(string title, string message, Action onYes, bool defaultYes = false,
            string detail = null, string question = null, Action onNo = null,
            string noLabel = "No", string yesLabel = "Yes")
        {
            this.title = title;
            this.message = message.Split('\n');
            this.onYes = onYes;
            this.onNo = onNo;
            this.detail = detail;
            this.question = question;
            labels = new[] { noLabel, yesLabel };
            buttons = new PanelButtonGroup(defaultYes ? 1 : 0);
        }

        public void Update(GameTime gameTime)
        {
        }

        // break a long path into pieces that fit, preferably after a folder separator
        static List<string> Wrap(string text, int maxWidth, float scale)
        {
            List<string> result = new List<string>();
            while (text.Length > 0)
            {
                int length = text.Length;
                while (length > 1 && TextureFont.GetTextWidth(text.Substring(0, length)) * scale > maxWidth)
                    length--;

                if (length < text.Length)
                {
                    int cut = text.LastIndexOfAny(new[] { '/', '\\' }, length - 1, length);
                    if (cut > 0)
                        length = cut + 1;
                }
                result.Add(text.Substring(0, length));
                text = text.Substring(length);
            }
            return result;
        }

        public bool Render()
        {
            if (BaseGame.UsePostScreenShaders)
                BaseGame.UI.PostScreenMenuShader.Start();

            BaseGame.UI.RenderMenuBackground();

            // the framed panel the dialog sits in
            Rectangle panel = new Rectangle(BaseGame.Width / 14, BaseGame.YToRes(105),
                BaseGame.Width * 6 / 7, BaseGame.YToRes(400));
            // (the panel is a little smaller than the frame, so its corners do not show outside it)
            int inset = BaseGame.XToRes(8);
            BaseGame.UI.RenderPanel(new Rectangle(panel.X + inset, panel.Y + inset,
                panel.Width - 2 * inset, panel.Height - 2 * inset), new Color(255, 255, 255, 235));
            BaseGame.UI.RenderFrame(panel);

            int centerX = BaseGame.Width / 2;
            int y = BaseGame.YToRes(150);
            TextureFont.WriteTextCentered(centerX, y, title, Color.White, 1.5f);
            y += BaseGame.YToRes(65);

            foreach (string line in message)
            {
                TextureFont.WriteTextCentered(centerX, y, line, Color.White, 0.9f);
                y += BaseGame.YToRes(32);
            }

            if (detail != null)
            {
                const float detailScale = 0.6f;
                foreach (string piece in Wrap(detail, panel.Width * 9 / 10, detailScale))
                {
                    TextureFont.WriteTextCentered(centerX, y, piece, Color.LightGray, detailScale);
                    y += BaseGame.YToRes(24);
                }
            }

            if (question != null)
            {
                y += BaseGame.YToRes(20);
                TextureFont.WriteTextCentered(centerX, y, question, Color.White, 0.9f);
            }
            y += BaseGame.YToRes(80);

            // [No] [Yes]: panel buttons (outline proportions 212 x 92)
            int buttonWidth = BaseGame.XToRes(190);
            int buttonHeight = buttonWidth * 92 / 212;
            Rectangle[] boxes = new Rectangle[2];
            for (int i = 0; i < 2; i++)
            {
                int cx = centerX + (i == 0 ? -1 : 1) * BaseGame.XToRes(120);
                boxes[i] = new Rectangle(cx - buttonWidth / 2, y - buttonHeight / 2, buttonWidth, buttonHeight);
            }
            int clicked = buttons.Render(boxes, labels, 1.3f);

            if (Input.KeyboardLeftJustPressed || Input.GamePadLeftJustPressed)
            {
                Sound.Play(Sound.Sounds.Highlight);
                buttons.Select(0);
            }
            if (Input.KeyboardRightJustPressed || Input.GamePadRightJustPressed)
            {
                Sound.Play(Sound.Sounds.Highlight);
                buttons.Select(1);
            }

            bool no = Input.KeyboardEscapeJustPressed || Input.GamePadBJustPressed ||
                Input.GamePadBackJustPressed;
            bool confirm = Input.KeyboardSpaceJustPressed || Input.GamePadAJustPressed ||
                Input.KeyboardKeyJustPressed(Keys.Enter);
            if (clicked >= 0)
                confirm = true;

            if (no || (confirm && buttons.Selection == 0))
            {
                Sound.Play(Sound.Sounds.ButtonClick);
                if (onNo != null)
                    onNo();
                return true;
            }
            if (confirm)
            {
                Sound.Play(Sound.Sounds.ButtonClick);
                if (onYes != null)
                    onYes();
                return true;
            }
            return false;
        }
    }
}
