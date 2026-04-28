using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Provides a UDP-based implementation of the <see cref="INetworkTransport"/> interface for sending and receiving network data.
    /// </summary>
    public class UdpTransport : INetworkTransport
    {
        private readonly UdpClient udpClient;
        private bool isBound;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpTransport"/> class.
        /// </summary>
        public UdpTransport()
        {
            udpClient = new UdpClient();
        }

        /// <summary>
        /// Receives data from the network in a blocking manner.
        /// </summary>
        /// <returns>A tuple containing the received data and the sender's endpoint.</returns>
        public (byte[] data, IPEndPoint sender) Receive()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var data = udpClient.Receive(ref remoteEndPoint);
            return (data, remoteEndPoint);
        }

        /// <summary>
        /// Receives data from the network asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The result contains the received data and the sender's endpoint.</returns>
        public async Task<(byte[] data, IPEndPoint sender)> ReceiveAsync()
        {
            var result = await udpClient.ReceiveAsync();
            return (result.Buffer, result.RemoteEndPoint);
        }

        /// <summary>
        /// Sends data to the specified endpoint in a blocking manner.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="endpoint">The endpoint to send the data to.</param>
        public void Send(byte[] data, IPEndPoint endpoint)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            udpClient.Send(data, data.Length, endpoint);
        }

        /// <summary>
        /// Sends a fixed-length prefix of data to the specified endpoint in a blocking manner.
        /// </summary>
        public void Send(byte[] data, int length, IPEndPoint endpoint)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (length < 0 || length > data.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            udpClient.Send(data, length, endpoint);
        }

        /// <summary>
        /// Sends data to the specified endpoint asynchronously.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="endpoint">The endpoint to send the data to.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendAsync(byte[] data, IPEndPoint endpoint)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            await udpClient.SendAsync(data, data.Length, endpoint);
        }

        /// <summary>
        /// Sends a fixed-length prefix of data to the specified endpoint asynchronously.
        /// </summary>
        public async Task SendAsync(byte[] data, int length, IPEndPoint endpoint)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (length < 0 || length > data.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            await udpClient.SendAsync(data, length, endpoint);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="UdpTransport"/> class.
        /// </summary>
        public void Dispose()
        {
            udpClient?.Close();
            udpClient?.Dispose();
        }

        /// <summary>
        /// Binds the transport to a local endpoint.
        /// </summary>
        public void Bind()
        {
            if (!isBound)
            {
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                isBound = true;
            }
        }

        /// <summary>
        /// Binds the transport to a specific local UDP port.
        /// </summary>
        /// <param name="port">Port to bind.</param>
        public void Bind(int port)
        {
            if (!isBound)
            {
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                isBound = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the transport is bound to a local endpoint.
        /// </summary>
        public bool IsBound => isBound;
    }
}
