//-----------------------------------------------------------------------------
// BlackjackAboutScreen.cs
//
// Blackjack-specific about screen content.
//-----------------------------------------------------------------------------

using CardsFramework.Core;

namespace Blackjack
{
    class BlackjackAboutScreen : AboutScreenBase
    {
        public BlackjackAboutScreen() : this(null)
        {
        }

        public BlackjackAboutScreen(string title) : base(title)
        {
        }

        protected override string[] ContentLines => new[]
        {
            "MonoGame Blackjack",
            "",
            GetVersionLine(),
            "",
            "Based on Microsoft XNA Card Game Starter Kit",
            "Modernized for MonoGame",
            "",
            "Features:",
            "- Cross-platform gameplay",
            "- Multiple languages supported",
            "- Customizable settings",
            "- NPC opponents",
            "",
            "Thanks:",
            "- Pixabay for Jazz Music, CardRemoval and Winning Sound effects",
            "- gnokii and openclipart.org for this game's Icon",
            "",
            "Built with MonoGame",
            "www.monogame.net"
        };
    }
}