namespace Microsoft.Xna.Framework.GamerServices
{
	/// <summary>
	/// Collection of gamers.
	/// </summary>
	public class GamerCollection<T> : System.Collections.ObjectModel.ReadOnlyCollection<T>
    {
        public GamerCollection(IList<T> list) : base(list) { }
    }
}
