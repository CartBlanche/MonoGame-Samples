using System.Threading.Tasks;
using Microsoft.Xna.Framework.GamerServices;
using NUnit.Framework;

namespace Microsoft.Xna.Framework.Net.Tests
{
    [TestFixture]
    public class GuideMessageBoxTests
    {
        [Test]
        public async Task ShowMessageBoxAsync_WithValidFocusButton_ReturnsFocusedButton()
        {
            var selected = await Guide.ShowMessageBoxAsync(
                title: "Confirm",
                text: "Proceed?",
                buttons: new[] { "No", "Yes" },
                focusButton: 1,
                icon: MessageBoxIcon.None);

            Assert.That(selected, Is.EqualTo(1));
        }

        [Test]
        public async Task ShowMessageBoxAsync_WithOutOfRangeFocusButton_DefaultsToFirstButton()
        {
            var selected = await Guide.ShowMessageBoxAsync(
                title: "Confirm",
                text: "Proceed?",
                buttons: new[] { "No", "Yes" },
                focusButton: 99,
                icon: MessageBoxIcon.None);

            Assert.That(selected, Is.EqualTo(0));
        }

        [Test]
        public async Task ShowMessageBoxAsync_WithNoButtons_ReturnsNull()
        {
            var selected = await Guide.ShowMessageBoxAsync(
                title: "Info",
                text: "Nothing to choose.",
                buttons: new string[0],
                focusButton: 0,
                icon: MessageBoxIcon.Alert);

            Assert.That(selected, Is.Null);
        }
    }
}
