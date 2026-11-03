using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Gorillas
{
	public class Gorilla : Player
	{
		
		bool isAI = false;
		bool isRemote = false;
		static Random random = new Random((int)DateTime.Now.Ticks);
		
		internal static Color[] gorillaData;
		
		Banana banana;
		
		public enum GorillaPlayerClass : int 
		{
			Player1 = 1,
			Player2 = 2,
		}
		
		GorillaPlayerClass playerClass;
		
		public Gorilla (ContentManager manager, GorillaPlayerClass playerClass, Banana banana, bool isAI, bool isRemote)
			: base(manager)
		{

			this.playerClass = playerClass;
			this.banana = banana;
			this.isAI = isAI;
			this.isRemote = isRemote;
			
			if (gorillaData == null) {
				Texture2D tex = animations["idle"].Texture;
				gorillaData = new Color[tex.Width * tex.Height];
				tex.GetData(gorillaData);
			}
		}
		
		public Gorilla (ContentManager manager, GorillaPlayerClass playerClass, Banana banana, bool isAI)
			: this(manager, playerClass, banana, isAI, false)
		{

			this.playerClass = playerClass;
			this.banana = banana;
			this.isAI = isAI;
		}
		
		public Gorilla (ContentManager manager, GorillaPlayerClass playerClass, Banana banana)
			: this(manager, playerClass, banana, false, false)
		{}
		
		
		public GorillaPlayerClass PlayerClass
		{
			get { return playerClass; }
		}
		
		public Vector2 EyeSight 
		{
			get {
				Vector2 eye = WorldLocation;
				eye.X += Width / 2;
				return eye;
			}
		}
		
		bool hasBanana = false;
		public void DropBanana() 
		{
			hasBanana = false;
			banana.Visible = false;
			PlayAnimation("idle");
			//Console.WriteLine("drop: " + Name);
		}

		public void GrabBanana()
		{
			
			Vector2 pos = Position;
			pos.X += Width / 2;
			pos.Y -= 10;
			
			banana.Visible = true;
			hasBanana = true;
			banana.ResetBanana();
			banana.BounceMe(pos);
			//Console.WriteLine("Grab: " + Name);
			
		}
		
		public void ThrowBanana() 
		{
			Vector2 pos = Position;
			pos.X += Width / 2;
			pos.Y -= 10;
			banana.WorldLocation = pos;
			banana.ThrowMe(Angle, Velocity);
			if ((int)playerClass % 2 == 0) {
				PlayAnimation("idleLeftUp");
				SoundManager.PlayEnemyShot();
			}
			else {
				PlayAnimation("idleRightUp");
				SoundManager.PlayEnemyShot();
			}
		}
		
		public bool HasBanana
		{
			
			get { return hasBanana; }
		}
		
		public Vector2 Position 
		{
			get { return WorldLocation; }
			set { WorldLocation = value; }
		}
		
		public float Angle { get; set; }
		
		public float Velocity { get; set; }

		public string Name { get; set; }
		
		public void DieGorillaDie () 
		{
			Kill();
		}
		
		
		public void GorillaWon () 
		{
			velocity = Vector2.Zero;
			initialPosition = worldLocation;
			//SoundManager.GorillaWon();
			switch (random.Next(1,4)) {
			case 1:
				velocity = new Vector2(0,1);
				PlayAnimation("victory");				
				break;
			case 2:
				PlayAnimation("winLeftUp");
				break;
			case 3:
				PlayAnimation("winLeftCheek");
				break;
			}
			
			Score++;
		}
		
		Vector2 trajectory = Vector2.Zero;
		
		public Vector2 AITrajectory
		{
			get { return trajectory; }
			set { trajectory = value; }
		}
		public override void Revive() 
		{
			trajectory = Vector2.Zero;
			Angle = 0;
			Velocity = 0;
			PlayAnimation("idle");			
			base.Revive();
		}
		
		public bool IsAI 
		{
			get { return isAI; }
			set { isAI = value; }
		}
		
		public bool IsRemote 
		{
			get { return isRemote; }
			set { isRemote = value; }
		}		
	}
}



