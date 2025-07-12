using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
