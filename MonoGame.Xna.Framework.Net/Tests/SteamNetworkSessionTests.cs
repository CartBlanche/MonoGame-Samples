using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
using NUnit.Framework;

namespace Microsoft.Xna.Framework.Net.Tests
{
    [TestFixture]
    public class SteamNetworkSessionTests
    {
        private const byte TestMessageType = 240;

        private sealed class TestReliableMessage : INetworkMessage
        {
            public byte MessageType => TestMessageType;

            public string Payload { get; set; }

            public void Serialize(PacketWriter writer)
            {
                writer.Write(Payload ?? string.Empty);
            }

            public void Deserialize(PacketReader reader)
            {
                Payload = reader.ReadString();
            }
        }

        [SetUp]
        public void Setup()
        {
            NetworkMessageRegistry.Register<TestReliableMessage>(TestMessageType);
        }

        [Test]
        public async Task SteamFactory_HostJoinAndReliableMessage_EndToEnd()
        {
            var factory = new SteamNetworkSessionFactory();
            var host = factory.CreateSession();
            var client = factory.CreateSession();
            var hostStartedCount = 0;
            var clientStartedCount = 0;

            host.GameStarted += (_, __) => hostStartedCount++;
            client.GameStarted += (_, __) => clientStartedCount++;

            try
            {
                await host.CreateAsync(NetworkSessionType.PlayerMatch, maxGamers: 4, privateGamerSlots: 0);

                var sessions = (await factory.FindSessionsAsync(NetworkSessionType.PlayerMatch)).ToList();
                Assert.That(sessions.Count, Is.EqualTo(1));

                await client.JoinAsync(sessions[0].JoinAddress);

                Assert.That(host.AllGamers.Count, Is.EqualTo(2));
                Assert.That(client.AllGamers.Count, Is.EqualTo(2));
                Assert.That(host.State, Is.EqualTo(NetworkSessionState.Playing));
                Assert.That(client.State, Is.EqualTo(NetworkSessionState.Playing));
                Assert.That(hostStartedCount, Is.EqualTo(1));
                Assert.That(clientStartedCount, Is.EqualTo(1));

                var receivedPayload = (string)null;
                client.MessageReceived += (sender, args) =>
                {
                    if (args.Message is TestReliableMessage testMessage)
                    {
                        receivedPayload = testMessage.Payload;
                    }
                };

                host.BroadcastMessage(new TestReliableMessage { Payload = "steam-e2e" });
                client.Update(new GameTime());

                Assert.That(receivedPayload, Is.EqualTo("steam-e2e"));
            }
            finally
            {
                await client.CloseAsync();
                await host.CloseAsync();
            }
        }
    }
}
