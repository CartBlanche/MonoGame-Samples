using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Writes network data packets in a format compatible with XNA's PacketWriter.
    /// </summary>
    public class PacketWriter : IDisposable
    {
        private readonly MemoryStream stream;
        private readonly BinaryWriter writer;
        private bool disposed;

        /// <summary>
        /// Initializes a new PacketWriter.
        /// </summary>
        public PacketWriter()
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the length of the packet data.
        /// </summary>
        public int Length => (int)stream.Length;

        /// <summary>
        /// Gets the position in the packet data.
        /// </summary>
        public int Position
        {
            get => (int)stream.Position;
            set => stream.Position = value;
        }

        /// <summary>
        /// Writes a boolean value.
        /// </summary>
        public void Write(bool value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes a byte value.
        /// </summary>
        public void Write(byte value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes a signed byte value.
        /// </summary>
        public void Write(sbyte value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes a 16-bit integer.
        /// </summary>
        public void Write(short value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer.
        /// </summary>
        public void Write(ushort value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes a 32-bit integer.
        /// </summary>
        public void Write(int value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer.
        /// </summary>
        public void Write(uint value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes a 64-bit integer.
        /// </summary>
        public void Write(long value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes an unsigned 64-bit integer.
        /// </summary>
        public void Write(ulong value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes a single-precision floating point value.
        /// </summary>
        public void Write(float value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes a double-precision floating point value.
        /// </summary>
        public void Write(double value)
        {
            writer.Write(value);
        }

        /// <summary>
        /// Writes a string value.
        /// </summary>
        public void Write(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            writer.Write(value);
        }

        /// <summary>
        /// Writes a Vector2 value.
        /// </summary>
        public void Write(Vector2 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        /// <summary>
        /// Writes a Vector3 value.
        /// </summary>
        public void Write(Vector3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        /// <summary>
        /// Writes a Vector4 value.
        /// </summary>
        public void Write(Vector4 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        /// <summary>
        /// Writes a Quaternion value.
        /// </summary>
        public void Write(Quaternion value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        /// <summary>
        /// Writes a Matrix value.
        /// </summary>
        public void Write(Matrix value)
        {
            writer.Write(value.M11); writer.Write(value.M12); writer.Write(value.M13); writer.Write(value.M14);
            writer.Write(value.M21); writer.Write(value.M22); writer.Write(value.M23); writer.Write(value.M24);
            writer.Write(value.M31); writer.Write(value.M32); writer.Write(value.M33); writer.Write(value.M34);
            writer.Write(value.M41); writer.Write(value.M42); writer.Write(value.M43); writer.Write(value.M44);
        }

        /// <summary>
        /// Writes a Color value.
        /// </summary>
        public void Write(Color value)
        {
            writer.Write(value.R);
            writer.Write(value.G);
            writer.Write(value.B);
            writer.Write(value.A);
        }

        /// <summary>
        /// Writes raw byte data.
        /// </summary>
        public void Write(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            writer.Write(buffer.Length);
            writer.Write(buffer);
        }

        /// <summary>
        /// Writes a portion of a byte array.
        /// </summary>
        public void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            writer.Write(count);
            writer.Write(buffer, offset, count);
        }

        /// <summary>
        /// Gets the packet data as a byte array.
        /// </summary>
        internal byte[] GetData()
        {
            return stream.ToArray();
        }

        /// <summary>
        /// Creates a packet reader for the current data.
        /// </summary>
        internal PacketReader CreateReader()
        {
            return new PacketReader(GetData());
        }

        /// <summary>
        /// Closes the packet writer.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the packet writer.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                writer?.Dispose();
                stream?.Dispose();
                disposed = true;
            }
        }
    }
}
