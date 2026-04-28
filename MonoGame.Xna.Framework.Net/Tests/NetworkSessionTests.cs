using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Net;
using System.Collections.Generic;
using System;
using System.Net;
using System.Threading;
using System.Linq;

namespace Microsoft.Xna.Framework.Net.Tests
{
    [TestFixture]
    public class NetworkSessionTests
    {
        private sealed class FakeNetworkTransport : INetworkTransport
        {
            private readonly Queue<(byte[] data, IPEndPoint sender)> receiveQueue = new Queue<(byte[] data, IPEndPoint sender)>();
            private readonly object gate = new object();

            public List<(byte[] data, int length, IPEndPoint endpoint)> SentPackets { get; } = new List<(byte[] data, int length, IPEndPoint endpoint)>();

            public bool IsBound { get; private set; }

            public void Bind()
            {
                IsBound = true;
            }

            public void Bind(int port)
            {
                IsBound = true;
            }

            public void Send(byte[] data, IPEndPoint endpoint)
            {
                Send(data, data.Length, endpoint);
            }

            public void Send(byte[] data, int length, IPEndPoint endpoint)
            {
                if (data == null) throw new ArgumentNullException(nameof(data));
                if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
                if (length < 0 || length > data.Length) throw new ArgumentOutOfRangeException(nameof(length));

                var copy = new byte[length];
                Buffer.BlockCopy(data, 0, copy, 0, length);
                lock (gate)
                {
                    SentPackets.Add((copy, length, endpoint));
                }
            }

            public Task SendAsync(byte[] data, IPEndPoint endpoint)
            {
                Send(data, endpoint);
                return Task.CompletedTask;
            }

            public Task SendAsync(byte[] data, int length, IPEndPoint endpoint)
            {
                Send(data, length, endpoint);
                return Task.CompletedTask;
            }

            public (byte[] data, IPEndPoint sender) Receive()
            {
                lock (gate)
                {
                    if (receiveQueue.Count == 0)
                    {
                        return (Array.Empty<byte>(), new IPEndPoint(IPAddress.Loopback, 0));
                    }

                    return receiveQueue.Dequeue();
                }
            }

            public Task<(byte[] data, IPEndPoint sender)> ReceiveAsync()
            {
                return Task.FromResult(Receive());
            }

            public void EnqueueInbound(byte[] data, IPEndPoint sender)
            {
                lock (gate)
                {
                    receiveQueue.Enqueue((data, sender));
                }
            }

            public void Dispose()
            {
            }
        }

        [Test]
        public async Task MessageReceived_EventIsFired_ThroughDispatch()
        {
            var session = await NetworkSession.CreateAsync(NetworkSessionType.Local, 1, 4, 0, new Dictionary<string, object>());
            var tcs = new TaskCompletionSource<MessageReceivedEventArgs>();

            session.MessageReceived += (sender, args) =>
            {
                tcs.TrySetResult(args);
            };

            session.DispatchIncomingMessage(new MessageReceivedEventArgs(new HeartbeatMessage
            {
                GamerId = "remote",
                SequenceNumber = 1,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, null));

            var result = await tcs.Task;
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<HeartbeatMessage>(result.Message);

            await session.DisposeAsync();
        }

        [Test]
        public async Task HostProcessesJoinRequest_AndSendsAcceptance()
        {
            var hostSession = await NetworkSession.CreateAsync(NetworkSessionType.Local, 1, 4, 0, new Dictionary<string, object>());
            var transport = new FakeNetworkTransport();
            hostSession.NetworkTransport = transport;

            var joinRequest = new JoinRequestMessage
            {
                GamerId = "Player2",
                Gamertag = "PlayerTwo"
            };

            var remoteEndpoint = new IPEndPoint(IPAddress.Loopback, 12345);
            hostSession.DispatchIncomingMessage(new MessageReceivedEventArgs(joinRequest, remoteEndpoint));

            Assert.IsTrue(hostSession.AllGamers.Any(g => g.Gamertag == "PlayerTwo"));
            Assert.That(transport.SentPackets.Count, Is.EqualTo(1));

            await hostSession.DisposeAsync();
        }

        [Test]
        public async Task SendToAll_UsesFakeTransportForRemoteGamers()
        {
            var session = await NetworkSession.CreateAsync(NetworkSessionType.Local, 1, 4, 0, new Dictionary<string, object>());
            var transport = new FakeNetworkTransport();
            session.NetworkTransport = transport;

            var remote = new NetworkGamer(session, "remote-1", false, false, "RemoteOne");
            session.AcceptGamer(remote);
            session.RegisterGamerEndpoint(remote, new IPEndPoint(IPAddress.Loopback, 54321));

            var writer = new PacketWriter();
            writer.Write("payload");

            session.SendToAll(writer, SendDataOptions.Reliable);

            Assert.That(transport.SentPackets.Count, Is.EqualTo(1));
            Assert.That(transport.SentPackets[0].length, Is.GreaterThan(0));

            await session.DisposeAsync();
        }

        [Test]
        public async Task DisposeAsync_CleansUpResources()
        {
            var session = await NetworkSession.CreateAsync(NetworkSessionType.Local, 1, 4, 0, new Dictionary<string, object>());
            Assert.DoesNotThrowAsync(async () => await session.DisposeAsync());
        }
    }
}