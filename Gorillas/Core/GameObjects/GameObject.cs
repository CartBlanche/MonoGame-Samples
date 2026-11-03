using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tile_Engine;

namespace Gorillas
{
	public class GameObject
	{
	#region Declarations
		protected Vector2 worldLocation;
		protected Vector2 velocity;
		protected int frameWidth;
		protected int frameHeight;
		protected bool enabled;
		protected bool flipped = false;
		protected bool onGround;
		protected Rectangle collisionRectangle;
		protected int collideWidth;
		protected int collideHeight;
		protected bool codeBasedBlocks = true;
		protected float drawDepth = 0.85f;
		protected Dictionary<string, AnimationStrip> animations = 
		new Dictionary<string, AnimationStrip> ();
		protected string currentAnimation;
		public int CollisionRadius = 0;
		public int BoundingXPadding = 0;
		public int BoundingYPadding = 0;
		

	#endregion

	#region Properties
		public bool Enabled {
			get { return enabled; }
			set { enabled = value; }
		}

		public bool Visible {
			get { return enabled; }
			set { enabled = value; }
		}
		
		public Vector2 WorldLocation {
			get { return worldLocation; }
			set { worldLocation = value; }
		}

		public Vector2 WorldCenter {
			get {
				return new Vector2 (
					(int)worldLocation.X + (int)(frameWidth / 2),
					(int)worldLocation.Y + (int)(frameHeight / 2));
			}
		}

		public Rectangle WorldRectangle {
			get {
				return new Rectangle (
					(int)worldLocation.X,
					(int)worldLocation.Y,
					frameWidth,
					frameHeight);
			}
		}

		public Rectangle CollisionRectangle {
			get {
				return new Rectangle (
					(int)worldLocation.X + collisionRectangle.X,
					(int)WorldRectangle.Y + collisionRectangle.Y,
					collisionRectangle.Width,
					collisionRectangle.Height);
			}
			set { collisionRectangle = value; }
		}
		
		public bool IsBoxColliding (Rectangle OtherBox)
		{
			return CollisionRectangle.Intersects (OtherBox);
		}

		public Vector2 Center {
			get {
				return worldLocation + 
					new Vector2 (frameWidth / 2, frameHeight / 2);
			}
		}
		
		public bool IsCircleColliding (Vector2 otherCenter, float otherRadius)
		{
			if (Vector2.Distance (Center, otherCenter) < 
				(CollisionRadius + otherRadius))
				return true;
			else
				return false;
		}		
		
		public float Rotation { get; set; }
		public Vector2 RotationOrigin { get; set; }
		
	#endregion 

	#region Helper Methods
		protected void updateAnimation (GameTime gameTime)
		{
			if (animations.ContainsKey (currentAnimation)) {
				if (animations [currentAnimation].FinishedPlaying) {
					PlayAnimation (animations [currentAnimation].NextAnimation);
				} else {
					animations [currentAnimation].Update (gameTime);
				}
			}
		}

	#endregion

	#region Map-Based Collision Detection Methods
		private Vector2 horizontalCollisionTest (Vector2 moveAmount)
		{
			if (moveAmount.X == 0)
				return moveAmount;

			Rectangle afterMoveRect = CollisionRectangle;
			afterMoveRect.Offset ((int)moveAmount.X, 0);
			Vector2 corner1, corner2;

			if (moveAmount.X < 0) {
				corner1 = new Vector2 (afterMoveRect.Left,
					afterMoveRect.Top + 1);
				corner2 = new Vector2 (afterMoveRect.Left,
					afterMoveRect.Bottom - 1);
			} else {
				corner1 = new Vector2 (afterMoveRect.Right,
					afterMoveRect.Top + 1);
				corner2 = new Vector2 (afterMoveRect.Right,
					afterMoveRect.Bottom - 1);
			}

//			Vector2 mapCell1 = TileMap.GetCellByPixel (corner1);
//			Vector2 mapCell2 = TileMap.GetCellByPixel (corner2);
//
//			if (!TileMap.CellIsPassable (mapCell1) || 
//		!TileMap.CellIsPassable (mapCell2)) {
//				moveAmount.X = 0;
//				velocity.X = 0;
//			}
//
//			if (codeBasedBlocks) {
//				if (TileMap.CellCodeValue (mapCell1) == "BLOCK" || 
//			TileMap.CellCodeValue (mapCell2) == "BLOCK") {
//					moveAmount.X = 0;
//					velocity.X = 0;
//				}
//			}

			return moveAmount;
		}

