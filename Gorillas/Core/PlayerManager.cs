using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Gorillas
{
	public class PlayerManager
	{
		//Gorilla gorilla1;
		//Gorilla gorilla2;
		Banana banana;
		HeadsUpTrajectory headsUp;
		GameplayScreen game;
		
		internal List<Gorilla> players = new List<Gorilla>();
		
		public PlayerManager (GameplayScreen game, Banana banana, HeadsUpTrajectory headsUp)
		{
			this.game = game;	
			this.banana = banana;
			this.headsUp = headsUp;
		}
		
		public void AddGorilla(Gorilla gorilla)
		{
			players.Add(gorilla);
		}
		
		public Gorilla GorillaAt(int index) 
		{
			if (index > players.Count -1)
				return null;
			return players[index];
		}
		
		public Gorilla GorillaNext (Gorilla gorilla) 
		{
			int index = players.IndexOf(gorilla);
			if (index >= players.Count -1)
				return players[0];
			return players[index+1];
		}
		
		public void Reset() 
		{
			players.Clear();
		}
		
		public void SwitchTurns ()
		{
			int index = -1;
			for (int x = 0; x < players.Count; x++) {
				if (players[x].HasBanana) {
					index = x + 1;
					players[x].DropBanana();
					break;
				}
			}
			
			if (index < 0 || index >= players.Count) {
				index = 0;
			}
			
			players[index].GrabBanana();
			headsUp.Gorilla = players[index];
		}
		
		
		public void KillOffGorilla (ObjectCollisionEnum whichGorilla)
		{
			if (whichGorilla == ObjectCollisionEnum.Gorilla1) {
				players[0].DieGorillaDie();
				players[1].GorillaWon();
			}
			else {
				players[1].DieGorillaDie();
				players[0].GorillaWon();

			}
			
			banana.Visible = false;
		}
		
		public int ScoreOfPlayer1 
		{
			get { return players[0].Score; }
		}
		
		public int ScoreOfPlayer2 
		{
			get { return players[1].Score; }
		}
		
		public void RevivePlayers ()
		{
			foreach (Gorilla mono in players) {
				mono.Revive();
				if (mono.IsAI) {
					Vector2 traj = Vector2.Zero;
					mono.AITrajectory = traj;
					Console.WriteLine(traj);
				}
			}
		}
		
		public void NewGame (int lives) 
		{
			foreach (Gorilla mono in players) {
				mono.Revive();
				mono.LivesRemaining = lives;
				mono.Score = 0;
			}
			
		}
		
	}
}

