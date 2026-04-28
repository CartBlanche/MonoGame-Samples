using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.GamerServices;
using NUnit.Framework;

namespace Microsoft.Xna.Framework.Net.Tests
{
    [TestFixture]
    public class GuideSignInTests
    {
        private IGuideSignInProvider originalProvider;

        [SetUp]
        public void SetUp()
        {
            originalProvider = Guide.SignInProvider;
            SignedInGamer.Current.SetSignedInToLive(false);
        }

        [TearDown]
        public void TearDown()
        {
            Guide.SignInProvider = originalProvider;
            SignedInGamer.Current.SetSignedInToLive(false);
        }

        [Test]
        public void ShowSignIn_WhenProviderSucceeds_SetsSignedInToLiveTrue()
        {
            Guide.SignInProvider = new StubSignInProvider(true);

            Guide.ShowSignIn(1, onlineOnly: false);

            Assert.That(SignedInGamer.Current.IsSignedInToLive, Is.True);
        }

        [Test]
        public void ShowSignIn_WhenProviderReturnsFalse_StaysSignedOut()
        {
            Guide.SignInProvider = new StubSignInProvider(false);

            Guide.ShowSignIn(1, onlineOnly: true);

            Assert.That(SignedInGamer.Current.IsSignedInToLive, Is.False);
        }

        [Test]
        public async Task ShowSignInAsync_UsesProviderResult()
        {
            Guide.SignInProvider = new StubSignInProvider(true);

            await Guide.ShowSignInAsync(1, onlineOnly: false);

            Assert.That(SignedInGamer.Current.IsSignedInToLive, Is.True);
        }

        private sealed class StubSignInProvider : IGuideSignInProvider
        {
            private readonly bool result;

            public StubSignInProvider(bool result)
            {
                this.result = result;
            }

            public Task<bool> ShowSignInAsync(int paneCount, bool onlineOnly, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(result);
            }
        }
    }
}
