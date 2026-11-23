using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace RacingGame.Multiplayer
{
    /// <summary>
    /// Gameplay packet types, the first byte of every payload. The Net library keeps the ids
    /// 1 to 10 for its own messages, and its channel framing uses 0x21 to 0x25, so gameplay
    /// ids start well above both (as in ShipGame).
    /// </summary>
    public enum RacePacketType : byte
    {
        LobbyUpdate = 70,   // client -> host: my car, color and ready state
        LobbySync = 71,     // host -> clients: the whole lobby (players, track, laps)
        RaceStart = 72,     // host -> all: the track, and who drives which car from which grid slot
        Ready = 73,         // client -> host: the track is loaded, I am waiting at the start
        Go = 74,            // host -> all: everybody is ready, start the countdown
        PlayerFinished = 75,// reliable: a player finished the race or dropped out (the host relays it)
        TrackRequest = 76,  // client -> host: I do not have the custom track with this id, send it
        TrackFile = 77,     // host -> client: the file of the custom track (reliable, big)
        CarState = 80,      // everybody: where my car is (unreliable, about 20 Hz; the host relays it)
    }

    /// <summary>
    /// A gameplay packet. The type byte is written by <see cref="RaceNetSession"/> before
    /// <see cref="Serialize"/>; each packet class also has a static Deserialize(BinaryReader).
    /// </summary>
    public interface INetworkPacket
    {
        RacePacketType Type { get; }
        void Serialize(BinaryWriter writer);
    }

    /// <summary>
    /// Helpers for writing XNA types with a BinaryWriter / BinaryReader
    /// </summary>
    public static class NetworkSerialization
    {
        public static void Write(this BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this BinaryWriter writer, Quaternion value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public static Quaternion ReadQuaternion(this BinaryReader reader)
        {
            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle());
        }
    }

    /// <summary>
    /// One player in a starting race
    /// </summary>
    public class RacePlayerInfo
    {
        public string Id { get; set; }      // network gamer id (unique per machine)
        public string Name { get; set; }
        public byte Car { get; set; }       // car model / texture number
        public byte Color { get; set; }     // index into RacingGameManager.CarColors
        public byte Slot { get; set; }      // start grid slot, 0 is the host
        public bool Ready { get; set; }
        public bool HasTrack { get; set; } = true;  // false while a custom track is still being fetched

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Name);
            writer.Write(Car);
            writer.Write(Color);
            writer.Write(Slot);
            writer.Write(Ready);
            writer.Write(HasTrack);
        }

        public static RacePlayerInfo Deserialize(BinaryReader reader)
        {
            return new RacePlayerInfo
            {
                Id = reader.ReadString(),
                Name = reader.ReadString(),
                Car = reader.ReadByte(),
                Color = reader.ReadByte(),
                Slot = reader.ReadByte(),
                Ready = reader.ReadBoolean(),
                HasTrack = reader.ReadBoolean(),
            };
        }
    }

    /// <summary>
    /// A custom track as announced in the lobby and the race start: its id (the SHA-256 of the file),
    /// and its name and author for showing before the file has arrived. Written after the track
    /// number when that is <see cref="RaceLobby.CustomTrackNumber"/>.
    /// </summary>
    public static class CustomTrackAnnouncement
    {
        public static void Write(BinaryWriter writer, string id, string name, string author)
        {
            writer.Write(id ?? "");
            writer.Write(name ?? "");
            writer.Write(author ?? "");
        }

        public static void Read(BinaryReader reader, out string id, out string name, out string author)
        {
            // the strings come from the host: limit them
            id = reader.ReadString();
            name = RacingGame.Tracks.CustomTracks.CleanText(reader.ReadString(), 40);
            author = RacingGame.Tracks.CustomTracks.CleanText(reader.ReadString(), 40);
            if (id.Length != 64)
                throw new InvalidDataException("bad track id");
        }
    }

    /// <summary>
    /// Client to host: which car this player drives, and whether the player is ready to race.
    /// The first one a client sends also puts it in the lobby.
    /// </summary>
    public class LobbyUpdatePacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.LobbyUpdate; } }
        public byte Car { get; set; }
        public byte Color { get; set; }
        public bool Ready { get; set; }
        public bool HasTrack { get; set; } = true;
        /// <summary>
        /// The player's chosen name (the Name setting), shown instead of the unique gamertag
        /// </summary>
        public string Name { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Car);
            writer.Write(Color);
            writer.Write(Ready);
            writer.Write(HasTrack);
            writer.Write(Name ?? "");
        }

        public static LobbyUpdatePacket Deserialize(BinaryReader reader)
        {
            return new LobbyUpdatePacket
            {
                Car = reader.ReadByte(), Color = reader.ReadByte(), Ready = reader.ReadBoolean(),
                HasTrack = reader.ReadBoolean(),
                Name = reader.ReadString(),
            };
        }
    }

    /// <summary>
    /// Host to clients: the whole lobby, sent whenever anything in it changes
    /// </summary>
    public class LobbySyncPacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.LobbySync; } }
        public byte Track { get; set; }             // 0 to 2 built in, 255 = a custom track
        public byte Laps { get; set; }
        public string CustomId { get; set; }        // only when Track is 255
        public string CustomName { get; set; }
        public string CustomAuthor { get; set; }
        public List<RacePlayerInfo> Players { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Track);
            writer.Write(Laps);
            if (Track == RaceLobby.CustomTrackNumber)
                CustomTrackAnnouncement.Write(writer, CustomId, CustomName, CustomAuthor);
            writer.Write((byte)Players.Count);
            foreach (RacePlayerInfo player in Players)
                player.Serialize(writer);
        }

        public static LobbySyncPacket Deserialize(BinaryReader reader)
        {
            LobbySyncPacket packet = new LobbySyncPacket { Track = reader.ReadByte(), Laps = reader.ReadByte() };
            if (packet.Track == RaceLobby.CustomTrackNumber)
            {
                string id, name, author;
                CustomTrackAnnouncement.Read(reader, out id, out name, out author);
                packet.CustomId = id;
                packet.CustomName = name;
                packet.CustomAuthor = author;
            }
            int count = reader.ReadByte();
            packet.Players = new List<RacePlayerInfo>(count);
            for (int i = 0; i < count; i++)
                packet.Players.Add(RacePlayerInfo.Deserialize(reader));
            return packet;
        }
    }

    /// <summary>
    /// Host to everybody: the race begins
    /// </summary>
    public class RaceStartPacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.RaceStart; } }
        public byte Track { get; set; }
        public byte Laps { get; set; }
        public string CustomId { get; set; }        // only when Track is 255
        public string CustomName { get; set; }
        public string CustomAuthor { get; set; }
        public List<RacePlayerInfo> Players { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Track);
            writer.Write(Laps);
            if (Track == RaceLobby.CustomTrackNumber)
                CustomTrackAnnouncement.Write(writer, CustomId, CustomName, CustomAuthor);
            writer.Write((byte)Players.Count);
            foreach (RacePlayerInfo player in Players)
                player.Serialize(writer);
        }

        public static RaceStartPacket Deserialize(BinaryReader reader)
        {
            RaceStartPacket packet = new RaceStartPacket { Track = reader.ReadByte(), Laps = reader.ReadByte() };
            if (packet.Track == RaceLobby.CustomTrackNumber)
            {
                string id, name, author;
                CustomTrackAnnouncement.Read(reader, out id, out name, out author);
                packet.CustomId = id;
                packet.CustomName = name;
                packet.CustomAuthor = author;
            }
            int count = reader.ReadByte();
            packet.Players = new List<RacePlayerInfo>(count);
            for (int i = 0; i < count; i++)
                packet.Players.Add(RacePlayerInfo.Deserialize(reader));
            return packet;
        }
    }

    /// <summary>
    /// Client to host: send me the file of the custom track with this id
    /// </summary>
    public class TrackRequestPacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.TrackRequest; } }
        public string Id { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
        }

        public static TrackRequestPacket Deserialize(BinaryReader reader)
        {
            return new TrackRequestPacket { Id = reader.ReadString() };
        }
    }

    /// <summary>
    /// Host to client: the file of a custom track. Reliable; the Net library splits it into
    /// datagrams and puts it back together (up to 1 MB), so it is a single packet here.
    /// </summary>
    public class TrackFilePacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.TrackFile; } }
        public string Id { get; set; }
        public byte[] Data { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Data.Length);
            writer.Write(Data);
        }

        /// <summary>
        /// Data is null if the announced size is not acceptable (the bytes are then not read)
        /// </summary>
        public static TrackFilePacket Deserialize(BinaryReader reader)
        {
            TrackFilePacket packet = new TrackFilePacket { Id = reader.ReadString() };
            int length = reader.ReadInt32();
            if (length > 0 && length <= RacingGame.Tracks.CustomTracks.MaxSharedFileBytes)
                packet.Data = reader.ReadBytes(length);
            return packet;
        }
    }

    /// <summary>
    /// Where a car is. The rotation and position are those of the car's render matrix, so a
    /// remote car is drawn exactly as the local one is.
    /// </summary>
    public class CarStatePacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.CarState; } }
        public byte Slot { get; set; }
        public ushort Sequence { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float WheelPos { get; set; }
        public float Speed { get; set; }
        public byte Lap { get; set; }           // laps completed
        public float Progress { get; set; }     // how far along the track in this lap
        /// <summary>Bit flags, see FlagBraking. Other bits are reserved (send 0).</summary>
        public byte Flags { get; set; }
        /// <summary>Flags bit 0: the brakes are applied (brake lights).</summary>
        public const byte FlagBraking = 1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Slot);
            writer.Write(Sequence);
            writer.Write(Position);
            writer.Write(Rotation);
            writer.Write(WheelPos);
            writer.Write(Speed);
            writer.Write(Lap);
            writer.Write(Progress);
            writer.Write(Flags);
        }

        public static CarStatePacket Deserialize(BinaryReader reader)
        {
            return new CarStatePacket
            {
                Slot = reader.ReadByte(),
                Sequence = reader.ReadUInt16(),
                Position = reader.ReadVector3(),
                Rotation = reader.ReadQuaternion(),
                WheelPos = reader.ReadSingle(),
                Speed = reader.ReadSingle(),
                Lap = reader.ReadByte(),
                Progress = reader.ReadSingle(),
                Flags = reader.ReadByte(),
            };
        }
    }

    /// <summary>
    /// Client to host: this machine has loaded the track and waits at the start line
    /// </summary>
    public class ReadyPacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.Ready; } }
        public void Serialize(BinaryWriter writer) { }
        public static ReadyPacket Deserialize(BinaryReader reader) { return new ReadyPacket(); }
    }

    /// <summary>
    /// Host to everybody: start the countdown now
    /// </summary>
    public class GoPacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.Go; } }
        public void Serialize(BinaryWriter writer) { }
        public static GoPacket Deserialize(BinaryReader reader) { return new GoPacket(); }
    }

    /// <summary>
    /// How a player's race ended
    /// </summary>
    public enum RaceOutcome : byte
    {
        Racing = 0,
        Finished = 1,
        DidNotFinish = 2,   // left the track, or left the race
    }

    /// <summary>
    /// A player finished all the laps, or dropped out. Sent reliably by the player's own machine
    /// to the host, which passes it on to the other clients (and sends it itself, on behalf of a
    /// player that has left). Every machine ends up with the same results.
    /// </summary>
    public class PlayerFinishedPacket : INetworkPacket
    {
        public RacePacketType Type { get { return RacePacketType.PlayerFinished; } }
        public byte Slot { get; set; }
        public RaceOutcome Outcome { get; set; }
        public float TimeSeconds { get; set; }  // race time, for a player that finished

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Slot);
            writer.Write((byte)Outcome);
            writer.Write(TimeSeconds);
        }

        public static PlayerFinishedPacket Deserialize(BinaryReader reader)
        {
            return new PlayerFinishedPacket
            {
                Slot = reader.ReadByte(),
                Outcome = (RaceOutcome)reader.ReadByte(),
                TimeSeconds = reader.ReadSingle(),
            };
        }
    }
}
