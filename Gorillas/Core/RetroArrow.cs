using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gorillas
{
	/// <summary>
	/// Draws the game's blocky retro arrow: a dashed shaft with a stepped arrow head and a black
	/// outline so it stays readable on any background. Used for the aim arrow and the wind indicator.
	/// </summary>
	static class RetroArrow
	{
		const float dashLength = 6f;
		const float dashGap = 3f;
		const float headLength = 14f;
		const float shaftThickness = 3f;

		/// <param name="pixel">A 1x1 white texture.</param>
		/// <param name="origin">Where the arrow starts (its tail).</param>
		/// <param name="angle">Direction in radians, screen space (y points down).</param>
		/// <param name="length">Total length including the head.</param>
		public static void Draw (SpriteBatch spriteBatch, Texture2D pixel, Vector2 origin, float angle, float length, Color fill)
		{
			Draw (spriteBatch, pixel, origin, angle, length, fill, 1f);
		}

		/// <param name="scale">Scales the shaft thickness and head size (1 = aim arrow size).</param>
		public static void Draw (SpriteBatch spriteBatch, Texture2D pixel, Vector2 origin, float angle, float length, Color fill, float scale)
		{
			if (length < 1f)
				return;

			Vector2 dir = new Vector2 ((float)Math.Cos (angle), (float)Math.Sin (angle));
			Color outline = Color.Black * 0.8f;
			Vector2 left = new Vector2 (0, 0.5f);

			float head = Math.Min (headLength * scale, length);
			float shaft = length - head;
			float thickness = shaftThickness * scale;

			// Dashed shaft. A slightly larger black pass underneath makes the outline.
			for (float d = 0; d < shaft; d += dashLength + dashGap) {
				float segment = Math.Min (dashLength, shaft - d);
				Vector2 start = origin + dir * d;
				spriteBatch.Draw (pixel, start - dir, null, outline, angle, left,
					new Vector2 (segment + 2, thickness + 2), SpriteEffects.None, 0);
				spriteBatch.Draw (pixel, start, null, fill, angle, left,
					new Vector2 (segment, thickness), SpriteEffects.None, 0);
			}

			// Stepped arrow head: slices that narrow towards the tip.
			const float step = 2f;
			float halfBase = 6f * scale;
			for (float d = 0; d < head; d += step) {
				float halfWidth = (head - d) / head * halfBase + 0.5f;
				Vector2 centre = origin + dir * (shaft + d);
				spriteBatch.Draw (pixel, centre - dir, null, outline, angle, left,
					new Vector2 (step + 2, halfWidth * 2 + 2), SpriteEffects.None, 0);
			}
			for (float d = 0; d < head; d += step) {
				float halfWidth = (head - d) / head * halfBase + 0.5f;
				Vector2 centre = origin + dir * (shaft + d);
				spriteBatch.Draw (pixel, centre, null, fill, angle, left,
					new Vector2 (step, halfWidth * 2), SpriteEffects.None, 0);
			}
		}
	}
}
