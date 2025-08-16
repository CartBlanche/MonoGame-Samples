using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Reads network data packets in a format compatible with XNA's PacketReader.
    /// </summary>
    public class PacketReader : IDisposable
    {
        private readonly MemoryStream stream;
        private readonly BinaryReader reader;
        private bool disposed;

        /// <summary>
        /// Initializes a new PacketReader from byte data.
        /// </summary>
        internal PacketReader(byte[] data)
        {
            stream = new MemoryStream(data);
            reader = new BinaryReader(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Initializes a new empty PacketReader.
        /// </summary>
        public PacketReader()
        {
            stream = new MemoryStream();
            reader = new BinaryReader(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Replaces the internal buffer with the provided data and rewinds to the start.
        /// This mirrors XNA's PacketReader.SetBuffer behavior for reuse.
        /// </summary>
        public void Reset(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            stream.SetLength(0);
            stream.Position = 0;
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
        }

        /// <summary>
        /// Gets the length of the packet data.
        /// </summary>
        public int Length => (int)stream.Length;

        /// <summary>
        /// Gets the current position in the packet data.
        /// </summary>
        public int Position
        {
            get => (int)stream.Position;
            set => stream.Position = value;
        }

        /// <summary>
        /// Gets the number of bytes remaining to be read.
        /// </summary>
        public int BytesRemaining => Length - Position;

        /// <summary>
        /// Reads a boolean value.
        /// </summary>
        public bool ReadBoolean()
        {
            return reader.ReadBoolean();
        }

        /// <summary>
        /// Reads a byte value.
        /// </summary>
        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        /// <summary>
        /// Reads a signed byte value.
        /// </summary>
        public sbyte ReadSByte()
        {
            return reader.ReadSByte();
        }

        /// <summary>
        /// Reads a 16-bit integer.
        /// </summary>
        public short ReadInt16()
        {
            return reader.ReadInt16();
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer.
        /// </summary>
        public ushort ReadUInt16()
        {
            return reader.ReadUInt16();
        }

        /// <summary>
        /// Reads a 32-bit integer.
        /// </summary>
        public int ReadInt32()
        {
            return reader.ReadInt32();
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer.
        /// </summary>
        public uint ReadUInt32()
        {
            return reader.ReadUInt32();
        }

        /// <summary>
        /// Reads a 64-bit integer.
        /// </summary>
        public long ReadInt64()
        {
            return reader.ReadInt64();
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer.
        /// </summary>
        public ulong ReadUInt64()
        {
            return reader.ReadUInt64();
        }

        /// <summary>
        /// Reads a single-precision floating point value.
        /// </summary>
        public float ReadSingle()
        {
            return reader.ReadSingle();
        }

        /// <summary>
        /// Reads a double-precision floating point value.
        /// </summary>
        public double ReadDouble()
        {
            return reader.ReadDouble();
        }

        /// <summary>
        /// Reads a string value.
        /// </summary>
        public string ReadString()
        {
            return reader.ReadString();
        }

        /// <summary>
        /// Reads a Vector2 value.
        /// </summary>
        public Vector2 ReadVector2()
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        /// <summary>
        /// Reads a Vector3 value.
        /// </summary>
        public Vector3 ReadVector3()
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        /// <summary>
        /// Reads a Vector4 value.
        /// </summary>
        public Vector4 ReadVector4()
        {
            return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        /// <summary>
        /// Reads a Quaternion value.
        /// </summary>
        public Quaternion ReadQuaternion()
        {
            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        /// <summary>
        /// Reads a Matrix value.
        /// </summary>
        public Matrix ReadMatrix()
        {
            return new Matrix(
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
            );
        }

        /// <summary>
        /// Reads a Color value.
        /// </summary>
        public Color ReadColor()
        {
            return new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }

        /// <summary>
        /// Reads a byte array.
        /// </summary>
        public byte[] ReadBytes()
        {
            int length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        /// <summary>
        /// Reads a specified number of bytes.
        /// </summary>
        public byte[] ReadBytes(int count)
        {
            return reader.ReadBytes(count);
        }

        /// <summary>
        /// Closes the packet reader.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the packet reader.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                reader?.Dispose();
                stream?.Dispose();
                disposed = true;
            }
        }
    }
}