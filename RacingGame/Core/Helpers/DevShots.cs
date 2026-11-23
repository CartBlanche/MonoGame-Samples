#if DEVSHOTS
// Dev-only harness for the before/after trailer (itch/video/): compiled in only with
//   dotnet build Desktop -c Release -p:DevShots=true
// so a release build has none of this. It is driven by environment variables:
//   DEVSHOT_NAME      name used in the cue lines (Old, New)
//   DEVSHOT_CAR       demo car number 0..2 (default 1) and DEVSHOT_COLOUR "r,g,b" (default 255,140,0): the same car in both copies
//   DEVSHOT_WIN       "x,y,w,h": window position and size in points (the picture is drawn at that size)
//   DEVSHOT_GOFILE    the rolling demo is held at its start until this file exists (so two copies start together)
//   DEVSHOT_OFFSET    seconds into the replay the demo starts from (default 0)
//   DEVSHOT_CLOCK     the demo's time of day: "seconds:hour,seconds:hour,..." linear between the points
//                     (seconds after the go file, hour 0..24), for example "0:11,14:11,20:23"
//   DEVSHOT_TEXTS     overlay texts: "from-to|text;from-to|text" in seconds after the go file, in the game's own font
//   DEVSHOT_CUES      "name:seconds,name:seconds": extra cue lines printed at those times after the go file
//   DEVSHOT_QUITAT    seconds after the go file when this copy quits
// The same file is used for the Old window, built from the original game on the main branch (record-video.sh copies it
// there and applies old-main.patch, which also defines DEVSHOTS_OLD: no lighting code exists there).
// Cue lines: "[DEVSHOT] cue <name> unix-ms <n> demo-s <t>", read by the director (itch/video/director.py).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using RacingGame.Graphics;
using RacingGame.Properties;

namespace RacingGame.Helpers
{
    public static class DevShots
    {
        static string Env(string n) { return Environment.GetEnvironmentVariable(n); }

        static readonly string name = Env("DEVSHOT_NAME") ?? "Game";
        static readonly string goFile = Env("DEVSHOT_GOFILE");
        static readonly float offset = F(Env("DEVSHOT_OFFSET"), 0.0f);
        static readonly float quitAt = F(Env("DEVSHOT_QUITAT"), -1.0f);
        static readonly List<KeyValuePair<float, float>> clock = ParseClock(Env("DEVSHOT_CLOCK"));
        static readonly List<Overlay> texts = ParseTexts(Env("DEVSHOT_TEXTS"));
        static readonly List<KeyValuePair<float, string>> cues = ParseCues(Env("DEVSHOT_CUES"));

        /// <summary>True when the video harness drives this copy (DEVSHOT_NAME is set).</summary>
        public static readonly bool Active = Env("DEVSHOT_NAME") != null;

        static readonly Stopwatch sinceGo = new Stopwatch();
        static int firstCue;
        static int frames;
        static bool readyPrinted, tunnelPrinted;

        sealed class Overlay { public float From, To; public string Text; }

        static float F(string s, float d)
        {
            float v;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : d;
        }

        static List<KeyValuePair<float, float>> ParseClock(string s)
        {
            var l = new List<KeyValuePair<float, float>>();
            if (string.IsNullOrEmpty(s)) return l;
            foreach (string p in s.Split(','))
            {
                string[] a = p.Split(':');
                if (a.Length == 2) l.Add(new KeyValuePair<float, float>(F(a[0], 0), F(a[1], 12)));
            }
            return l;
        }

        static List<Overlay> ParseTexts(string s)
        {
            var l = new List<Overlay>();
            if (string.IsNullOrEmpty(s)) return l;
            foreach (string p in s.Split(';'))
            {
                int bar = p.IndexOf('|');
                if (bar < 0) continue;
                string[] a = p.Substring(0, bar).Split('-');
                if (a.Length == 2)
                    l.Add(new Overlay { From = F(a[0], 0), To = F(a[1], 0), Text = p.Substring(bar + 1) });
            }
            return l;
        }

        static List<KeyValuePair<float, string>> ParseCues(string s)
        {
            var l = new List<KeyValuePair<float, string>>();
            if (string.IsNullOrEmpty(s)) return l;
            foreach (string p in s.Split(','))
            {
                string[] a = p.Split(':');
                if (a.Length == 2) l.Add(new KeyValuePair<float, string>(F(a[1], 0), a[0]));
            }
            l.Sort((x, y) => x.Key.CompareTo(y.Key));
            return l;
        }

        public static void Cue(string cue)
        {
            Console.WriteLine("[DEVSHOT] cue " + cue + " unix-ms " +
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + " demo-s " +
                DemoSeconds.ToString("0.000", CultureInfo.InvariantCulture));
            Console.Out.Flush();
        }

        /// <summary>Call once from the game's constructor: settings for this copy (never saved).</summary>
        static Game game;

