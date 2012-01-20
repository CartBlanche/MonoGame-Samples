
using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Samples.Draw2D
{
	public class FPSCounterComponent : DrawableGameComponent
    {
        int frameRate = 0;
        int frameCounter = 0;
        TimeSpan elapsedTime = TimeSpan.Zero;

        public SpriteBatch Batch { get; set; }
        public SpriteFont Font { get; set; }

        public FPSCounterComponent(Game game)
            : base(game)
        {
        }


        public override void Update(GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            }
        }


        public override void Draw(GameTime gameTime)
        {
            frameCounter++;

            string fps = string.Format("fps: {0} mem : {1}", frameRate, GC.GetTotalMemory(false));

            Batch.DrawString(Font, fps, new Vector2(1, 1), Color.Black);
            Batch.DrawString(Font, fps, new Vector2(0, 0), Color.White);
        }
    }
}
