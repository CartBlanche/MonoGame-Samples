using System;
using System.Collections.Generic;

namespace Gorillas
{
	public static class GamePreferrences
	{
		static int lives = 3;
		static List<string> playerNames = new List<string>();
		
		static int NumberOfLives
		{
			get { return lives; }
			set { lives = value; }
		}
		
		
	}
}

