using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Tile_Engine;

namespace Gorillas
{
	public static class TrajectoryAI
	{
		static Random random = new Random((int)DateTime.Now.Ticks);
		
		public static void CalculateAccuracy (ref Vector2 trajectory)
		{
			int percentage = random.Next(1,101);
			int angleOrVelocity = random.Next(2);
			Console.WriteLine("Percentage: " + percentage);
			
			switch (OptionsMenuScreen.currentSkillLevel) {
			case SkillLevel.Beginner:
				if (percentage < 20) {
					
					if (angleOrVelocity == 0)
						trajectory.X += GetRange(4,2);
					else
						trajectory.Y += GetRange(4,2);;
				}
				else {
					if (percentage < 50) {
						if (angleOrVelocity == 0)
							trajectory.X += GetRange(10,5);
						else
							trajectory.Y += GetRange(10,5);
					}	
					else {
						if (angleOrVelocity == 0)
							trajectory.X += GetRange(25,15);
						else
							trajectory.Y += GetRange(25,15);
						
					}
				}
				break;
			case SkillLevel.Intermediate:
				if (percentage < 30) {
					
					if (angleOrVelocity == 0)
						trajectory.X += GetRange(4,2);
					else
						trajectory.Y += GetRange(4,2);;
				}
				else {
					if (percentage < 50) {
						if (angleOrVelocity == 0)
							trajectory.X += GetRange(10,5);
						else
							trajectory.Y += GetRange(10,5);
					}	
					else {
						if (angleOrVelocity == 0)
							trajectory.X += GetRange(15,5);
						else
							trajectory.Y += GetRange(15,5);
						
					}
				}
				break;
			case SkillLevel.Advanced:
				if (percentage < 40) {
					
					if (angleOrVelocity == 0)
						trajectory.X += GetRange(2,0);
					else
						trajectory.Y += GetRange(2,0);;
				}
				else {
					if (percentage < 50) {
						if (angleOrVelocity == 0)
							trajectory.X += GetRange(10,5);
						else
							trajectory.Y += GetRange(10,5);
					}	
					else {
						if (angleOrVelocity == 0)
							trajectory.X += GetRange(10,5);
						else
							trajectory.Y += GetRange(10,5);
						
					}
				}
				break;				
			}
			
			trajectory.X = MathHelper.Clamp(trajectory.X, 0, 90);
			trajectory.Y = MathHelper.Clamp(trajectory.Y, 0, 100);
		}
		
