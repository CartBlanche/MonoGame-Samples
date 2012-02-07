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
    static class Missile
    {
        static Texture2D bullet = null;
	   
	    public static void Render(SpriteBatch spriteBatch,ContentManager contentManager,Transform transform) {
            if (bullet == null)
            {
                bullet = contentManager.Load<Texture2D>("bullet");
            }
		    spriteBatch.Draw(bullet, new Vector2(transform.GetX(), transform.GetY()), Color.White);
	    }
    }
}
