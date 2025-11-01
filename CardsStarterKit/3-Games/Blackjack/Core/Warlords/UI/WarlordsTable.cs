//-----------------------------------------------------------------------------
// WarlordsTable.cs
//
// Visual representation of the Warlords playing field
//-----------------------------------------------------------------------------

using System;
using CardsFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Warlords
{
    /// <summary>
    /// Displays the four-zone playing field for Warlords
    /// </summary>
    public class WarlordsTable : GameTable
    {
        private Rectangle[] zoneRects;
        private SpriteFont font;
        private Texture2D pixel;
        
        public WarlordsTable(Rectangle tableBounds, string theme, Game game, 
                            SpriteBatch spriteBatch, Matrix? globalTransformation = null)
            : base(tableBounds, Vector2.Zero, 4, null, theme, game, spriteBatch, globalTransformation)
        {
            // Divide table into 4 equal horizontal zones
            int zoneHeight = tableBounds.Height / 4;
            zoneRects = new Rectangle[4];
            
            for (int i = 0; i < 4; i++)
            {
                zoneRects[i] = new Rectangle(
                    tableBounds.X,
                    tableBounds.Y + (i * zoneHeight),
                    tableBounds.Width,
                    zoneHeight
                );
            }
        }
        
        protected override void LoadContent()
        {
            base.LoadContent();
            
            // Load font
            font = Game.Content.Load<SpriteFont>("Fonts/Regular");
            
            // Create 1x1 white pixel texture for drawing rectangles
            pixel = new Texture2D(Game.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }
        
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            
            SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);
            
            // Zone colors and labels
            Color[] zoneColors = { 
                Color.DarkRed * 0.3f,      // Enemy Base
                Color.Red * 0.2f,          // Enemy Battlefield
                Color.Blue * 0.2f,         // Your Battlefield
                Color.DarkBlue * 0.3f      // Your Home Base
            };
            
            string[] zoneLabels = {
                "ENEMY BASE",
                "ENEMY BATTLEFIELD",
                "YOUR BATTLEFIELD",
                "YOUR HOME BASE"
            };
            
            for (int i = 0; i < 4; i++)
            {
                // Draw zone background
                SpriteBatch.Draw(pixel, zoneRects[i], zoneColors[i]);
                
                // Draw zone border
                DrawRectangleBorder(zoneRects[i], Color.White, 2);
                
                // Draw zone label
                Vector2 labelSize = font.MeasureString(zoneLabels[i]);
                Vector2 labelPos = new Vector2(
                    zoneRects[i].X + 10,
                    zoneRects[i].Y + 10
                );
                SpriteBatch.DrawString(font, zoneLabels[i], labelPos, Color.White);
            }
            
            SpriteBatch.End();
        }
        
        /// <summary>
        /// Draw a rectangle border
        /// </summary>
        private void DrawRectangleBorder(Rectangle rect, Color color, int thickness)
        {
            // Top
            SpriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            SpriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            SpriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            SpriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
        
        /// <summary>
        /// Get the rectangle for a specific zone
        /// </summary>
        public Rectangle GetZoneRect(int zoneIndex)
        {
            if (zoneIndex >= 0 && zoneIndex < zoneRects.Length)
                return zoneRects[zoneIndex];
            return Rectangle.Empty;
        }
    }
}