		private static int GetRange(int range, int rangeError) 
		{
			int minus = random.Next(2);
			int newRange = 0;
			if (minus == 1) {
				newRange = random.Next(-range,-rangeError);
				Console.WriteLine("Minus: " + newRange);
			}
			else {
				newRange = random.Next(rangeError,range);
				Console.WriteLine("Positive: " + newRange);

				
			}
			
			return newRange;
		}	
		public static void Tracker (GameplayScreen engine, Gorilla thrower, Gorilla target, ref Vector2 trajectory ) 
		{
			CityScape city = engine.cityScape;
			Console.WriteLine(trajectory);
			Vector2 initialPosition = Vector2.Zero;
			Vector2 initialVelocity = Vector2.Zero;
			Vector2 acceleration = new Vector2 (0, -9.8f);
			float elapsed = 0.016f;

			if (trajectory == Vector2.Zero) {
				trajectory.X = 45;
				trajectory.Y = 50;
				if ((int)thrower.PlayerClass % 2 == 0) {
					trajectory.X = 180 - trajectory.X;
				}
			}
			
			float angle = trajectory.X;
			Vector2 velocity = new Vector2(0,trajectory.Y);
			
			List<CityBuilding> buildings = city.buildings;
			
			angle = 180 - angle;
			
			angle = MathHelper.ToRadians (angle);

			initialVelocity = new Vector2 ((float)Math.Cos (angle), (float)Math.Sin (angle)) * velocity.Y;
			
			initialPosition = thrower.WorldLocation;
			initialPosition.X += thrower.Width /2;
			initialPosition.Y -= 10;
			acceleration.X = -WindManager.Wind / 3f; // set the wind speed (must match Banana.ThrowMe)
			
			bool done = false;

			float time = 0;
			float time2 = 0;
			Vector2 newPosition = initialPosition;
			Vector2 oldPosition = initialPosition;
			bool gorillaHit = false;
			bool buildingHit = false;
			bool outOfBounds = false;
			bool isAscending = false;
			bool rightToLeft = false;
			
			float maxLimitAngle = 90;
			float maxLimitVelocity = 100;
			float minLimitAngle = 0;
			float minLimitVelocity = 0;
			
			bool acquiringTarget = true;
			
			int index = 0;
			int tries = 0;
			
			while (acquiringTarget && tries < 2) {
				index = 0;
				tries ++;
				if (tries == 2) {
					angle = MathHelper.ToRadians(180 - MathHelper.ToDegrees(62));
					velocity.Y = 50;
					
				}
				maxLimitAngle = 90;
				maxLimitVelocity = 100;
				minLimitAngle = 0;
				minLimitVelocity = 0;
				
				while (acquiringTarget && index < 20) {
					
					done = false;
					time = 0;
					index++;
					newPosition = initialPosition;
					initialVelocity = new Vector2 ((float)Math.Cos (angle), (float)Math.Sin (angle)) * velocity.Y;
					gorillaHit = false;
					buildingHit = false;
					outOfBounds = false;
					isAscending = false;
					rightToLeft = false;				
					
					while (!done) {
						
						oldPosition = newPosition;
						
						time += elapsed;
						
						time2 = time * 3.0f;
						
						newPosition = initialPosition - (((initialVelocity * time2)
						 + (0.5f * acceleration * time2 * time2)));
						
						if (newPosition.X < 0 || newPosition.X > Camera.ViewPortWidth) {
							done = true;
							outOfBounds = true;
						}
						
						foreach (Gorilla player in engine.players.players) {
							if (player.CollisionRectangle.Contains(newPosition)) {
								gorillaHit = true;
								done = true;
							}
						}
						
						foreach (var building in buildings) {
							if (building.isCollision(newPosition)) {
								done = true;
								buildingHit = true;
							}
						}
						
						isAscending = false;
						rightToLeft = false;
						if (newPosition.Y < oldPosition.Y)
							isAscending = true;
						if (newPosition.X > oldPosition.X)
							rightToLeft = true;
					}
		
					
					Console.WriteLine("\n-------\n gorilla: " + gorillaHit + " building: " + buildingHit + " out of bounds: " + outOfBounds + " ascending: " + isAscending + " rightToLeft: " + rightToLeft + " @ " + newPosition);
					
					if (outOfBounds) {
						Console.WriteLine ("over shot torget decrease velocity");
						float oldVelocity = velocity.Y;
						
						velocity.Y = minLimitVelocity + ((velocity.Y -  minLimitVelocity) / 2);
						maxLimitVelocity = oldVelocity;
						//velocity.Y = minLimitVelocity;
					}
					if (gorillaHit) {
						Console.WriteLine (" killed the animal with angle: " + MathHelper.ToDegrees(angle) + " velocity: " + velocity.Y );
						acquiringTarget = false;
						trajectory.X = MathHelper.ToDegrees(angle);
						trajectory.Y = velocity.Y;
					}
					if (buildingHit) {
						if (isAscending) {
							Console.WriteLine ("increase angle");
							angle = 180 - MathHelper.ToDegrees(angle);
							if (angle < 0)
								Console.WriteLine(angle);
							float oldAngle = angle;
							angle += (maxLimitAngle - angle) / 2;
							
							angle = 180 - angle;
							minLimitAngle = oldAngle;
							angle = MathHelper.ToRadians(angle);
						}
						if (!isAscending) {
							if (newPosition.X > target.WorldRectangle.Right) {
								if (rightToLeft) {
									Console.WriteLine ("over shot target decrease velocity");
									float oldVelocity = velocity.Y;
									
									velocity.Y = minLimitVelocity + ((velocity.Y -  minLimitVelocity) / 2);
									maxLimitVelocity = oldVelocity;
								}
								else {
									Console.WriteLine ("under shot target increase velocity");
									float oldVelocity = velocity.Y;
									
									velocity.Y += (maxLimitVelocity - velocity.Y) / 2;
									minLimitVelocity = oldVelocity;
									
								}
							}
							else {
								if (rightToLeft) {
									Console.WriteLine ("under shot target increase velocity");
	 								float oldVelocity = velocity.Y;
									
									velocity.Y += (maxLimitVelocity - velocity.Y) / 2;
									minLimitVelocity = oldVelocity;
								}
								else {
									Console.WriteLine ("over shot target decrease velocity");
									float oldVelocity = velocity.Y;
									
									velocity.Y = minLimitVelocity + ((velocity.Y -  minLimitVelocity) / 2);
									maxLimitVelocity = oldVelocity;
	
								}
							}
		
						}
					}
				}
			}
			
			if (acquiringTarget) {
				trajectory.X = 60;
				trajectory.Y = maxLimitVelocity;
			}
			if (tries >= 2) {
				if (trajectory.X < -3372)
					Console.WriteLine(trajectory);
				trajectory.X = Math.Abs(angle); //MathHelper.ToDegrees(Math.Abs(angle));
				trajectory.Y = velocity.Y;
			}
		}
		
		
		
	}
}

