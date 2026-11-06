using System;
using Microsoft.Xna.Framework;
using RacingGame.Graphics;
using RacingGame.Tracks;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// A top-down picture of a custom track (a thick black line with a chequered start marker, as
    /// on the built-in track buttons), shared by the custom track list and the multiplayer lobby.
    /// Draw a light background first (UIRenderer.RenderLightPanel or FillRect).
    /// </summary>
    static class TrackPreview
    {
        // the outline of the last track drawn (rebuilt when another track is shown)
        static CustomTrackInfo outlineFor;
        static Vector2[] outline;           // closed Catmull-Rom polyline in world X/Y

        static void BuildOutline(CustomTrackInfo info)
        {
            outlineFor = info;
            outline = null;
            if (!info.IsPlayable)
                return;
            var pts = info.Data.TrackPoints;
            int n = pts.Count;
            int steps = Math.Max(3, Math.Min(12, 220 / n));
            outline = new Vector2[n * steps + 1];
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = new Vector2(pts[(i + n - 1) % n].X, pts[(i + n - 1) % n].Y);
                Vector2 p2 = new Vector2(pts[i].X, pts[i].Y);
                Vector2 p3 = new Vector2(pts[(i + 1) % n].X, pts[(i + 1) % n].Y);
                Vector2 p4 = new Vector2(pts[(i + 2) % n].X, pts[(i + 2) % n].Y);
                for (int s = 0; s < steps; s++)
                    outline[k++] = Vector2.CatmullRom(p1, p2, p3, p4, s / (float)steps);
            }
            outline[k] = outline[0];   // close the loop
        }

        /// <summary>
        /// Draw the track inside the area, keeping its shape (north is up), with a margin.
        /// Thickness is the width of the road line in pixels. Returns false if the track can't be
        /// drawn (it is not playable).
        /// </summary>
        public static bool Draw(CustomTrackInfo info, Rectangle area, int margin, int thickness)
        {
            if (info == null)
                return false;
            if (outlineFor != info)
                BuildOutline(info);
            if (outline == null)
                return false;

            Vector2 min = outline[0], max = outline[0];
            foreach (Vector2 v in outline)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            float width = area.Width - 2 * margin, height = area.Height - 2 * margin;
            float scale = Math.Min(width, height) / Math.Max(Math.Max(max.X - min.X, max.Y - min.Y), 1);
            Vector2 centre = (min + max) / 2;
            int cx = area.X + area.Width / 2, cy = area.Y + area.Height / 2;
            Func<Vector2, Point> map = v => new Point(
                cx + (int)Math.Round((v.X - centre.X) * scale),
                cy - (int)Math.Round((v.Y - centre.Y) * scale));

            // road: a thick black line
            Color road = new Color(25, 25, 25);
            for (int i = 1; i < outline.Length; i++)
                BaseGame.UI.RenderThickLine(map(outline[i - 1]), map(outline[i]), thickness, road);

            // start / finish: a chequered marker (as on the built-in track pictures)
            Point m = map(outline[0]);
            int cell = Math.Max(3, thickness);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    BaseGame.UI.FillRect(new Rectangle(m.X - cell * 3 / 2 + x * cell, m.Y - cell * 3 / 2 + y * cell, cell, cell),
                        (x + y) % 2 == 0 ? Color.White : Color.Black);
            return true;
        }
    }
}
