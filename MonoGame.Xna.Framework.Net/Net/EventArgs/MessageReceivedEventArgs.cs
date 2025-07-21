using System;
using System.Net;

namespace Microsoft.Xna.Framework.Net
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public INetworkMessage Message { get; }
        public IPEndPoint RemoteEndPoint { get; }

        public MessageReceivedEventArgs(INetworkMessage message, IPEndPoint remoteEndPoint)
        {
            Message = message;
            RemoteEndPoint = remoteEndPoint;
        }
    }
}