		private Vector2 verticalCollisionTest (Vector2 moveAmount)
		{
			if (moveAmount.Y == 0)
				return moveAmount;

			Rectangle afterMoveRect = CollisionRectangle;
			afterMoveRect.Offset ((int)moveAmount.X, (int)moveAmount.Y);
			Vector2 corner1, corner2;

			if (moveAmount.Y < 0) {
				corner1 = new Vector2 (afterMoveRect.Left + 1,
					afterMoveRect.Top);
				corner2 = new Vector2 (afterMoveRect.Right - 1,
					afterMoveRect.Top);
			} else {
				corner1 = new Vector2 (afterMoveRect.Left + 1,
					afterMoveRect.Bottom);
				corner2 = new Vector2 (afterMoveRect.Right - 1,
					afterMoveRect.Bottom);
			}

//			Vector2 mapCell1 = TileMap.GetCellByPixel (corner1);
//			Vector2 mapCell2 = TileMap.GetCellByPixel (corner2);
//
//			if (!TileMap.CellIsPassable (mapCell1) || 
//		!TileMap.CellIsPassable (mapCell2)) {
//				if (moveAmount.Y > 0)
//					onGround = true;
//				moveAmount.Y = 0;
//				velocity.Y = 0;
//			}
//
//			if (codeBasedBlocks) {
//				if (TileMap.CellCodeValue (mapCell1) == "BLOCK" || 
//			TileMap.CellCodeValue (mapCell2) == "BLOCK") {
//					if (moveAmount.Y > 0)
//						onGround = true;
//					moveAmount.Y = 0;
//					velocity.Y = 0;
//				}
//			}

			return moveAmount;
		}

	#endregion

	#region Public Methods
		public void PlayAnimation (string name)
		{
			if (!(name == null) && animations.ContainsKey (name)) {
				currentAnimation = name;
				animations [name].Play ();
			}
		}

		public virtual void Update (GameTime gameTime)
		{
			if (!enabled)
				return;

			float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

			updateAnimation (gameTime);

			if (velocity.Y != 0) {
				onGround = false;
			}

			Vector2 moveAmount = velocity * elapsed;

			moveAmount = horizontalCollisionTest (moveAmount);
			moveAmount = verticalCollisionTest (moveAmount);

			Vector2 newPosition = worldLocation + moveAmount;

			newPosition = new Vector2 (
					MathHelper.Clamp (newPosition.X, 0, 
					Camera.WorldRectangle.Width - frameWidth),
					MathHelper.Clamp (newPosition.Y, 2 * (-TileMap.TileHeight), 
					Camera.WorldRectangle.Height - frameHeight));

			worldLocation = newPosition;
		}

		public virtual void Draw (SpriteBatch spriteBatch)
		{
			if (!enabled)
				return;

			if (animations.ContainsKey (currentAnimation)) {

				SpriteEffects effect = SpriteEffects.None;

				if (flipped) {
					effect = SpriteEffects.FlipHorizontally;
				}
				
				Rectangle ws = Camera.WorldToScreen(WorldRectangle);
				if (ws.X != (int)worldLocation.X || ws.Y != (int)worldLocation.Y)
					Console.WriteLine(ws + " / " + worldLocation);
					
				spriteBatch.Draw (animations [currentAnimation].Texture, 
							Camera.WorldToScreen (WorldRectangle), 
							animations [currentAnimation].FrameRectangle, 
							Color.White, Rotation, RotationOrigin, effect, drawDepth);
			}
		}

		public int Width 
		{
			get { return animations [currentAnimation].FrameWidth; }
		}
		public int Height
		{
			get { return animations [currentAnimation].FrameHeight; }
		}
		
	#endregion

	}
}
