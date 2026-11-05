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
            get { return Data != null && Data.Author != null ? Data.Author.Trim() : ""; }
        }

        /// <summary>
        /// Full path of the .Track file.
        /// </summary>
        public string Path;

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
                return System.IO.Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "RacingGame", "Tracks");
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
        /// Load and validate one file.
        /// </summary>
        public static CustomTrackInfo Load(string path)
        {
            CustomTrackInfo info = new CustomTrackInfo();
            info.Path = path;
            info.Name = FileDisplayName(path);
            try
            {
                using (Stream stream = File.OpenRead(path))
                    info.Data = TrackData.Load(stream);
                if (!string.IsNullOrWhiteSpace(info.Data.Name))
                    info.Name = info.Data.Name.Trim();
                info.Error = Validate(info.Data, out info.Length);
                if (info.Error == null)
                    info.Heights = info.Data.DecompressHeightmap();
            }
            catch (Exception ex)
            {
                info.Data = null;
                info.Heights = null;
                info.Error = "Can't read file: " + ex.Message;
                Log.Write("CustomTracks: " + path + ": " + ex);
            }
            return info;
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
