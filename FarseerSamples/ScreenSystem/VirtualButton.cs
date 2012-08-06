using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace FarseerPhysics.SamplesFramework
{
    public sealed class VirtualButton
    {
        private Texture2D _sprite;
        private Vector2 _origin;
        private Rectangle _normal;
        private Rectangle _pressed;
        private Vector2 _position;

        public bool Pressed;

        public VirtualButton(Texture2D sprite, Vector2 position, Rectangle normal, Rectangle pressed)
        {
            _sprite = sprite;
            _origin = new Vector2(normal.Width / 2f, normal.Height / 2f);
            _normal = normal;
            _pressed = pressed;
            Pressed = false;
            _position = position;
        }

        public void Update(TouchLocation touchLocation)
        {
            if (touchLocation.State == TouchLocationState.Pressed ||
                touchLocation.State == TouchLocationState.Moved)
            {
                Vector2 delta = touchLocation.Position - _position;
                if (delta.LengthSquared() <= 400f)
                {
                    Pressed = true;
                }
            }
        }

        public void Draw(SpriteBatch batch)
        {
            batch.Draw(_sprite, _position, Pressed ? _pressed : _normal, Color.White, 0f, _origin, 1f, SpriteEffects.None, 0f);
        }
    }
}
