// DiscoveryPacket.cs - Replaces your string header
using System;
using System.IO;
using System.Net;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Immutable discovery packet with length-prefixed binary format.
    /// Format: [Magic(4)][Version(1)][Type(1)][PayloadLength(2)][Payload][CRC32(4)]
    /// </summary>
    public class DiscoveryPacket
    {
        public const uint MAGIC = 0x4D6F6E6F; // "Mono" in hex
        public const byte PROTOCOL_VERSION = 1;
        public const byte TYPE_DISCOVERY = 1;
        public const byte TYPE_RESPONSE = 2;

        public uint Magic { get; } = MAGIC;
        public byte Version { get; } = PROTOCOL_VERSION;
        public byte Type { get; }
        public DiscoveryPayload Payload { get; }

        public DiscoveryPacket(byte type, DiscoveryPayload payload)
        {
            Type = type;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        // Serialize to bytes using PacketWriter
        public byte[] ToByteArray()
        {
            var writer = new PacketWriter();

            // Header
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(Type);

            // Payload (write length first, then data)
            var payloadBytes = Payload.ToByteArray();
            writer.Write((ushort)payloadBytes.Length);
            writer.Write(payloadBytes);

            // CRC32 checksum (calculate on all data before CRC field)
            var dataWithoutCrc = writer.GetData();
            uint crc = CalculateCRC32(dataWithoutCrc, 0, dataWithoutCrc.Length);
            writer.Write(crc);

            return writer.GetData();
        }

        // Deserialize from bytes
        public static bool TryParse(byte[] data, out DiscoveryPacket packet)
        {
            packet = null;
            if (data == null || data.Length < 12) return false; // Minimum size

            try
            {
                var reader = new PacketReader(data);

                var magic = reader.ReadUInt32();
                if (magic != MAGIC) return false;

                var version = reader.ReadByte();
                if (version != PROTOCOL_VERSION) return false;

                var type = reader.ReadByte();
                var payloadLength = reader.ReadUInt16();

                // Validate payload length
                if (payloadLength > reader.BytesRemaining - 4) return false;

                var payloadBytes = reader.ReadBytes(payloadLength);
                var checksum = reader.ReadUInt32();

                // Verify CRC (basic check)
                var calculatedCrc = CalculateCRC32(data, 0, data.Length - 4);
                if (checksum != calculatedCrc) return false;

                if (!DiscoveryPayload.TryParse(payloadBytes, out var payload)) return false;

                packet = new DiscoveryPacket(type, payload);
                return true;
            }
            catch
            {
                return false; // Any parse error = invalid packet
            }
        }

        private static uint CalculateCRC32(byte[] data, int offset, int length)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = offset; i < offset + length; i++)
            {
                crc ^= data[i];
                for (uint j = 0; j < 8; j++)
                {
                    bool isOdd = (crc & 1) != 0;
                    crc = (crc >> 1) ^ (isOdd ? 0xEDB88320 : 0);
                }
            }
            return ~crc;
        }
    }

    /// <summary>
    /// Payload containing session information.
    /// </summary>
    public class DiscoveryPayload
    {
        public string SessionId { get; }
        public int MaxGamers { get; }
        public int PrivateGamerSlots { get; }
        public string HostGamertag { get; }
        public int GamePort { get; }
        public byte[] SessionProperties { get; }

        public DiscoveryPayload(string sessionId, int maxGamers, int privateSlots,
                               string hostGamertag, int gamePort, byte[] properties)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            MaxGamers = maxGamers;
            PrivateGamerSlots = privateSlots;
            HostGamertag = hostGamertag ?? throw new ArgumentNullException(nameof(hostGamertag));
            GamePort = gamePort;
            SessionProperties = properties ?? Array.Empty<byte>();

            Validate();
        }

        private void Validate()
        {
            if (SessionId.Length != 36) throw new ArgumentException("Invalid GUID length", nameof(SessionId));
            if (MaxGamers < 1 || MaxGamers > 32) throw new ArgumentOutOfRangeException(nameof(MaxGamers));
            if (PrivateGamerSlots < 0 || PrivateGamerSlots > MaxGamers)
                throw new ArgumentOutOfRangeException(nameof(PrivateGamerSlots));
            if (GamePort < 1024 || GamePort > 65535) throw new ArgumentOutOfRangeException(nameof(GamePort));
            if (HostGamertag.Length > 32) throw new ArgumentException("Gamertag too long", nameof(HostGamertag));
        }

        public byte[] ToByteArray()
        {
            var writer = new PacketWriter();
            writer.Write(SessionId);
            writer.Write(MaxGamers);
            writer.Write(PrivateGamerSlots);
            writer.Write(HostGamertag);
            writer.Write(GamePort);
            writer.Write(SessionProperties);
            return writer.GetData();
        }

        public static bool TryParse(byte[] data, out DiscoveryPayload payload)
        {
            payload = null;
            if (data == null || data.Length < 50) return false; // Minimum GUID + metadata

            try
            {
                var reader = new PacketReader(data);
                var sessionId = reader.ReadString();
                var maxGamers = reader.ReadInt32();
                var privateSlots = reader.ReadInt32();
                var gamertag = reader.ReadString();
                var gamePort = reader.ReadInt32();
                var properties = reader.ReadBytes();

                payload = new DiscoveryPayload(sessionId, maxGamers, privateSlots, gamertag, gamePort, properties);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}