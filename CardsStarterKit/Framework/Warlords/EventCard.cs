//-----------------------------------------------------------------------------
// EventCard.cs
//
// Represents a one-time event card
//-----------------------------------------------------------------------------

namespace WarlordsFramework
{
    /// <summary>
    /// Event cards are one-time effects that go to The Void after use
    /// </summary>
    public class EventCard : WarlordsCard
    {
        // Future expansion - actual effect (null for minimal prototype)
        public object Effect { get; set; }
        
        public EventCard()
        {
        }
    }
}
