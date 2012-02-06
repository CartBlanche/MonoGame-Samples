using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;
using StarWarrior.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace StarWarrior.Spatials
{
    static class ShipExplosion
    {
        static Texture2D circle = null;

        public static void Render(SpriteBatch spriteBatch, ContentManager contentManager, Transform transform, Color color, int radius)
        {
            if (circle == null)
            {
                circle = contentManager.Load<Texture2D>("explosion");
            }
            spriteBatch.Draw(circle, new Vector2((float)transform.GetX() - radius, (float)transform.GetY() - radius), null, Color.White, 0, new Vector2(0, 0), 0.8f, SpriteEffects.None, 0);
        }
    }
}
