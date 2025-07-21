namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Represents a network message that can be serialized and deserialized for transmission.
    /// </summary>
    public interface INetworkMessage
    {
        /// <summary>
        /// Gets the type identifier of the network message.
        /// </summary>
        byte MessageType { get; }

        /// <summary>
        /// Serializes the network message into a <see cref="PacketWriter"/> for transmission.
        /// </summary>
        /// <param name="writer">The <see cref="PacketWriter"/> to serialize the message into.</param>
        void Serialize(PacketWriter writer);

        /// <summary>
        /// Deserializes the network message from a <see cref="PacketReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="PacketReader"/> to deserialize the message from.</param>
        void Deserialize(PacketReader reader);
    }
}
