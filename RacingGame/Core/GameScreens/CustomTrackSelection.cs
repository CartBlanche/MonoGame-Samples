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

        public CustomTrackSelection()
        {
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
            TextureFont.WriteTextCentered(BaseGame.XToRes(30) + ListWidth / 2, y, "Custom tracks", Color.White, 1.5f);
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
                        TrackSelection.CustomTrack = list[selected];
                        RacingGameManager.AddGameScreen(new GameScreen());
                    }
                }
            }

            BaseGame.UI.RenderBottomButtons(true);
            TextureFont.WriteTextCentered(centerX, BaseGame.YToRes(535),
                "Folder: " + CustomTracks.Directory, Color.Gray, 0.5f);

            return Input.KeyboardEscapeJustPressed || Input.GamePadBJustPressed ||
                Input.GamePadBackJustPressed || BaseGame.UI.backButtonPressed;
        }


        const int ListWidth = 460;   // 1024 wide layout units

        // Cached outline of the previewed track (rebuilt when the selection changes)
        CustomTrackInfo previewFor;
        Vector2[] outline;           // closed Catmull-Rom polyline in world X/Y
        float[] outlineLength;       // cumulative length at each outline point

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

        void BuildOutline(CustomTrackInfo info)
        {
            previewFor = info;
            outline = null;
            if (!info.IsPlayable)
                return;
            var pts = info.Data.TrackPoints;
            int n = pts.Count;
            int Steps = Math.Max(3, Math.Min(12, 220 / n));   // keep within the 2D line budget
            outline = new Vector2[n * Steps + 1];
            outlineLength = new float[outline.Length];
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = new Vector2(pts[(i + n - 1) % n].X, pts[(i + n - 1) % n].Y);
                Vector2 p2 = new Vector2(pts[i].X, pts[i].Y);
                Vector2 p3 = new Vector2(pts[(i + 1) % n].X, pts[(i + 1) % n].Y);
                Vector2 p4 = new Vector2(pts[(i + 2) % n].X, pts[(i + 2) % n].Y);
                for (int s = 0; s < Steps; s++)
                    outline[k++] = Vector2.CatmullRom(p1, p2, p3, p4, s / (float)Steps);
            }
            outline[k] = outline[0];   // close the loop
            for (int i = 1; i < outline.Length; i++)
                outlineLength[i] = outlineLength[i - 1] + Vector2.Distance(outline[i - 1], outline[i]);
        }

        /// <summary>
        /// Position and direction at a fraction (0..1) of the lap.
        /// </summary>
        void PointAt(float fraction, out Vector2 pos, out Vector2 dir)
        {
            float target = fraction * outlineLength[outlineLength.Length - 1];
            int i = 1;
            while (i < outlineLength.Length - 1 && outlineLength[i] < target)
                i++;
            float seg = Math.Max(outlineLength[i] - outlineLength[i - 1], 0.0001f);
            float t = MathHelper.Clamp((target - outlineLength[i - 1]) / seg, 0, 1);
            pos = Vector2.Lerp(outline[i - 1], outline[i], t);
            dir = outline[i] - outline[i - 1];
            if (dir.LengthSquared() > 0)
                dir.Normalize();
        }

        /// <summary>
        /// Top-down map and details of the highlighted track.
        /// </summary>
        void RenderPreview(CustomTrackInfo info)
        {
            if (previewFor != info)
                BuildOutline(info);

            int px = BaseGame.XToRes(520), py = BaseGame.YToRes(195);
            int size = Math.Min(BaseGame.XToRes(250), BaseGame.YToRes(250));
            Color frame = new Color(192, 192, 192, 128);
            Point a = new Point(px, py), b = new Point(px + size, py),
                c = new Point(px + size, py + size), d = new Point(px, py + size);
            BaseGame.DrawLine(a, b, frame);
            BaseGame.DrawLine(b, c, frame);
            BaseGame.DrawLine(c, d, frame);
            BaseGame.DrawLine(d, a, frame);

            if (outline == null)
            {
                Text(px + 10, py + 10, Fit(info.Error ?? "", size - 20, 0.6f), Color.Red, 0.6f);
                return;
            }

            // Fit the track into the box, keeping its shape (north is up)
            Vector2 min = outline[0], max = outline[0];
            foreach (Vector2 v in outline)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            float margin = 14;
            float scale = (size - 2 * margin) / Math.Max(Math.Max(max.X - min.X, max.Y - min.Y), 1);
            Vector2 centre = (min + max) / 2;
            Func<Vector2, Point> map = v => new Point(
                px + size / 2 + (int)Math.Round((v.X - centre.X) * scale),
                py + size / 2 - (int)Math.Round((v.Y - centre.Y) * scale));

            // Road, drawn 2 pixels thick like the highscore separator
            Color road = Color.White;
            for (int i = 1; i < outline.Length; i++)
            {
                Point p0 = map(outline[i - 1]), p1 = map(outline[i]);
                BaseGame.DrawLine(p0, p1, road);
                BaseGame.DrawLine(new Point(p0.X, p0.Y + 1), new Point(p1.X, p1.Y + 1), road);
            }

            // Custom checkpoints: short ticks across the road
            if (info.Data.Checkpoints != null)
                foreach (float f in info.Data.Checkpoints)
                {
                    Vector2 pos, dir;
                    PointAt(f, out pos, out dir);
                    Point m = map(pos);
                    int tx = (int)Math.Round(-dir.Y * 5), ty = (int)Math.Round(-dir.X * 5);
                    BaseGame.DrawLine(new Point(m.X - tx, m.Y - ty), new Point(m.X + tx, m.Y + ty), Color.Cyan);
                }

            // Start / finish with an arrow in the driving direction
            {
                Vector2 pos, dir;
                PointAt(0, out pos, out dir);
                Point m = map(pos);
                Vector2 sd = new Vector2(dir.X, -dir.Y);   // to screen space
                Vector2 side = new Vector2(-sd.Y, sd.X);
                Point tip = new Point(m.X + (int)(sd.X * 14), m.Y + (int)(sd.Y * 14));
                Point l = new Point(m.X + (int)(side.X * 6), m.Y + (int)(side.Y * 6));
                Point r = new Point(m.X - (int)(side.X * 6), m.Y - (int)(side.Y * 6));
                BaseGame.DrawLine(l, r, Color.Yellow);
                BaseGame.DrawLine(l, tip, Color.Yellow);
                BaseGame.DrawLine(r, tip, Color.Yellow);
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
