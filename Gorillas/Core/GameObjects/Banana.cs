using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

using Tile_Engine;


namespace Gorillas
{
	public class Banana : GameObject
	{
		
		GraphicsDevice graphics;
		bool beingThrown = false;
		
		#region Constructor
		public Banana (ContentManager content, GameplayScreen  game) {
			

			graphics  = game.ScreenManager.GraphicsDevice;
			
			animations.Add ("idle", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Banana/BananaIdle"),
					4,
					"idle"));
			animations ["idle"].LoopAnimation = true;

			animations.Add ("throw", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Banana/BananaIdle"),
					4,
					"throw"));
			animations ["throw"].LoopAnimation = true;
			
			animations.Add ("bounce", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Banana/BananaIdle"),
					4,
					"bounce"));
			animations ["bounce"].LoopAnimation = true;
			
			frameWidth = 4;
			frameHeight = 9;
			CollisionRectangle = new Rectangle (1, 1, 4, 9);

			drawDepth = 0.925f;

			enabled = true;
			codeBasedBlocks = false;
			PlayAnimation ("idle");
			
			RotationOrigin = Center;
		}
		
		#endregion
		float time = 0;
		public override void Update (GameTime gameTime)
		{
			PositionColor = Color.Transparent;
			if (!enabled)
				return;

			float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
			
			updateAnimation (gameTime);

			Vector2 newPosition = worldLocation;

			if (currentAnimation == "idle" ||
				currentAnimation == "throw") {
//				if (velocity.Y != 0) {
//					onGround = false;
//				}
	
	//			Vector2 newPosition = worldLocation; // + moveAmount;
	//
	//			newPosition = new Vector2 (
	//					MathHelper.Clamp (newPosition.X, 0, 
	//					Camera.WorldRectangle.Width - frameWidth),
	//					MathHelper.Clamp (newPosition.Y, 2 * (-TileMap.TileHeight), 
	//					Camera.WorldRectangle.Height - frameHeight));
	//
	//			worldLocation = newPosition;

				time += elapsed;
				
				float time2 = time * 3.0f;

				if (beingThrown) {
					
					// calculate the bananas next position
					// (vA-vB).Normalize() gives you a direction vector looking from A to B.					
					newPosition = initialPosition - (((initialVelocity * time2)
					 + (0.5f * acceleration * time2 * time2)));
					//Console.WriteLine("Update Banana " + worldLocation);
					// rotate the banana
					switch ((int)(time * 10) % 4) {
					case 0:
						Rotation = MathHelper.ToRadians(0);
						break;
					case 1:
						Rotation = MathHelper.ToRadians(90);
						break;
					case 2:
						Rotation = MathHelper.ToRadians(180);
						break;
					case 3:
						Rotation = MathHelper.ToRadians(270);
						break;
					}

				}
			}
			
			if (currentAnimation == "bounce") {
				
				if (velocity.Y == 0 || acceleration.Y == 0 || elapsed <= 0) {
					Console.WriteLine("vel: " + velocity + " acc: " + acceleration 
						+ " elapsed " + elapsed);
				}
				Vector2 moveAmount = velocity * acceleration * elapsed ;
	
				newPosition = worldLocation + moveAmount;
				
				// Start: Hopefully this will stop the banana from getting stuck
				if (newPosition.Y > initialPosition.Y)
					newPosition.Y = initialPosition.Y;
				
				if (Vector2.Distance(initialPosition,newPosition) >= 25)
					newPosition = initialPosition;
				// End: Hopefully this will stop the banana from getting stuck
				
				if (Vector2.Distance(initialPosition,newPosition) >= 20 || 
					newPosition.Y >= initialPosition.Y)
					velocity *= -1;
				
			}
			
			
			worldLocation = newPosition;
			
//			if (beingThrown) {
//				// Get the background color of the new position
//				//  this is used for collision detection
//				//Rectangle bounds = ((Engine)Game).cityScape.renderTarget.Bounds;
//				int newY = graphics.PreferredBackBufferHeight - (int)worldLocation.Y;
//				byte[] buffer = new byte[4];
//				int pos = 0;
//				MonoMac.OpenGL.GL.ReadPixels((int)worldLocation.X,newY,1,1,
//					MonoMac.OpenGL.PixelFormat.Rgba,MonoMac.OpenGL.PixelType.UnsignedByte,buffer);
//				PositionColor = new Color(buffer[pos],buffer[pos+1],buffer[pos+2],buffer[pos+3]);
//			}			
			//base.Update (gameTime);
		}
		
		public override void Draw (SpriteBatch spriteBatch)
		{
			// This is here for now because of the text being drawn.  If done in update the text is also 
			//  being hit.
			//  The text needs to be redone.
			// Collision against the city is done on the CPU in CityScape.CheckBuildingCollision
			// (cityscapeData), so there is no need to read pixels back from the frame buffer.

			base.Draw (spriteBatch);
				
		}
		public void ResetBanana () 
		{
			Rotation = 0;
			beingThrown = false;
		}
		
		public bool BeingThrown 
		{
			get { return beingThrown; }
		}		
		
		internal Color PositionColor = Color.Transparent;
		
		Vector2 initialPosition = Vector2.Zero;
		Vector2 initialVelocity = Vector2.Zero;
		Vector2 acceleration = new Vector2 (0, -9.8f);
		
		float angle = 0;
		
		public void ThrowMe (float angle, float velocity)
		{

			angle = 180 - angle;
			
			this.velocity.Y = velocity;
			this.angle = MathHelper.ToRadians (angle);

			initialVelocity = new Vector2 ((float)Math.Cos (this.angle), (float)Math.Sin (this.angle)) * this.velocity.Y;
			initialPosition = worldLocation;
			acceleration = new Vector2 (0, -9.8f);
			acceleration.X = -WindManager.Wind / 3f; // set the wind speed (float: integer division made winds of 1-2 do nothing)
			
			beingThrown = true;
			time = 0;
			PlayAnimation("throw");

		}

		public void BounceMe (Vector2 startPosition)
		{
			worldLocation = startPosition;
			initialPosition = worldLocation;
			velocity = new Vector2(0,1);
			acceleration = new Vector2(0,-20f);
			Rotation = 0;
			PlayAnimation("bounce");
		}		

		public void GuidePosition (float angle, float velocity, Vector2 eyeSight)
		{
			PlayAnimation("idle");
			float radians = MathHelper.ToRadians(angle);
			worldLocation.X = eyeSight.X + (float)Math.Cos(radians) * velocity;
			worldLocation.Y = eyeSight.Y + (float)Math.Sin(radians) * velocity;
		}		
		
	}
}

