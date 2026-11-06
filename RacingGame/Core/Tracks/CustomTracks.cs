//-----------------------------------------------------------------------------
// CustomTracks.cs
//
// Finds, loads and validates user made *.Track files from the Tracks folder.
// See Documentation/Track-Format-Spec.md and Documentation/Track-Editor-Plan.md.
//-----------------------------------------------------------------------------
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using RacingGame.Helpers;

namespace RacingGame.Tracks
{
    /// <summary>
    /// One track found in the custom Tracks folder.
    /// </summary>
    public class CustomTrackInfo
    {
        /// <summary>
        /// Display name (file name without extension).
        /// </summary>
        public string Name;

        /// <summary>
        /// Who made the track (from the file, empty if not recorded).
        /// </summary>
        public string Author
        {
            get { return Data != null ? CustomTracks.CleanText(Data.Author, 40) : ""; }
        }

        /// <summary>
        /// Full path of the .Track file (null for a track received from another player).
        /// </summary>
        public string Path;

        /// <summary>
        /// Identifies the exact file: the SHA-256 of its bytes, in lower case hex. Used to tell
        /// whether two players have the same track.
        /// </summary>
        public string Id;

        /// <summary>
        /// Parsed data, null if the file could not be loaded.
        /// </summary>
        public TrackData Data;

        /// <summary>
        /// Why the file can't be played (null if it is fine).
        /// </summary>
        public string Error;

        /// <summary>
        /// The track's own terrain (raw 257 x 257 bytes), null for the shipped one.
        /// </summary>
        public byte[] Heights;

        /// <summary>
        /// Rough length of the control polygon in game units (0 if invalid).
        /// </summary>
        public float Length;

        /// <summary>
        /// Can this track be selected and played?
        /// </summary>
        public bool IsPlayable
        {
            get { return Error == null && Data != null; }
        }
    }

    /// <summary>
    /// Custom (user made) tracks. They live in a folder the user can write to,
    /// next to the game settings: LocalApplicationData/RacingGame/Tracks.
    /// </summary>
    public static class CustomTracks
    {
        /// <summary>
        /// Terrain is 256 cells of 10 units. Tracks must stay inside.
        /// </summary>
        public const float MapSize = 2560.0f;

        /// <summary>
        /// Smallest useful number of control points (TrackLine needs 3).
        /// </summary>
        const int MinControlPoints = 4;

        static readonly List<CustomTrackInfo> tracks = new List<CustomTrackInfo>();

        /// <summary>
        /// Folder that is scanned for *.Track files. Created on demand.
        /// </summary>
        public static string Directory
        {
            get
            {
                // RACINGGAME_TRACKS_DIR: another folder, e.g. to test receiving tracks on a
                // second instance on the same machine
                string other = Environment.GetEnvironmentVariable("RACINGGAME_TRACKS_DIR");
                if (!string.IsNullOrWhiteSpace(other))
                    return other;
                return System.IO.Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "RacingGame", "Tracks");
            }
        }

        /// <summary>
        /// Heading of the custom tracks screens and dialogs
        /// </summary>
        public const string Title = "Custom Tracks";

        /// <summary>
        /// Label of the Custom button on the track selection screen
        /// </summary>
        public const string ButtonLabel = "Custom";

        /// <summary>
        /// Where the Tarmac Toolbox track editor lives (the Racing Game page on itch.io).
        /// </summary>
        public const string EditorUrl = "https://softsavage.itch.io/racing-game";

