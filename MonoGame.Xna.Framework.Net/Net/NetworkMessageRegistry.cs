using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Provides a registry for network message types, allowing messages to be registered and created dynamically.
    /// </summary>
    public static class NetworkMessageRegistry
    {
        private static readonly Dictionary<byte, Func<INetworkMessage>> registry = new();

        /// <summary>
        /// Registers a network message type with the specified type identifier.
        /// </summary>
        /// <typeparam name="T">The type of the network message to register. Must implement <see cref="INetworkMessage"/> and have a parameterless constructor.</typeparam>
        /// <param name="typeId">The unique identifier for the message type.</param>
        public static void Register<T>(byte typeId) where T : INetworkMessage, new()
        {
            registry[typeId] = () => new T();
        }

        /// <summary>
        /// Creates a network message instance based on the specified type identifier.
        /// </summary>
        /// <param name="typeId">The unique identifier for the message type.</param>
        /// <returns>An instance of the network message, or <c>null</c> if the type identifier is not registered.</returns>
        public static INetworkMessage CreateMessage(byte typeId)
        {
            return registry.TryGetValue(typeId, out var ctor) ? ctor() : null;
        }

        static NetworkMessageRegistry()
        {
            Register<JoinRequestMessage>(2);
            Register<JoinAcceptedMessage>(3);
        }
    }
}