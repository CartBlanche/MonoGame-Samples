//-----------------------------------------------------------------------------
// tracktool: create, inspect, validate and edit RacingGame *.Track files.
// See Documentation/Track-Editor-Plan.md (D10-D12) and Track-Format-Spec.md.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using RacingGame.Tracks;

namespace RacingGame.TrackTool
{
    static class Program
    {
        const string Usage = @"tracktool - RacingGame track file tool

  tracktool info <file>
  tracktool validate <file|folder> [...]
  tracktool new <circle|oval|rounded-square|wavy> -o <file> [options]
        --name <text> --author <text> --description <text>
        --points <n=24> --radius <units=800> --z <height=80>
        --terrain <none|flat|hill|noise> --seed <n=1>
  tracktool meta <file> [--name <t>] [--author <t>] [--description <t>]
        [--created <today|yyyy-MM-dd>] [-o <out>]
  tracktool checkpoints <file> (--auto | --none | --count <n> | --set <f1,f2,...>) [-o <out>]
        --auto   the game puts one every 500 units (default)   --none   no checkpoints
        --count  n evenly spaced ones                          --set    fractions of the lap, e.g. 0.25,0.5,0.8
  tracktool heightmap dump <file> <out.png> [--default <LandscapeHeights.data>]
  tracktool heightmap set <file> <in.png> [--resample] [-o <out>]
  tracktool heightmap gen <file> --terrain <flat|hill|noise> [--seed <n>] [-o <out>]
  tracktool heightmap clear <file> [-o <out>]

Edits happen in place unless -o is given. Exit code: 0 ok, 1 invalid track, 2 usage error.
Folder for tracks in the game (macOS): ~/Library/Application Support/RacingGame/Tracks
";

        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || args[0] == "-h" || args[0] == "--help" || args[0] == "help")
                {
                    Console.Write(Usage);
                    return args.Length == 0 ? 2 : 0;
                }
                var opt = new Options(args);
                switch (args[0])
                {
                    case "info": return Commands.Info(opt);
                    case "validate": return Commands.Validate(opt);
                    case "new": return Commands.New(opt);
                    case "meta": return Commands.Meta(opt);
                    case "checkpoints": return Commands.Checkpoints(opt);
                    case "heightmap": return Commands.Heightmap(opt);
                    default:
                        Console.Error.WriteLine("Unknown command '" + args[0] + "'. Try: tracktool help");
                        return 2;
                }
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }
    }

    class UsageException : Exception
    {
        public UsageException(string m) : base(m) { }
    }

    /// <summary>
    /// Tiny argument parser: positional args plus --key value / --flag / -o value.
    /// </summary>
    class Options
    {
        public readonly List<string> Positional = new List<string>();
        readonly Dictionary<string, string> values = new Dictionary<string, string>();
        static readonly HashSet<string> Flags = new HashSet<string> { "resample", "auto", "none" };

        public Options(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
            {
                string a = args[i];
                if (a == "-o") a = "--out";
                if (a.StartsWith("--"))
                {
                    string key = a.Substring(2);
                    if (Flags.Contains(key)) { values[key] = "true"; continue; }
                    if (i + 1 >= args.Length)
                        throw new UsageException("Option " + a + " needs a value");
                    values[key] = args[++i];
                }
                else
                    Positional.Add(a);
            }
        }

        public string Get(string key) { string v; return values.TryGetValue(key, out v) ? v : null; }
        public bool Has(string key) { return values.ContainsKey(key); }
        public int GetInt(string key, int def)
        {
            string v = Get(key);
            if (v == null) return def;
            int r;
            if (!int.TryParse(v, out r)) throw new UsageException("--" + key + " must be a whole number");
            return r;
        }
        public float GetFloat(string key, float def)
        {
            string v = Get(key);
            if (v == null) return def;
            float r;
            if (!float.TryParse(v, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out r))
                throw new UsageException("--" + key + " must be a number");
            return r;
        }
        public string Arg(int index, string what)
        {
            if (index >= Positional.Count) throw new UsageException("Missing " + what);
            return Positional[index];
        }
    }

    static class TrackIO
    {
        public static TrackData Read(string path)
        {
            if (!File.Exists(path)) throw new UsageException("File not found: " + path);
            using (Stream s = File.OpenRead(path))
                return TrackData.Load(s);
        }

        public static void Write(TrackData data, string path)
        {
            if (data.FormatVersion < 1) data.FormatVersion = 1;
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(dir);
            var settings = new XmlWriterSettings { Indent = true, Encoding = new System.Text.UTF8Encoding(false) };
            using (XmlWriter w = XmlWriter.Create(path, settings))
                new XmlSerializer(typeof(TrackData)).Serialize(w, data);
        }

        public static string OutPath(Options o, string input)
        {
            return o.Get("out") ?? input;
        }
    }

    static class Commands
    {
        // ---- info / validate ------------------------------------------------

        public static int Info(Options o)
        {
            string path = o.Arg(0, "<file>");
            CustomTrackInfo info = CustomTracks.Load(path);
            TrackData d = info.Data ?? TrackIO.Read(path);
            Console.WriteLine("File:         " + path);
            Console.WriteLine("Name:         " + (string.IsNullOrWhiteSpace(d.Name) ? "(none, file name is used: " + Path.GetFileNameWithoutExtension(path) + ")" : d.Name));
            Console.WriteLine("Author:       " + (string.IsNullOrWhiteSpace(d.Author) ? "(none recorded)" : d.Author));
            if (!string.IsNullOrWhiteSpace(d.OriginalAuthor))
                Console.WriteLine("Original by:  " + d.OriginalAuthor);
            if (!string.IsNullOrWhiteSpace(d.Description)) Console.WriteLine("Description:  " + d.Description);
            if (!string.IsNullOrWhiteSpace(d.Created)) Console.WriteLine("Created:      " + d.Created);
            Console.WriteLine("Format:       " + (d.FormatVersion == 0 ? "0 (no metadata, e.g. a built-in track)" : d.FormatVersion.ToString()));
            Console.WriteLine("Control pts:  " + d.TrackPoints.Count);
            Console.WriteLine("Length:       ~" + info.Length.ToString("F0") + " units (control polygon)");
            Console.WriteLine("Helpers:      width " + d.WidthHelpers.Count + ", road " + d.RoadHelpers.Count);
            Console.WriteLine("Scenery:      " + d.NeutralsObjects.Count + " objects");
            Console.WriteLine("Checkpoints:  " + (d.Checkpoints == null ? "automatic (one every 500 units)" :
                d.Checkpoints.Length == 0 ? "none" :
                d.Checkpoints.Length + " custom, at " + string.Join(", ", Array.ConvertAll(d.Checkpoints, f => (f * 100).ToString("F1") + "%"))));
            Console.WriteLine("Heightmap:    " + (d.Heightmap == null ? "none (shipped terrain)" :
                d.Heightmap.Length + " bytes compressed" + (info.Heights != null ? ", valid 257x257" : ", INVALID")));
            if (d.TrackPoints.Count > 0)
            {
                Vector3 min = d.TrackPoints[0], max = d.TrackPoints[0];
                foreach (Vector3 p in d.TrackPoints) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
                Console.WriteLine("Extent:       X " + min.X.ToString("F0") + ".." + max.X.ToString("F0") +
                    "  Y " + min.Y.ToString("F0") + ".." + max.Y.ToString("F0") +
                    "  Z " + min.Z.ToString("F0") + ".." + max.Z.ToString("F0"));
            }
            Console.WriteLine("Status:       " + (info.IsPlayable ? "OK (playable)" : "INVALID: " + info.Error));
            foreach (string w in Warnings(d)) Console.WriteLine("Warning:      " + w);
            return info.IsPlayable ? 0 : 1;
        }

        /// <summary>
        /// Advice that does not stop a track from loading but usually means trouble.
        /// </summary>
        static List<string> Warnings(TrackData d)
        {
            var w = new List<string>();
            if (string.IsNullOrWhiteSpace(d.Author))
                w.Add("no Author recorded (the editor requires one)");
            var pts = d.TrackPoints;
            for (int i = 0; i < pts.Count; i++)
            {
                float gap = Vector3.Distance(pts[i], pts[(i + 1) % pts.Count]);
                if (gap < 30 || gap > 400)
                {
                    w.Add("points " + i + "->" + ((i + 1) % pts.Count) + " are " + gap.ToString("F0") +
                        " units apart (shipped tracks use 37-378)");
                    break;
                }
            }
            // accidental loop-the-loop (see Track-Format-Spec 3.2)
            for (int i = 1; i < pts.Count - 3; i++)
            {
                Vector3 v1 = pts[i + 1] - pts[i], v2 = pts[i + 2] - pts[i + 1];
                float xy = (float)Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y), dz = Math.Abs(v1.Z);
                if (dz / 2 > xy && Math.Abs(v1.Z + v2.Z) < dz / 2)
                {
                    w.Add("points " + i + ".." + (i + 2) + " will turn into a loop-the-loop (steep up then down)");
                    break;
                }
            }
            return w;
        }

        public static int Validate(Options o)
        {
            if (o.Positional.Count == 0) throw new UsageException("Missing <file|folder>");
            int bad = 0, total = 0;
            foreach (string arg in o.Positional)
            {
                var files = new List<string>();
                if (Directory.Exists(arg)) files.AddRange(Array.FindAll(Directory.GetFiles(arg), CustomTracks.IsTrackFile));
                else files.Add(arg);
                files.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string f in files)
                {
                    total++;
                    CustomTrackInfo info = CustomTracks.Load(f);
                    if (info.IsPlayable)
                    {
                        Console.WriteLine("OK       " + f + (info.Author.Length > 0 ? "  by " + info.Author : ""));
                        foreach (string w in Warnings(info.Data)) Console.WriteLine("         warning: " + w);
                    }
                    else { bad++; Console.WriteLine("INVALID  " + f + "  " + info.Error); }
                }
            }
            Console.WriteLine(total + " checked, " + bad + " invalid");
            return bad == 0 ? 0 : 1;
        }

        // ---- new -------------------------------------------------------------

        public static int New(Options o)
        {
            string shape = o.Arg(0, "<shape>");
            string outFile = o.Get("out");
            if (outFile == null) throw new UsageException("new needs -o <file>");
            int n = o.GetInt("points", 24);
            float radius = o.GetFloat("radius", 800);
            float zFixed = o.GetFloat("z", 80);
            string terrain = o.Get("terrain") ?? "none";
            int seed = o.GetInt("seed", 1);
            if (n < 8 || n > 400) throw new UsageException("--points must be 8..400");
            if (radius < 150 || radius > 1200) throw new UsageException("--radius must be 150..1200");

            var d = new TrackData();
            d.Name = o.Get("name") ?? Path.GetFileNameWithoutExtension(outFile);
            d.Author = o.Get("author");
            d.Description = o.Get("description");
            d.Created = DateTime.Today.ToString("yyyy-MM-dd");
            if (string.IsNullOrWhiteSpace(d.Author))
                Console.WriteLine("Note: no --author given. The game accepts it but the editor will require one.");

            byte[] heights = terrain == "none" ? null : Terrain.Generate(terrain, seed, 1280, 1280, radius);
            if (heights != null) d.Heightmap = TrackData.CompressHeightmap(heights);

            const float cx = 1280, cy = 1280;
            for (int i = 0; i < n; i++)
            {
                double a = i * 2 * Math.PI / n;
                double x, y;
                switch (shape)
                {
                    case "circle": x = Math.Cos(a); y = Math.Sin(a); break;
                    case "oval": x = Math.Cos(a); y = 0.64 * Math.Sin(a); break;
                    case "rounded-square":
                        {
                            // superellipse |x|^4 + |y|^4 = 1
                            double c = Math.Cos(a), s = Math.Sin(a);
                            double r = Math.Pow(Math.Pow(Math.Abs(c), 4) + Math.Pow(Math.Abs(s), 4), -0.25);
                            x = c * r; y = s * r; break;
                        }
                    case "wavy":
                        {
                            double r = 1 + 0.22 * Math.Sin(3 * a);
                            x = Math.Cos(a) * r; y = Math.Sin(a) * r; break;
                        }
                    default: throw new UsageException("Unknown shape '" + shape + "' (circle, oval, rounded-square, wavy)");
                }
                float px = (float)(cx + radius * x), py = (float)(cy + radius * y);
                float pz = zFixed;
                if (heights != null && !o.Has("z"))
                    pz = Terrain.Height(heights, px, py) + 15;
                d.TrackPoints.Add(new Vector3(px, py, pz));
            }

            float len;
            string err = CustomTracks.Validate(d, out len);
            if (err != null) throw new Exception("Generated track is invalid: " + err + " (try a smaller --radius)");
            TrackIO.Write(d, outFile);
            Console.WriteLine("Wrote " + outFile + "  (" + shape + ", " + n + " points, ~" + len.ToString("F0") +
                " units" + (heights != null ? ", own terrain '" + terrain + "'" : "") + ")");
            foreach (string w in Warnings(d)) Console.WriteLine("Warning: " + w);
            return 0;
        }

        // ---- meta ------------------------------------------------------------

        public static int Meta(Options o)
        {
            string file = o.Arg(0, "<file>");
            TrackData d = TrackIO.Read(file);
            bool any = false;
            if (o.Has("name")) { d.Name = o.Get("name"); any = true; }
            if (o.Has("description")) { d.Description = o.Get("description"); any = true; }
            if (o.Has("created"))
            {
                string c = o.Get("created");
                d.Created = c == "today" ? DateTime.Today.ToString("yyyy-MM-dd") : c;
                any = true;
            }
            if (o.Has("author"))
            {
                string newAuthor = o.Get("author");
                // D10: editing someone else's track keeps the original creator
                if (!string.IsNullOrWhiteSpace(d.Author) && d.Author != newAuthor &&
                    string.IsNullOrWhiteSpace(d.OriginalAuthor))
                {
                    d.OriginalAuthor = d.Author;
                    Console.WriteLine("Author changed: keeping '" + d.OriginalAuthor + "' as OriginalAuthor.");
                }
                d.Author = newAuthor;
                any = true;
            }
            if (!any) throw new UsageException("meta needs at least one of --name --author --description --created");
            string outFile = TrackIO.OutPath(o, file);
            TrackIO.Write(d, outFile);
            Console.WriteLine("Wrote " + outFile);
            return 0;
        }

        // ---- checkpoints -----------------------------------------------------

        public static int Checkpoints(Options o)
        {
            string file = o.Arg(0, "<file>");
            TrackData d = TrackIO.Read(file);
            if (o.Has("auto")) d.Checkpoints = null;
            else if (o.Has("none")) d.Checkpoints = new float[0];
            else if (o.Has("count"))
            {
                int n = o.GetInt("count", 0);
                if (n < 1 || n > 40) throw new UsageException("--count must be 1..40");
                d.Checkpoints = new float[n];
                for (int i = 0; i < n; i++) d.Checkpoints[i] = (float)Math.Round((i + 1) / (double)(n + 1), 4);
            }
            else if (o.Has("set"))
            {
                var list = new List<float>();
                foreach (string part in o.Get("set").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    float f;
                    if (!float.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))
                        throw new UsageException("'" + part + "' is not a number");
                    list.Add(f);
                }
                d.Checkpoints = list.ToArray();
            }
            else throw new UsageException("checkpoints needs one of --auto --none --count --set");
            float len;
            string err = CustomTracks.Validate(d, out len);
            if (err != null) throw new Exception("Not saved: " + err);
            string outFile = TrackIO.OutPath(o, file);
            TrackIO.Write(d, outFile);
            Console.WriteLine("Wrote " + outFile + " (checkpoints: " + (d.Checkpoints == null ? "automatic" : d.Checkpoints.Length.ToString()) + ")");
            return 0;
        }

        // ---- heightmap -------------------------------------------------------

        public static int Heightmap(Options o)
        {
            string sub = o.Arg(0, "heightmap subcommand (dump|set|gen|clear)");
            string file = o.Arg(1, "<file>");
            switch (sub)
            {
                case "dump":
                    {
                        string png = o.Arg(2, "<out.png>");
                        TrackData d = TrackIO.Read(file);
                        byte[] raw = d.DecompressHeightmap();
                        if (raw == null)
                        {
                            string def = o.Get("default");
                            if (def == null)
                                throw new UsageException("Track has no own heightmap. Pass --default <LandscapeHeights.data> to dump the shipped terrain.");
                            raw = File.ReadAllBytes(def);
                            if (raw.Length != TrackData.HeightmapBytes) throw new Exception("--default file is not 66049 bytes");
                        }
                        Png.WriteGrey(png, raw, 257, 257);
                        Console.WriteLine("Wrote " + png + " (257x257 greyscale: black = 0, white = 300 units high)");
                        return 0;
                    }
                case "set":
                    {
                        string png = o.Arg(2, "<in.png>");
                        TrackData d = TrackIO.Read(file);
                        byte[] raw = Png.ReadGrey(png, 257, 257, o.Has("resample"));
                        d.Heightmap = TrackData.CompressHeightmap(raw);
                        string outFile = TrackIO.OutPath(o, file);
                        TrackIO.Write(d, outFile);
                        Console.WriteLine("Wrote " + outFile + " (heightmap " + d.Heightmap.Length + " bytes compressed)");
                        return 0;
                    }
                case "gen":
                    {
                        string kind = o.Get("terrain");
                        if (kind == null) throw new UsageException("heightmap gen needs --terrain <flat|hill|noise>");
                        TrackData d = TrackIO.Read(file);
                        float cx = 0, cy = 0, rad = 0;
                        if (d.TrackPoints.Count > 0)
                        {
                            Vector3 min = d.TrackPoints[0], max = d.TrackPoints[0];
                            foreach (Vector3 p in d.TrackPoints) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
                            cx = (min.X + max.X) / 2; cy = (min.Y + max.Y) / 2;
                            rad = Math.Max(max.X - min.X, max.Y - min.Y) / 2;
                        }
                        byte[] raw = Terrain.Generate(kind, o.GetInt("seed", 1), cx, cy, rad);
                        d.Heightmap = TrackData.CompressHeightmap(raw);
                        string outFile = TrackIO.OutPath(o, file);
                        TrackIO.Write(d, outFile);
                        Console.WriteLine("Wrote " + outFile + " (terrain '" + kind + "'). Note: road heights are not changed; the game lifts the road above the ground.");
                        return 0;
                    }
                case "clear":
                    {
                        TrackData d = TrackIO.Read(file);
                        d.Heightmap = null;
                        string outFile = TrackIO.OutPath(o, file);
                        TrackIO.Write(d, outFile);
                        Console.WriteLine("Wrote " + outFile + " (back to the shipped terrain)");
                        return 0;
                    }
                default:
                    throw new UsageException("Unknown heightmap subcommand '" + sub + "'");
            }
        }
    }

    /// <summary>
    /// Simple terrain generators. Height byte 0..255 = 0..300 units.
    /// </summary>
    static class Terrain
    {
        public static float Height(byte[] h, float x, float y)
        {
            int ix = Math.Max(0, Math.Min(256, (int)Math.Round(x / 10)));
            int iy = Math.Max(0, Math.Min(256, (int)Math.Round(y / 10)));
            return h[ix + iy * 257] / 255f * 300f;
        }

        public static byte[] Generate(string kind, int seed, float cx, float cy, float radius)
        {
            byte[] h = new byte[257 * 257];
            switch (kind)
            {
                case "flat":
                    for (int i = 0; i < h.Length; i++) h[i] = 10;
                    break;
                case "hill":
                    {
                        double sigma = Math.Max(150, radius * 0.45);
                        for (int y = 0; y < 257; y++)
                            for (int x = 0; x < 257; x++)
                            {
                                double dx = x * 10 - cx, dy = y * 10 - cy;
                                h[x + y * 257] = (byte)Math.Round(10 + 190 * Math.Exp(-(dx * dx + dy * dy) / (2 * sigma * sigma)));
                            }
                        break;
                    }
                case "noise":
                    {
                        // 4 octaves of seeded value noise, gentle rolling terrain (byte 5..~120)
                        for (int y = 0; y < 257; y++)
                            for (int x = 0; x < 257; x++)
                            {
                                double v = 0, amp = 1, total = 0, freq = 1.0 / 64;
                                for (int o = 0; o < 4; o++)
                                {
                                    v += amp * ValueNoise(x * freq, y * freq, seed + o * 101);
                                    total += amp; amp *= 0.5; freq *= 2;
                                }
                                v /= total;
                                h[x + y * 257] = (byte)Math.Round(5 + 115 * v);
                            }
                        break;
                    }
                default:
                    throw new UsageException("Unknown terrain '" + kind + "' (none, flat, hill, noise)");
            }
            return h;
        }

        static double Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint n = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                n = (n ^ (n >> 13)) * 1274126177u;
                n ^= n >> 16;
                return (n & 0xFFFFFF) / (double)0x1000000;
            }
        }

        static double ValueNoise(double x, double y, int seed)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            double fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            double a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed);
            double c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
            return (a + (b - a) * fx) + ((c + (d - c) * fx) - (a + (b - a) * fx)) * fy;
        }
    }

    /// <summary>
    /// PNG import/export of heightmaps (greyscale 8 bit).
    /// </summary>
    static class Png
    {
        public static void WriteGrey(string path, byte[] data, int w, int h)
        {
            using (Stream s = File.Create(path))
                new StbImageWriteSharp.ImageWriter().WritePng(data, w, h,
                    StbImageWriteSharp.ColorComponents.Grey, s);
        }

        public static byte[] ReadGrey(string path, int w, int h, bool resample)
        {
            if (!File.Exists(path)) throw new UsageException("File not found: " + path);
            StbImageSharp.ImageResult img;
            using (Stream s = File.OpenRead(path))
                img = StbImageSharp.ImageResult.FromStream(s, StbImageSharp.ColorComponents.Grey);
            if (img.Width == w && img.Height == h)
                return img.Data;
            if (!resample)
                throw new UsageException("PNG is " + img.Width + "x" + img.Height + ", the heightmap needs " + w + "x" + h +
                    ". Use --resample to scale it.");
            byte[] result = new byte[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float sx = x * (img.Width - 1) / (float)(w - 1), sy = y * (img.Height - 1) / (float)(h - 1);
                    int x0 = (int)sx, y0 = (int)sy, x1 = Math.Min(x0 + 1, img.Width - 1), y1 = Math.Min(y0 + 1, img.Height - 1);
                    float fx = sx - x0, fy = sy - y0;
                    float a = img.Data[x0 + y0 * img.Width], b = img.Data[x1 + y0 * img.Width];
                    float c = img.Data[x0 + y1 * img.Width], d = img.Data[x1 + y1 * img.Width];
                    float top = a + (b - a) * fx, bot = c + (d - c) * fx;
                    result[x + y * w] = (byte)Math.Round(top + (bot - top) * fy);
                }
            return result;
        }
    }
}
