using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace Gorillas
{
	public class Sun : GameObject
	{
		bool collisionReaction = false;
		
		#region Constructor
		public Sun (ContentManager content) {
			animations.Add ("idle", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Sun/Sunr"),
					41,
					"idle"));
			animations ["idle"].LoopAnimation = true;

			animations.Add ("smile", 
				new AnimationStrip (
					content.Load<Texture2D> ("Textures/Sprites/Sun/SunSmile"),
					41,
					"smile"));
			animations ["smile"].LoopAnimation = true;

			frameWidth = 41;
			frameHeight = 31;
			CollisionRectangle = new Rectangle (1, 1, 41, 31);

			drawDepth = 0.825f;

			enabled = true;
			codeBasedBlocks = false;
			PlayAnimation ("idle");
		}
		
		#endregion
		
		public bool CollisionReaction 
		{
			get { return collisionReaction; }
			
			set {
				collisionReaction = value;
			}
		}
		
		public void ReactToCollision () 
		{
			collisionReaction = true;
			PlayAnimation("smile");
		}
		
		public void Reset () 
		{
			collisionReaction = false;
			PlayAnimation("idle");
		}
		
	}
}

