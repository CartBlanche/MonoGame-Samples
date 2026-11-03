using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Tile_Engine;

namespace Gorillas
{
	public class Player : GameObject
	{
		private Vector2 fallSpeed = new Vector2 (0, 0);
		private float moveScale = 180.0f;
		private bool dead = false;
		private int score = 0;
		private int livesRemaining = 3;

		public bool Dead {
			get { return dead; }
		}

		public int Score {
			get { return score; }
			set { score = value; }
		}

		public int LivesRemaining {
			get { return livesRemaining; }
			set { livesRemaining = value; }
		}

	#region Constructor
		public Player (ContentManager content)
			{
			animations.Add ("idle", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaArmsDown"),
					28,
					"idle"));
					animations ["idle"].LoopAnimation = true;

			animations.Add ("idleLeftUp", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaLeftUp"),
					28,
					"idleLeftUp"));
					animations ["idleLeftUp"].LoopAnimation = true;

			animations.Add ("winLeftUp", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaLeftUp"),
					28,
					"winLeftUp"));
					animations ["winLeftUp"].LoopAnimation = false;
					animations ["winLeftUp"].FrameLength = 0.5f;
					animations ["winLeftUp"].NextAnimation = "winRightUp";
			
			animations.Add ("winLeftCheek", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaButtLeft"),
					28,
					"winLeftCheek"));
					animations ["winLeftCheek"].LoopAnimation = false;
					animations ["winLeftCheek"].FrameLength = 0.5f;
					animations ["winLeftCheek"].NextAnimation = "winRightCheek";			

			animations.Add ("idleRightUp", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaRightUp"),
					28,
					"idleRightUp"));
					animations ["idleRightUp"].LoopAnimation = true;
		//			animations ["jump"].LoopAnimation = false;
		//			animations ["jump"].FrameLength = 0.08f;
		//			animations ["jump"].NextAnimation = "idle";
			
			animations.Add ("winRightUp", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaRightUp"),
					28,
					"winRightUp"));
					animations ["winRightUp"].LoopAnimation = false;
					animations ["winRightUp"].FrameLength = 0.5f;
					animations ["winRightUp"].NextAnimation = "winLeftUp";

			animations.Add ("winRightCheek", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaButtRight"),
					28,
					"winRightCheek"));
					animations ["winRightCheek"].LoopAnimation = false;
					animations ["winRightCheek"].FrameLength = 0.5f;
					animations ["winRightCheek"].NextAnimation = "winLeftCheek";
			
			animations.Add ("victory", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaVictory"),
					28,
					"victory"));
					animations ["victory"].LoopAnimation = false;

			animations.Add ("die", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Gorilla/GorillaExplode"),
					32,
					"die"));
					animations ["die"].LoopAnimation = false;

			frameWidth = 28;
			frameHeight = 29;
			CollisionRectangle = new Rectangle (5, 1, 23, 29);

			drawDepth = 0.825f;

			enabled = true;
			codeBasedBlocks = false;
			PlayAnimation ("idle");
		}

	#endregion

	#region Public Methods
			//velocity = new Vector2(0,1);
		Vector2 acceleration = new Vector2(0,-40f);
		protected Vector2 initialPosition = Vector2.Zero;
		
		public override void Update (GameTime gameTime)
		{
			if (!Dead) {
				float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;				
				//string newAnimation = "idle";
				if (currentAnimation == "victory") {
				
					Vector2 moveAmount = velocity * elapsed * acceleration;
		
					Vector2 newPosition = worldLocation + moveAmount;
					
					if (Vector2.Distance(initialPosition,newPosition) > 20 || 
						newPosition.Y > initialPosition.Y)
						velocity *= -1;
					worldLocation = newPosition;
				}
				else {
					velocity = Vector2.Zero;//new Vector2 (0, velocity.Y);
				}
//				GamePadState gamePad = GamePad.GetState (PlayerIndex.One);
//				KeyboardState keyState = Keyboard.GetState ();
//
//				if (keyState.IsKeyDown (Keys.Left) || 
//			(gamePad.ThumbSticks.Left.X < -0.3f)) {
//					flipped = false;
//					newAnimation = "run";
//					velocity = new Vector2 (-moveScale, velocity.Y);
//				}
//
//				if (keyState.IsKeyDown (Keys.Right) || 
//			(gamePad.ThumbSticks.Left.X > 0.3f)) {
//					flipped = true;
//					newAnimation = "run";
//					velocity = new Vector2 (moveScale, velocity.Y);
//				}
//
//				if (keyState.IsKeyDown (Keys.Space) || 
//			(gamePad.Buttons.A == ButtonState.Pressed)) {
//					if (onGround) {
//						Jump ();
//						newAnimation = "jump";
//					}
//				}

//				if (keyState.IsKeyDown (Keys.Up) || 
//			gamePad.ThumbSticks.Left.Y > 0.3f) {
//					checkLevelTransition ();
//				}


//				if (currentAnimation == "jump")
//					newAnimation = "jump";

				//if (newAnimation != currentAnimation) {
				//	PlayAnimation (newAnimation);
				//}
				
			}
			
			if (currentAnimation == "die") {
				frameWidth = 64;
				frameHeight = 64;				
			}
			else {
				frameWidth = 28;
				frameHeight = 29;				
			}
			
				
			//velocity += fallSpeed;

			repositionCamera ();
			base.Update (gameTime);
		}

		public void Jump ()
		{
			velocity.Y = -500;
		}

		public void Kill ()
		{
			// offset for explosion
			Vector2 wl = WorldLocation;
			wl.X -=20;
			wl.Y -=20;
			WorldLocation = wl;
			PlayAnimation ("die");
			SoundManager.GorillaExplode();
			LivesRemaining--;
			velocity.X = 0;
			dead = true;
		}

		public virtual void Revive ()
		{
			PlayAnimation ("idle");
			velocity = new Vector2 (0, 0);
			dead = false;
		}

	#endregion

	#region Helper Methods
		private void repositionCamera ()
		{
			int screenLocX = (int)Camera.WorldToScreen (worldLocation).X;

			if (screenLocX > 500) {
				Camera.Move (new Vector2 (screenLocX - 500, 0));
			}

			if (screenLocX < 200) {
				Camera.Move (new Vector2 (screenLocX - 200, 0));
			}
		}

	#endregion


	}
}
