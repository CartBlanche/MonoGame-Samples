using System;

namespace Gorillas
{
	public static class WindManager
	{
		static int wind = 0;
		static Random random = new Random ((int)DateTime.Now.Ticks);		

		internal static void CalculateWindSpeed ()
		{
			wind = random.Next (11) - 5;

			if (random.Next (3) == 1) {
				if (wind > 0)
					wind += random.Next (11);
				else
					wind -= random.Next (11);
			}
		}
		
		public static int Wind 
		{
			get { return wind; }
			set { wind = value; }
		}
	}
}

