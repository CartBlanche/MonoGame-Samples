//-----------------------------------------------------------------------------
// TrackData.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using RacingGame.Helpers;
#if NETFX_CORE
using Serializable = System.Runtime.Serialization.DataContractAttribute;
#endif

namespace RacingGame.Tracks
{
    /// <summary>
    /// Track data, imported from 3ds max data. See TrackImporter class.
    /// </summary>
    public class TrackData
    {
        /// <summary>
        /// Directory where all the track data files are stored.
        /// </summary>
        public const string Directory = "Content";
        /// <summary>
        /// Extension for the track data files.
        /// </summary>
        public const string Extension = "Track";
        // Metadata (all optional, so the built-in tracks load unchanged).
        // Writers should emit these first, in this order (tested: the reader
        // also accepts them after the lists).

        /// <summary>
        /// Format version of the file, 1 = first version with metadata.
        /// </summary>
        public int FormatVersion;
        /// <summary>
        /// Display name of the track (free text). Empty: use the file name.
        /// </summary>
        public string Name;
        /// <summary>
        /// Who created this track (free text).
        /// </summary>
        public string Author;
        /// <summary>
        /// Creator of the first version if someone else edited it later.
        /// </summary>
        public string OriginalAuthor;
        /// <summary>
        /// Short description (free text).
        /// </summary>
        public string Description;
        /// <summary>
        /// When the track was created (ISO date, e.g. 2026-11-04).
        /// </summary>
        public string Created;

        /// <summary>
        /// Optional timing checkpoints: where along the lap each one is, as a
        /// fraction of the lap (0..1, increasing, measured from the start line).
        /// Null: the game puts one every 500 units. An empty list: none.
        /// </summary>
        public float[] Checkpoints;

        /// <summary>
        /// Optional own terrain for this track: the 257 x 257 height bytes
        /// (same layout as Content/LandscapeHeights.data), deflate compressed.
        /// XmlSerializer writes this as base64. Null: the shipped terrain.
        /// Use CompressHeightmap / DecompressHeightmap, don't fill by hand.
        /// </summary>
        public byte[] Heightmap;

        /// <summary>
        /// Size of an uncompressed heightmap (Landscape.HeightmapSize).
        /// </summary>
        public const int HeightmapBytes = 257 * 257;

