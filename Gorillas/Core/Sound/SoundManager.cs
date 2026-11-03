using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Gorillas
{
	public static class SoundManager
	{
		private static List<SoundEffect> explosions = new
		List<SoundEffect> ();
		private static int explosionCount = 5;
		private static SoundEffect playerShot;
		private static SoundEffect enemyShot;
		private static SoundEffect gorillaExplode;
		private static SoundEffect gorillaWon;
		private static SoundEffect bank;
		private static List<SoundEffect> gamePlay = new List<SoundEffect> ();
		private static int gamePlayCount = 2;		
		private static Random rand = new Random ();
		

		public static void Initialize (ContentManager content)
		{
			try {
				//playerShot = content.Load<SoundEffect> ("Sounds/Shot1");
				playerShot = content.Load<SoundEffect> ("Sounds/Throw2");
				//enemyShot = content.Load<SoundEffect> ("Sounds/Shot2");
				enemyShot = content.Load<SoundEffect> ("Sounds/Throw2");
				gorillaExplode = content.Load<SoundEffect> ("Sounds/ExplodeGorilla");
				gorillaWon = content.Load<SoundEffect> ("Sounds/GorillaWon");
				//bank = content.Load<SoundEffect> ("Sounds/Slipstream");
				for (int x = 1; x <= explosionCount; x++) {
					explosions.Add (content.Load<SoundEffect> ("Sounds/Explosion" + x.ToString ()));
				}
//				for (int x = 1; x <= gamePlayCount; x++) {
//					gamePlay.Add (content.Load<SoundEffect> ("Sounds/Crisis" + x.ToString ()));
//				}
			} catch {
				Debug.Write ("SoundManager Initialization Failed");
			}
		}

		public static void PlayExplosion ()
		{
			try {
				int which = rand.Next (0, explosionCount);
				//Console.WriteLine("sound : " + which);
				explosions [which].Play ();
			} catch {
				Debug.Write ("PlayExplosion Failed");
			}
		}

		public static void PlayPlayerShot ()
		{
			try {
				playerShot.Play ();
			} catch {
				Debug.Write ("PlayPlayerShot Failed");
			}
		}

		public static void PlayEnemyShot ()
		{
			try {
				enemyShot.Play ();
			} catch {
				Debug.Write ("PlayEnemyShot Failed");
			}
		}
		
		public static void GorillaExplode ()
		{
			try {
				gorillaExplode.Play ();
			} catch {
				Debug.Write ("GorillaExplode Failed");
			}
		}
		
		public static void GorillaWon ()
		{
			try {
				gorillaWon.Play ();
			} catch {
				Debug.Write ("GorillaExplode Failed");
			}
		}	

		public static void GameStart ()
		{
			try {
				int which = rand.Next (0, gamePlayCount + 1);
				//Console.WriteLine("sound : " + which);
				//gamePlay [0].Play ();
			} catch {
				Debug.Write ("GameStart Failed");
			}
		}	
	}
}
