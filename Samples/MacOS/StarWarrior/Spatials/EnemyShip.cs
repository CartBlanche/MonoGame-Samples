using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;
using StarWarrior.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarWarrior.Primitives;
using Microsoft.Xna.Framework.Content;

namespace StarWarrior.Spatials
{
    static class EnemyShip
    {
        private static Texture2D ship = null;
      
	    public static void Render(SpriteBatch spriteBatch, ContentManager contentManager, Transform transform) {
            if (ship == null)
            {
                ship = contentManager.Load<Texture2D>("enemy");
            }
            Rectangle rect = new Rectangle((int)transform.GetX(),(int)transform.GetY(),ship.Width,ship.Height);
            spriteBatch.Draw(ship, rect, Color.Red);
	    }
    }
}