        /// <summary>
        /// Compress raw heightmap bytes for storing in the Heightmap field.
        /// </summary>
        public static byte[] CompressHeightmap(byte[] raw)
        {
            if (raw == null || raw.Length != HeightmapBytes)
                throw new ArgumentException("Heightmap must be " + HeightmapBytes + " bytes");
            using (MemoryStream ms = new MemoryStream())
            {
                using (System.IO.Compression.DeflateStream ds = new System.IO.Compression.DeflateStream(
                    ms, System.IO.Compression.CompressionLevel.Optimal, true))
                    ds.Write(raw, 0, raw.Length);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Raw heightmap bytes, or null if the track has no own heightmap.
        /// Throws InvalidDataException if the stored data is not exactly
        /// 257 x 257 bytes (also protects against decompression bombs).
        /// </summary>
        public byte[] DecompressHeightmap()
        {
            if (Heightmap == null || Heightmap.Length == 0)
                return null;
            byte[] raw = new byte[HeightmapBytes];
            using (MemoryStream ms = new MemoryStream(Heightmap))
            using (System.IO.Compression.DeflateStream ds = new System.IO.Compression.DeflateStream(
                ms, System.IO.Compression.CompressionMode.Decompress))
            {
                int read = 0;
                while (read < raw.Length)
                {
                    int n = ds.Read(raw, read, raw.Length - read);
                    if (n <= 0)
                        break;
                    read += n;
                }
                if (read != raw.Length || ds.ReadByte() != -1)
                    throw new InvalidDataException("Heightmap is not " + HeightmapBytes + " bytes");
            }
            return raw;
        }

        /// <summary>
        /// Track points
        /// </summary>
        private List<Vector3> trackPoints = new List<Vector3>();
        /// <summary>
        /// Width helper
        /// </summary>
        [Serializable]
        public class WidthHelper
        {
            /// <summary>
            /// Position
            /// </summary>
            public Vector3 pos;
            /// <summary>
            /// Scale
            /// </summary>
            public float scale;

            /// <summary>
            /// Create width helper
            /// </summary>
            public WidthHelper()
            {
            }

            /// <summary>
            /// Create width helper
            /// </summary>
            /// <param name="setPos">Set position</param>
            /// <param name="setScale">Set scale</param>
            public WidthHelper(Vector3 setPos, float setScale)
            {
                pos = setPos;
                scale = setScale;
            }
        }

        /// <summary>
        /// Width helper position
        /// </summary>
        private List<WidthHelper> widthHelpers = new List<WidthHelper>();
        /// <summary>
        /// Road helper
        /// </summary>
        [Serializable]
        public class RoadHelper
        {
            /// <summary>
            /// Helper type
            /// </summary>
            public enum HelperType
            {
                Tunnel,
                Palms,
                Laterns,
                Reset,
            }

            /// <summary>
            /// Type
            /// </summary>
            public HelperType type;
            /// <summary>
            /// Position
            /// </summary>
            public Vector3 pos;

            /// <summary>
            /// Create road helper
            /// </summary>
            public RoadHelper()
            {
            }

            /// <summary>
            /// Create road helper
            /// </summary>
            /// <param name="setType">Set type</param>
            /// <param name="setPos">Set position</param>
            public RoadHelper(HelperType setType, Vector3 setPos)
            {
                type = setType;
                pos = setPos;
            }
        }

        /// <summary>
        /// Tunnel helper position
        /// </summary>
        private List<RoadHelper> roadHelpers = new List<RoadHelper>();
        /// <summary>
        /// Neutral object
        /// </summary>
        [Serializable]
        public class NeutralObject
        {
            /// <summary>
            /// Model name
            /// </summary>
            public string modelName;
            /*not required, just use the matrix
            /// <summary>
            /// Position
            /// </summary>
            public Vector3 pos;
             */
            /// <summary>
            /// Matrix
            /// </summary>
            public Matrix matrix;

            /// <summary>
            /// Create neutral object
            /// </summary>
            public NeutralObject()
            {
            }

            /// <summary>
            /// Create neutral object
            /// </summary>
            /// <param name="setModelName">Set model name</param>
            /// <param name="setMatrix">Set matrix</param>
            public NeutralObject(string setModelName, Matrix setMatrix)
            {
                modelName = setModelName;
                matrix = setMatrix;
            }
        }

        /// <summary>
        /// List of neutral objects used in this level
        /// </summary>
        private List<NeutralObject> objects = new List<NeutralObject>();
        /// <summary>
        /// Track points
        /// </summary>
        /// <returns>List</returns>
        public List<Vector3> TrackPoints
        {
            get
            {
                return trackPoints;
            }
        }

        /// <summary>
        /// Width helpers
        /// </summary>
        /// <returns>List</returns>
        public List<WidthHelper> WidthHelpers
        {
            get
            {
                return widthHelpers;
            }
        }

        /// <summary>
        /// Tunnel helper position
        /// </summary>
        /// <returns>List</returns>
        public List<RoadHelper> RoadHelpers
        {
            get
            {
                return roadHelpers;
            }
        }

        /// <summary>
        /// Neutrals objects
        /// </summary>
        /// <returns>List</returns>
        public List<NeutralObject> NeutralsObjects
        {
            get
            {
                return objects;
            }
        }
        /// <summary>
        /// Create track data, empty constructor, required for Serialization.
        /// </summary>
        public TrackData()
        {
        }

        /// <summary>
        /// Create track data (used only in TrackImporter).
        /// </summary>
        /// <param name="setFilename">Set filename</param>
        /// <param name="setTrackPoints">Set track points</param>
        /// <param name="setWidthHelpers">Set width helpers</param>
        /// <param name="setRoadHelpers">Set road helpers</param>
        /// <param name="setObjects">Set objects</param>
        public TrackData(List<Vector3> setTrackPoints,
            List<WidthHelper> setWidthHelpers,
            List<RoadHelper> setRoadHelpers,
            List<NeutralObject> setObjects)
        {
            trackPoints = setTrackPoints;
            widthHelpers = setWidthHelpers;
            roadHelpers = setRoadHelpers;
            objects = setObjects;
        }

        /// <summary>
        /// Load track data
        /// </summary>
        /// <param name="setFilename">Set filename</param>
        public static TrackData Load(string setFilename)
        {
            // Load track data
            using (StreamReader file = new StreamReader(TitleContainer.OpenStream(
                Path.Combine(Directory, setFilename + "." + Extension))))
            {
                // Load everything into this class with help of the XmlSerializer.
                TrackData loadedTrack = (TrackData)
                    new XmlSerializer(typeof(TrackData)).
                    Deserialize(file.BaseStream);
                // Return loaded file
                return loadedTrack;
            }
        }

        /// <summary>
        /// Load track data from any stream (used for custom tracks in the
        /// user's Tracks folder, see CustomTracks).
        /// </summary>
        public static TrackData Load(Stream stream)
        {
            return (TrackData)new XmlSerializer(typeof(TrackData)).Deserialize(stream);
        }

    }
}
