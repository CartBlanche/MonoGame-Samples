using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    public class Agent
    {
        private Body _agentBody;
        private Sprite _box;
        private Category _collidesWith;
        private Category _collisionCategories;
        private Sprite _knob;
        private float _offset;
        private PhysicsGameScreen _screen;

        public Agent(World world, PhysicsGameScreen screen, Vector2 position)
        {
            _collidesWith = Category.All;
            _collisionCategories = Category.All;

            _agentBody = BodyFactory.CreateBody(world, position);
            _agentBody.BodyType = BodyType.Dynamic;

            //Center
            FixtureFactory.AttachCircle(0.5f, 0.5f, _agentBody);

            //Left arm
            FixtureFactory.AttachRectangle(1.5f, 0.4f, 1f, new Vector2(-1f, 0f), _agentBody);
            FixtureFactory.AttachCircle(0.5f, 0.5f, _agentBody, new Vector2(-2f, 0f));

            //Right arm
            FixtureFactory.AttachRectangle(1.5f, 0.4f, 1f, new Vector2(1f, 0f), _agentBody);
            FixtureFactory.AttachCircle(0.5f, 0.5f, _agentBody, new Vector2(2f, 0f));

            //Top arm
            FixtureFactory.AttachRectangle(0.4f, 1.5f, 1f, new Vector2(0f, 1f), _agentBody);
            FixtureFactory.AttachCircle(0.5f, 0.5f, _agentBody, new Vector2(0f, 2f));

            //Bottom arm
            FixtureFactory.AttachRectangle(0.4f, 1.5f, 1f, new Vector2(0f, -1f), _agentBody);
            FixtureFactory.AttachCircle(0.5f, 0.5f, _agentBody, new Vector2(0f, -2f));

            _screen = screen;

            //GFX
            AssetCreator creator = _screen.ScreenManager.Assets;
            _box = new Sprite(creator.TextureFromVertices(PolygonTools.CreateRectangle(1.75f, 0.2f),
                                                           MaterialType.Blank, Color.White, 1f));
            _knob = new Sprite(creator.CircleTexture(0.5f, MaterialType.Blank, Color.Orange, 1f));
            _offset = ConvertUnits.ToDisplayUnits(2f);
        }

        public Category CollisionCategories
        {
            get { return _collisionCategories; }
            set
            {
                _collisionCategories = value;
                Body.CollisionCategories = value;
            }
        }

        public Category CollidesWith
        {
            get { return _collidesWith; }
            set
            {
                _collidesWith = value;
                Body.CollidesWith = value;
            }
        }

        public Body Body
        {
            get { return _agentBody; }
        }

        public void Draw()
        {
            SpriteBatch batch = _screen.ScreenManager.SpriteBatch;
            //cross
            batch.Draw(_box.Texture, ConvertUnits.ToDisplayUnits(_agentBody.Position), null,
                        Color.White, _agentBody.Rotation, _box.Origin, 1f, SpriteEffects.None, 0f);
            batch.Draw(_box.Texture, ConvertUnits.ToDisplayUnits(_agentBody.Position), null,
                        Color.White, _agentBody.Rotation + MathHelper.Pi / 2f, _box.Origin, 1f, SpriteEffects.None, 0f);
            //knobs
            batch.Draw(_knob.Texture, ConvertUnits.ToDisplayUnits(_agentBody.Position), null,
                        Color.White, _agentBody.Rotation, _knob.Origin, 1f, SpriteEffects.None, 0f);
            batch.Draw(_knob.Texture, ConvertUnits.ToDisplayUnits(_agentBody.Position), null,
                        Color.White, _agentBody.Rotation, _knob.Origin + new Vector2(0f, _offset), 1f,
                        SpriteEffects.None, 0f);
            batch.Draw(_knob.Texture, ConvertUnits.ToDisplayUnits(_agentBody.Position), null,
                        Color.White, _agentBody.Rotation, _knob.Origin - new Vector2(0f, _offset), 1f,
                        SpriteEffects.None, 0f);
            batch.Draw(_knob.Texture, ConvertUnits.ToDisplayUnits(_agentBody.Position), null,
                        Color.White, _agentBody.Rotation, _knob.Origin + new Vector2(_offset, 0f), 1f,
                        SpriteEffects.None, 0f);
            batch.Draw(_knob.Texture, ConvertUnits.ToDisplayUnits(_agentBody.Position), null,
                        Color.White, _agentBody.Rotation, _knob.Origin - new Vector2(_offset, 0f), 1f,
                        SpriteEffects.None, 0f);
        }
    }
}