        public static void Init(Game owner)
        {
            if (!Active) return;
            game = owner;
            GameSettings s = GameSettings.Default;
            string[] w = (Env("DEVSHOT_WIN") ?? "").Split(',');
            s.Fullscreen = false;
            int ww, hh;
            if (w.Length == 4 && int.TryParse(w[2], out ww) && int.TryParse(w[3], out hh))
            {
                s.ResolutionWidth = ww;
                s.ResolutionHeight = hh;
            }
            Console.WriteLine("[DEVSHOT] " + name + " " + s.ResolutionWidth + "x" + s.ResolutionHeight);
        }

        /// <summary>Seconds into the demo as the video sees it: held at the offset until the go file is there.</summary>
        public static float DemoSeconds
        {
            get
            {
                // Before the go file the demo runs forward in small steps up to the offset (a jump
                // would lose the car's track progress, which the tunnel lights depend on)
                return Math.Min(offset, warmFrames * WarmStep) + (float)sinceGo.Elapsed.TotalSeconds;
            }
        }

        const float WarmStep = 0.05f;      // demo seconds per frame while fast-forwarding to the offset
        static int warmFrames;

        /// <summary>Seconds since the go file (0 before it).</summary>
        public static float SinceGo
        {
            get { return (float)sinceGo.Elapsed.TotalSeconds; }
        }

        /// <summary>The demo's time of day (hours), from DEVSHOT_CLOCK, or -1 when it is not set.</summary>
        /// <summary>The demo car (the same in both copies; the demo normally picks a random one).</summary>
        public static int CarNumber
        {
            get
            {
                int n;
                return int.TryParse(Env("DEVSHOT_CAR"), out n) ? n : 1;
            }
        }

        public static Color CarColor
        {
            get
            {
                string[] c = (Env("DEVSHOT_COLOUR") ?? "255,140,0").Split(',');
                int r, g, b;
                if (c.Length == 3 && int.TryParse(c[0], out r) && int.TryParse(c[1], out g) && int.TryParse(c[2], out b))
                    return new Color(r, g, b);
                return new Color(255, 140, 0);
            }
        }

        public static float ClockHour()
        {
            if (clock.Count == 0) return -1.0f;
            float t = SinceGo;
            if (t <= clock[0].Key) return clock[0].Value;
            for (int i = 1; i < clock.Count; i++)
                if (t <= clock[i].Key)
                {
                    float u = (t - clock[i - 1].Key) / Math.Max(0.001f, clock[i].Key - clock[i - 1].Key);
                    return MathHelper.Lerp(clock[i - 1].Value, clock[i].Value, u);
                }
            return clock[clock.Count - 1].Value;
        }

        /// <summary>Called every frame from the splash screen (the rolling demo).</summary>
        public static void Splash(float splashAge)
        {
            frames++;
#if !DEVSHOTS_OLD
            // the new look, whatever this Mac's saved setting says (the settings are loaded after the constructor; never saved)
            if (frames == 1)
                GameSettings.Default.Lighting = LightingQuality.High;
#endif
            if (!sinceGo.IsRunning && warmFrames * WarmStep < offset)
                warmFrames++;
            if (frames == 5 || frames == 40)
            {
                string[] w = (Env("DEVSHOT_WIN") ?? "").Split(',');
                int x, y;
                if (w.Length == 4 && int.TryParse(w[0], out x) && int.TryParse(w[1], out y))
                    game.Window.Position = new Point(x, y);
            }
            if (!readyPrinted && splashAge > 1.5f && warmFrames * WarmStep >= offset)
            {
                readyPrinted = true;
                Cue("ready");
            }
            if (!sinceGo.IsRunning && readyPrinted && goFile != null && File.Exists(goFile))
            {
                sinceGo.Start();
                Cue("go");
            }
            if (!sinceGo.IsRunning)
                return;
            while (firstCue < cues.Count && SinceGo >= cues[firstCue].Key)
                Cue(cues[firstCue++].Value);
#if !DEVSHOTS_OLD
            if (!tunnelPrinted && BaseGame.Lighting.TunnelBlend > 0.05f)
            {
                tunnelPrinted = true;
                Cue("tunnel-enter");
            }
#endif
            if (quitAt > 0 && SinceGo >= quitAt)
            {
                Cue("quit");
                Environment.Exit(0);
            }
        }

        /// <summary>Draws the overlay texts in the game's own font (after the scene).</summary>
        public static void RenderOverlay()
        {
            if (!sinceGo.IsRunning) return;
            float t = SinceGo;
            // The game's own black bar behind the text (as behind "Press START"): kept through the short
            // gaps between two texts so it does not flicker
            bool bar = false;
            foreach (Overlay o in texts)
                if (t >= o.From && t <= o.To + 0.35f) bar = true;
            if (bar)
                BaseGame.UI.RenderBlackBar(548, 60);
            foreach (Overlay o in texts)
            {
                if (t < o.From || t > o.To) continue;
                float a = Math.Min(Math.Min((t - o.From) / 0.35f, (o.To - t) / 0.35f), 1.0f);
                if (a <= 0) continue;
                // text shrunk to fit the window
                float scale = Math.Min(1.35f, 0.92f * BaseGame.Width / TextureFont.GetTextWidth(o.Text));
                TextureFont.WriteTextCentered(BaseGame.Width / 2, BaseGame.YToRes(578), o.Text, Color.White * a, scale);
            }
        }
    }
}
#endif
