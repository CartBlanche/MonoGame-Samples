using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
namespace BackgroundThreadTester
{
    public class TextManager : Microsoft.Xna.Framework.DrawableGameComponent
    {
        public SpriteFont sfStandard;
        protected SpriteBatch spriteBatch = null;
        
        public TextManager(Game game, SpriteFont sfStandardFont)
            : base(game)
        {
            spriteBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));
            sfStandard = sfStandardFont;

        }//TextManager

        public override void Draw(GameTime gameTime)
        {  
            spriteBatch.DrawString(sfStandard, "Click here to create background thread and make it\nadd 5 new components", new Vector2(50, 200), Color.White);
        }//Draw
    }
}
