using System;
using System.Net;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Event arguments for when a network message is received.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the received message.
        /// </summary>
        public INetworkMessage Message { get; }

        /// <summary>
        /// Gets the remote endpoint that sent the message.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets the gamer who sent the message.
        /// </summary>
        public INetworkGamer Sender { get; internal set; }

        /// <summary>
        /// Initializes a new instance of MessageReceivedEventArgs.
        /// </summary>
        public MessageReceivedEventArgs(INetworkMessage message, IPEndPoint remoteEndPoint)
        {
            Message = message;
            RemoteEndPoint = remoteEndPoint;
            Sender = null;
        }
    }
}
