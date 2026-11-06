using System;
using System.Collections.Generic;
using System.IO;
using RacingGame.Tracks;

namespace RacingGame.Multiplayer
{
    /// <summary>
    /// A custom track that is used in a race: the file's bytes (what the host sends to the others)
    /// and the loaded track.
    /// </summary>
    public class SharedTrack
    {
        public string Id;
        public byte[] Bytes;
        public CustomTrackInfo Info;

        /// <summary>
        /// Came over the network (the player does not have the file in the Tracks folder)
        /// </summary>
        public bool Received;

        /// <summary>
        /// Saved into the Tracks folder after the race
        /// </summary>
        public bool Saved;
    }

    /// <summary>
    /// The custom tracks known to this session: the one the host has chosen, and tracks received
    /// from a host. A received track stays in memory only, until the player chooses to save it.
    /// </summary>
    public static class SharedTracks
    {
        static readonly Dictionary<string, SharedTrack> cache = new Dictionary<string, SharedTrack>();

        /// <summary>
        /// The track with this id from the cache or the Tracks folder, or null if this machine
        /// does not have it.
        /// </summary>
        public static SharedTrack Resolve(string id)
        {
            SharedTrack shared;
            if (cache.TryGetValue(id, out shared))
                return shared;

            CustomTracks.Refresh();
            CustomTrackInfo info = CustomTracks.FindById(id);
            if (info == null)
                return null;
            return FromInfo(info);
        }

        /// <summary>
        /// A track of the Tracks folder as a shared track (its file is read now, so the host can send it)
        /// </summary>
        public static SharedTrack FromInfo(CustomTrackInfo info)
        {
            SharedTrack shared;
            if (cache.TryGetValue(info.Id, out shared))
                return shared;

            shared = new SharedTrack { Id = info.Id, Info = info };
            try
            {
                if (info.Path != null)
                    shared.Bytes = File.ReadAllBytes(info.Path);
            }
            catch (Exception ex)
            {
                NetLog.Write("Can't read " + info.Path + ": " + ex.Message);
            }
            cache[info.Id] = shared;
            return shared;
        }

        /// <summary>
        /// Keep a track received from the host
        /// </summary>
        public static SharedTrack AddReceived(byte[] bytes, CustomTrackInfo info)
        {
            SharedTrack shared = new SharedTrack { Id = info.Id, Bytes = bytes, Info = info, Received = true };
            cache[info.Id] = shared;
            return shared;
        }

        public static SharedTrack Get(string id)
        {
            SharedTrack shared;
            return id != null && cache.TryGetValue(id, out shared) ? shared : null;
        }
    }
}
