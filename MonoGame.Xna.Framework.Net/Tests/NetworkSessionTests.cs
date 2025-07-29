using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Net;
using System.Collections.Generic;
using System;

namespace Microsoft.Xna.Framework.Net.Tests
{
    [TestFixture]
    public class NetworkSessionTests
    {
        [Test]
        public async Task MessageReceived_EventIsFired()
        {
            var session = await NetworkSession.CreateAsync(NetworkSessionType.SystemLink, 1, 4, 0, new Dictionary<string, object>());
            var tcs = new TaskCompletionSource<MessageReceivedEventArgs>();

            session.MessageReceived += (sender, args) =>
            {
                tcs.TrySetResult(args);
            };

            // Simulate a message being received (this requires a real or mock transport)
            // For demonstration, we'll invoke the event manually:
            session.GetType().GetMethod("OnMessageReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(session, new object[] { new MessageReceivedEventArgs(null, null) });

            var result = await tcs.Task;
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task GamerJoined_EventIsFired()
        {
            var session = await NetworkSession.CreateAsync(NetworkSessionType.SystemLink, 1, 4, 0, new Dictionary<string, object>());
            var tcs = new TaskCompletionSource<GamerJoinedEventArgs>();

            session.GamerJoined += (sender, args) =>
            {
                tcs.TrySetResult(args);
            };

            // Simulate a gamer joining
            var gamerType = typeof(NetworkGamer);
            var gamer = (NetworkGamer)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(gamerType);
            session.GetType().GetMethod("OnGamerJoined", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(session, new object[] { gamer });

            var result = await tcs.Task;
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task SessionEnded_EventIsFired()
        {
            var session = await NetworkSession.CreateAsync(NetworkSessionType.SystemLink, 1, 4, 0, new Dictionary<string, object>());
            var tcs = new TaskCompletionSource<NetworkSessionEndedEventArgs>();

            session.SessionEnded += (sender, args) =>
            {
                tcs.TrySetResult(args);
            };

            // Simulate session ending
            var endReasonType = typeof(NetworkSessionEndReason);
            var reason = Enum.GetValues(endReasonType).GetValue(0);
            session.GetType().GetMethod("OnSessionEnded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(session, new object[] { reason });

            var result = await tcs.Task;
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task CreateAsync_CreatesSessionSuccessfully()
        {
            var session = await NetworkSession.CreateAsync(NetworkSessionType.SystemLink, 1, 4, 0, new Dictionary<string, object>());
            Assert.IsNotNull(session);
            Assert.AreEqual(NetworkSessionState.Lobby, session.SessionState);
            Assert.AreEqual(4, session.MaxGamers);
        }

        [Test]
        public void Create_Synchronous_CreatesSessionSuccessfully()
        {
            var session = NetworkSession.Create(NetworkSessionType.SystemLink, 1, 4, 0, new Dictionary<string, object>());
            Assert.IsNotNull(session);
            Assert.AreEqual(NetworkSessionState.Lobby, session.SessionState);
            Assert.AreEqual(4, session.MaxGamers);
        }

        [Test]
        public void Cancel_CancelsSessionWithoutException()
        {
            var session = NetworkSession.Create(NetworkSessionType.SystemLink, 1, 4, 0, new Dictionary<string, object>());
            Assert.DoesNotThrow(() => session.Cancel());
        }

        [Test]
        public async Task DisposeAsync_CleansUpResources()
        {
            var session = await NetworkSession.CreateAsync(NetworkSessionType.SystemLink, 1, 4, 0, new Dictionary<string, object>());
            await session.DisposeAsync();
            Assert.Pass(); // If no exception, disposal succeeded
        }

        [Test]
        public async Task CreateFindJoin_SimulatesSessionLifecycle()
        {
            // Create host session
            var hostSession = await NetworkSession.CreateAsync(NetworkSessionType.SystemLink, 1, 4, 0, new Dictionary<string, object>());
            Assert.IsNotNull(hostSession);
            Assert.AreEqual(NetworkSessionState.Lobby, hostSession.SessionState);

            // Simulate finding available sessions (mocked)
            var foundSessions = await NetworkSession.FindAsync(NetworkSessionType.SystemLink, 1, new Dictionary<string, object>());
            Assert.IsNotNull(foundSessions);

            // Simulate joining the session as a client
            var availableSession = new AvailableNetworkSession(
                sessionName: "TestSession",
                hostGamertag: "HostPlayer",
                currentGamerCount: 1,
                openPublicGamerSlots: 3,
                openPrivateGamerSlots: 0,
                sessionType: NetworkSessionType.SystemLink,
                sessionProperties: new Dictionary<string, object>(),
                sessionId: "test-session-id");
            var clientSession = await NetworkSession.JoinAsync(availableSession);
            Assert.IsNotNull(clientSession);
            Assert.AreEqual(NetworkSessionState.Lobby, clientSession.SessionState);

            // Clean up
            await hostSession.DisposeAsync();
            await clientSession.DisposeAsync();
        }
    }
}