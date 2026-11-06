//-----------------------------------------------------------------------------
// CustomTrackSelection.cs
//
// Lists the *.Track files found in the user's Tracks folder and lets the
// player pick one. See Documentation/Track-Editor-Plan.md.
//-----------------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Helpers;
using RacingGame.Sounds;
using RacingGame.Tracks;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// Simple list of custom tracks.
    /// </summary>
    class CustomTrackSelection : IGameScreen
    {
        const int MaxVisibleRows = 8;

        int selected = 0;
        int firstVisible = 0;

        // when set, choosing a track hands it to this instead of starting a single player race
        readonly Action<CustomTrackInfo> pick;
        bool picked;

        public CustomTrackSelection() : this(null)
        {
        }

        public CustomTrackSelection(Action<CustomTrackInfo> pick)
        {
            this.pick = pick;
            CustomTracks.Refresh();
            // Start on the first playable track
            for (int i = 0; i < CustomTracks.Tracks.Count; i++)
                if (CustomTracks.Tracks[i].IsPlayable)
                {
                    selected = i;
                    break;
                }
        }

        public void Update(GameTime gameTime)
        {
        }

        public bool Render()
        {
            BaseGame.UI.RenderMenuBackground();
            BaseGame.UI.RenderBlackBar(120, 520);

            int centerX = BaseGame.Width / 2;
            int y = BaseGame.YToRes(150);
            TextureFont.WriteTextCentered(BaseGame.XToRes(30) + ListWidth / 2, y, CustomTracks.Title, Color.White, 1.5f);
            y += BaseGame.YToRes(50);

            var list = CustomTracks.Tracks;
            if (list.Count == 0)
            {
                TextureFont.WriteTextCentered(centerX, y + BaseGame.YToRes(40),
                    "No tracks found. Save .Track files into:", Color.Yellow, 0.8f);
                TextureFont.WriteTextCentered(centerX, y + BaseGame.YToRes(80),
                    CustomTracks.Directory, Color.LightGray, 0.6f);
            }
            else
            {
                if (Input.KeyboardDownJustPressed || Input.GamePadDownJustPressed ||
                    Input.MouseWheelDelta < 0)
                    Move(1);
                else if (Input.KeyboardUpJustPressed || Input.GamePadUpJustPressed ||
                    Input.MouseWheelDelta > 0)
                    Move(-1);

                // Paging: 8 tracks a page, the page follows the selection
                int pages = (list.Count + MaxVisibleRows - 1) / MaxVisibleRows;
                if (pages > 1)
                {
                    bool prevPage = Input.KeyboardLeftJustPressed || Input.GamePadLeftJustPressed ||
                        Input.KeyboardKeyJustPressed(Keys.PageUp);
                    bool nextPage = Input.KeyboardRightJustPressed || Input.GamePadRightJustPressed ||
                        Input.KeyboardKeyJustPressed(Keys.PageDown);
                    if (BaseGame.UI.RenderPageArrow(860, 587, false)) prevPage = true;
                    if (BaseGame.UI.RenderPageArrow(965, 587, true)) nextPage = true;
                    if (prevPage || nextPage)
                    {
                        int page = (selected / MaxVisibleRows + pages + (nextPage ? 1 : -1)) % pages;
                        selected = Math.Min(page * MaxVisibleRows, list.Count - 1);
                        Sound.Play(Sound.Sounds.ButtonClick);
                    }
                    TextureFont.WriteTextCentered(BaseGame.XToRes(912), BaseGame.YToRes(545),
                        "Page " + (selected / MaxVisibleRows + 1) + " / " + pages, Color.Gray, 0.55f);
                }
                firstVisible = selected / MaxVisibleRows * MaxVisibleRows;

                int rowH = BaseGame.YToRes(40);
                int rowW = ListWidth;
                int hovered = -1;
                for (int i = firstVisible; i < list.Count && i < firstVisible + MaxVisibleRows; i++)
                {
                    CustomTrackInfo info = list[i];
                    Rectangle row = new Rectangle(BaseGame.XToRes(30), y, rowW, rowH);
                    if (Input.MouseInBox(row))
                        hovered = i;

                    Color color = !info.IsPlayable ? Color.Red :
                        i == selected ? Color.Yellow : Color.White;
                    // Name only, the details are in the preview panel
                    string text = (i == selected ? "> " : "  ") + info.Name;
                    if (!info.IsPlayable)
                        text += "  (can't play)";
                    text = Fit(text, rowW, 1.0f);
                    TextureFont.WriteText(row.X, y, text, color);
                    y += rowH;
                }
                if (hovered >= 0 && Input.HasMouseMoved)
                    selected = hovered;

                if (selected >= 0 && selected < list.Count)
                    RenderPreview(list[selected]);

                // Start the selected track
                bool start = Input.KeyboardSpaceJustPressed || Input.GamePadAJustPressed ||
                    Input.KeyboardKeyJustPressed(Keys.Enter) ||
                    (hovered >= 0 && Input.MouseLeftButtonJustPressed);
                if (start && selected >= 0 && selected < list.Count)
                {
                    if (list[selected].IsPlayable)
                    {
                        Sound.Play(Sound.Sounds.ButtonClick);
                        if (pick != null)
                        {
                            pick(list[selected]);
                            picked = true;
                        }
                        else
                        {
                            TrackSelection.CustomTrack = list[selected];
                            RacingGameManager.AddGameScreen(new GameScreen());
                        }
                    }
                }
            }

            BaseGame.UI.RenderBottomButtons(true);
            TextureFont.WriteTextCentered(centerX, BaseGame.YToRes(535),
                "Folder: " + CustomTracks.Directory, Color.Gray, 0.5f);

            return picked || Input.KeyboardEscapeJustPressed || Input.GamePadBJustPressed ||
                Input.GamePadBackJustPressed || BaseGame.UI.backButtonPressed;
        }


        const int ListWidth = 460;   // 1024 wide layout units

        /// <summary>
        /// Left aligned text at a scale (the font has no such overload).
        /// </summary>
        static void Text(int x, int y, string text, Color color, float scale)
        {
            TextureFont.WriteTextCentered(
                x + (int)Math.Round(TextureFont.GetTextWidth(text) * scale / 2),
                y + (int)Math.Round(TextureFont.Height * scale / 2),
                text, color, scale);
        }

        /// <summary>
        /// Shorten text with ".." so it fits the width.
        /// </summary>
        static string Fit(string text, int width, float scale)
        {
            if (TextureFont.GetTextWidth(text) * scale <= width)
                return text;
            while (text.Length > 3 && TextureFont.GetTextWidth(text + "..") * scale > width)
                text = text.Substring(0, text.Length - 1);
            return text + "..";
        }

        /// <summary>
        /// Word wrap into at most maxLines lines (last one gets "..").
        /// </summary>
        static System.Collections.Generic.List<string> Wrap(string text, int width,
            float scale, int maxLines)
        {
            var lines = new System.Collections.Generic.List<string>();
            string line = "";
            foreach (string word in text.Replace('\r', ' ').Replace('\n', ' ').Split(' '))
            {
                if (word.Length == 0)
                    continue;
                string t = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && TextureFont.GetTextWidth(t) * scale > width)
                {
                    lines.Add(line);
                    line = word;
                    if (lines.Count == maxLines)
                        break;
                }
                else
                    line = t;
            }
            if (lines.Count < maxLines && line.Length > 0)
                lines.Add(line);
            else if (lines.Count == maxLines)
                lines[maxLines - 1] = Fit(lines[maxLines - 1] + "..", width, scale);
            for (int i = 0; i < lines.Count; i++)
                lines[i] = Fit(lines[i], width, scale);
            return lines;
        }

        /// <summary>
        /// Top-down map and details of the highlighted track.
        /// </summary>
        void RenderPreview(CustomTrackInfo info)
        {
            int px = BaseGame.XToRes(520), py = BaseGame.YToRes(195);
            int size = Math.Min(BaseGame.XToRes(250), BaseGame.YToRes(250));

            // a light map card (like the built-in track buttons): white-ish with a grey edge
            int edge = Math.Max(2, size / 100);
            BaseGame.UI.FillRect(new Rectangle(px - edge, py - edge, size + 2 * edge, size + 2 * edge),
                new Color(150, 150, 150));
            BaseGame.UI.FillRect(new Rectangle(px, py, size, size), new Color(240, 240, 240));

            if (!TrackPreview.Draw(info, new Rectangle(px, py, size, size), 14, Math.Max(3, size / 40)))
            {
                Text(px + 10, py + 10, Fit(info.Error ?? "", size - 20, 0.6f), Color.Red, 0.6f);
                return;
            }

            // Details to the right of the map
            int tx0 = px + size + BaseGame.XToRes(14);
            int tw = BaseGame.XToRes(1000) - tx0;
            int ty0 = py;
            int lh = BaseGame.YToRes(26);
            float fs = 0.6f;
            Text(tx0, ty0, Fit(info.Name, tw, 0.8f), Color.Yellow, 0.8f);
            ty0 += BaseGame.YToRes(34);
            Text(tx0, ty0, Fit(info.Author.Length > 0 ? "by " + info.Author : "Author unknown", tw, fs), Color.White, fs);
            ty0 += lh;
            if (!string.IsNullOrWhiteSpace(info.Data.Created))
            {
                Text(tx0, ty0, Fit("Made " + info.Data.Created.Trim(), tw, fs), Color.LightGray, fs);
                ty0 += lh;
            }
            Text(tx0, ty0, "Length " + (int)(info.Length / 100) / 10.0f + " km", Color.LightGray, fs);
            ty0 += lh;
            Text(tx0, ty0, info.Data.Checkpoints == null ? "Checkpoints: automatic"
                : "Checkpoints: " + info.Data.Checkpoints.Length, Color.LightGray, fs);
            ty0 += lh;
            Text(tx0, ty0, info.Heights == null ? "Standard terrain" : "Custom terrain", Color.LightGray, fs);
            ty0 += lh;
            if (!string.IsNullOrWhiteSpace(info.Data.OriginalAuthor))
                Text(tx0, ty0, Fit("Original: " + info.Data.OriginalAuthor.Trim(), tw, fs), Color.LightGray, fs);

            // Description below the map
            if (!string.IsNullOrWhiteSpace(info.Data.Description))
            {
                int dy = py + size + BaseGame.YToRes(10);
                foreach (string line in Wrap(info.Data.Description, BaseGame.XToRes(480), 0.6f, 3))
                {
                    Text(px, dy, line, Color.White, 0.6f);
                    dy += BaseGame.YToRes(24);
                }
            }
        }

        void Move(int delta)
        {
            int count = CustomTracks.Tracks.Count;
            if (count == 0)
                return;
            selected = (selected + delta + count) % count;
            Sound.Play(Sound.Sounds.ButtonClick);
        }
    }
}