        /// <summary>
        /// Create the Tracks folder if it does not exist yet (done at startup, so players find it
        /// ready to drop exported tracks into). Never throws.
        /// </summary>
        public static void EnsureDirectory()
        {
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
            }
            catch (Exception ex)
            {
                Log.Write("Could not create " + Directory + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Tracks found by the last call to Refresh.
        /// </summary>
        public static IList<CustomTrackInfo> Tracks
        {
            get { return tracks; }
        }

        /// <summary>
        /// Scan the folder again (creates it if it does not exist yet).
        /// Never throws: problems end up in CustomTrackInfo.Error or the log.
        /// </summary>
        public static void Refresh()
        {
            tracks.Clear();
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                List<string> files = new List<string>();
                foreach (string file in System.IO.Directory.GetFiles(Directory))
                    if (IsTrackFile(file))
                        files.Add(file);
                files.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                    tracks.Add(Load(file));
            }
            catch (Exception ex)
            {
                Log.Write("CustomTracks: can't scan " + Directory + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Is this a track file? Accepts Name.Track and Name.Track.txt (what a web
        /// page or an editor that can only save text files produces), in any case.
        /// Hidden files and macOS "._" companions are ignored.
        /// </summary>
        public static bool IsTrackFile(string path)
        {
            string name = System.IO.Path.GetFileName(path);
            if (name.StartsWith("."))
                return false;
            string ext = "." + TrackData.Extension;
            return name.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(ext + ".txt", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Name shown when the file has no Name of its own: the file name without
        /// ".txt" and ".Track".
        /// </summary>
        public static string FileDisplayName(string path)
        {
            string name = System.IO.Path.GetFileName(path);
            if (name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);
            if (name.EndsWith("." + TrackData.Extension, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - TrackData.Extension.Length - 1);
            return name;
        }

        /// <summary>
        /// The biggest track file that is shared in a race (the host does not offer a bigger one
        /// and a client does not accept one). Real tracks are 5 to 30 KB.
        /// </summary>
        public const int MaxSharedFileBytes = 256 * 1024;

        /// <summary>
        /// Load and validate one file.
        /// </summary>
        public static CustomTrackInfo Load(string path)
        {
            CustomTrackInfo info = new CustomTrackInfo();
            info.Path = path;
            info.Name = FileDisplayName(path);
            try
            {
                Fill(info, File.ReadAllBytes(path), int.MaxValue);
            }
            catch (Exception ex)
            {
                Fail(info, "Can't read file: " + ex.Message);
                Log.Write("CustomTracks: " + path + ": " + ex);
            }
            return info;
        }

        /// <summary>
        /// Load and validate a track that came from another player (untrusted): a size cap, the
        /// same validation as a file. Never throws; check IsPlayable.
        /// </summary>
        public static CustomTrackInfo FromBytes(byte[] bytes, string fallbackName)
        {
            CustomTrackInfo info = new CustomTrackInfo();
            info.Name = string.IsNullOrWhiteSpace(fallbackName) ? "Shared track" : fallbackName;
            try
            {
                Fill(info, bytes, MaxSharedFileBytes);
            }
            catch (Exception ex)
            {
                Fail(info, "Can't read the track: " + ex.Message);
                Log.Write("CustomTracks: received track: " + ex);
            }
            return info;
        }

        static void Fill(CustomTrackInfo info, byte[] bytes, int maxBytes)
        {
            if (bytes.Length > maxBytes)
                throw new InvalidDataException("the file is bigger than " + (maxBytes / 1024) + " KB");

            info.Id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            using (Stream stream = new MemoryStream(bytes))
                info.Data = TrackData.Load(stream);
            if (!string.IsNullOrWhiteSpace(info.Data.Name))
                info.Name = CleanText(info.Data.Name, 40);
            info.Error = Validate(info.Data, out info.Length);
            if (info.Error == null)
                info.Heights = info.Data.DecompressHeightmap();
        }

        static void Fail(CustomTrackInfo info, string error)
        {
            info.Data = null;
            info.Heights = null;
            info.Error = error;
        }

        /// <summary>
        /// Text from a file or another player made safe for showing: control characters
        /// removed, trimmed, at most maxLength characters.
        /// </summary>
        public static string CleanText(string text, int maxLength)
        {
            if (text == null)
                return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in text)
                if (!char.IsControl(c))
                    sb.Append(c);
            string clean = sb.ToString().Trim();
            return clean.Length > maxLength ? clean.Substring(0, maxLength).TrimEnd() : clean;
        }

        /// <summary>
        /// The playable track with this Id in the Tracks folder (as last scanned), or null.
        /// </summary>
        public static CustomTrackInfo FindById(string id)
        {
            foreach (CustomTrackInfo info in tracks)
                if (info.IsPlayable && info.Id == id)
                    return info;
            return null;
        }

        /// <summary>
        /// Save a track received in a race into the Tracks folder, under a name made from the
        /// track's own name that does not overwrite anything. Returns the path, or null on failure.
        /// </summary>
        public static string SaveReceived(byte[] bytes, string name)
        {
            try
            {
                EnsureDirectory();
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (char c in CleanText(name, 40))
                    sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : (c == ' ' ? '-' : '_'));
                string baseName = sb.Length == 0 ? "Shared-Track" : sb.ToString();

                string path = System.IO.Path.Combine(Directory, baseName + "." + TrackData.Extension);
                for (int i = 2; File.Exists(path); i++)
                    path = System.IO.Path.Combine(Directory, baseName + "-" + i + "." + TrackData.Extension);
                File.WriteAllBytes(path, bytes);
                return path;
            }
            catch (Exception ex)
            {
                Log.Write("CustomTracks: can't save a shared track: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Basic structural checks only (the editor does the full validation).
        /// Returns null if the track is acceptable, otherwise a short reason.
        /// </summary>
        public static string Validate(TrackData data, out float length)
        {
            length = 0;
            if (data == null || data.TrackPoints == null)
                return "No track points";
            List<Vector3> pts = data.TrackPoints;
            if (pts.Count < MinControlPoints)
                return "Needs at least " + MinControlPoints + " track points";

            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 p = pts[i];
                if (float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsNaN(p.Z) ||
                    float.IsInfinity(p.X) || float.IsInfinity(p.Y) || float.IsInfinity(p.Z))
                    return "Point " + i + " is not a number";
                if (p.X < 0 || p.Y < 0 || p.X > MapSize || p.Y > MapSize)
                    return "Point " + i + " is outside the map (0.." + (int)MapSize + ")";
                length += Vector3.Distance(p, pts[(i + 1) % pts.Count]);
            }
            if (length < 500)
                return "Track is too short";
            if (data.Checkpoints != null)
            {
                float previous = 0;
                for (int i = 0; i < data.Checkpoints.Length; i++)
                {
                    float f = data.Checkpoints[i];
                    if (float.IsNaN(f) || f <= 0.02f || f >= 0.98f)
                        return "Checkpoint " + (i + 1) + " must be between 2% and 98% of the lap";
                    if (f < previous + 0.01f)
                        return "Checkpoint " + (i + 1) + " is out of order or less than 1% of the lap after the one before";
                    previous = f;
                }
            }
            return null;
        }
    }
}
