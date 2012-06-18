using System.Collections.Generic;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    public class Spiderweb
    {
        private Sprite _goo;
        private Sprite _link;

        private float _radius;
        private float _spriteScale;
        private World _world;

        public Spiderweb(World world, Vector2 position, float radius, int rings, int sides)
        {
            const float breakpoint = 100f;

            _world = world;
            _radius = radius;

            List<List<Body>> ringBodys = new List<List<Body>>(rings);

            for (int i = 1; i < rings; ++i)
            {
                Vertices vertices = PolygonTools.CreateCircle(i * 2.9f, sides);
                List<Body> bodies = new List<Body>(sides);

                //Create the first goo
                Body prev = BodyFactory.CreateCircle(world, radius, 0.2f, vertices[0]);
                prev.FixedRotation = true;
                prev.Position += position;
                prev.BodyType = BodyType.Dynamic;

                bodies.Add(prev);

                //Connect the first goo to the next
                for (int j = 1; j < vertices.Count; ++j)
                {
                    Body bod = BodyFactory.CreateCircle(world, radius, 0.2f, vertices[j]);
                    bod.FixedRotation = true;
                    bod.BodyType = BodyType.Dynamic;
                    bod.Position += position;

                    DistanceJoint dj = JointFactory.CreateDistanceJoint(world, prev, bod, Vector2.Zero, Vector2.Zero);
                    dj.Frequency = 4.0f;
                    dj.DampingRatio = 0.5f;
                    dj.Breakpoint = breakpoint;

                    prev = bod;
                    bodies.Add(bod);
                }

                //Connect the first and the last box
                DistanceJoint djEnd = JointFactory.CreateDistanceJoint(world, bodies[0], bodies[bodies.Count - 1],
                                                                       Vector2.Zero, Vector2.Zero);
                djEnd.Frequency = 4.0f;
                djEnd.DampingRatio = 0.5f;
                djEnd.Breakpoint = breakpoint;

                ringBodys.Add(bodies);
            }

            //Create an outer ring
            Vertices lastRing = PolygonTools.CreateCircle(rings * 2.9f, sides);
            lastRing.Translate(ref position);

            List<Body> lastRingFixtures = ringBodys[ringBodys.Count - 1];

            //Fix each of the fixtures of the outer ring
            for (int j = 0; j < lastRingFixtures.Count; ++j)
            {
                FixedDistanceJoint fdj = JointFactory.CreateFixedDistanceJoint(world, lastRingFixtures[j], Vector2.Zero,
                                                                               lastRing[j]);
                fdj.Frequency = 4.0f;
                fdj.DampingRatio = 0.5f;
                fdj.Breakpoint = breakpoint;
            }

            //Interconnect the rings
            for (int i = 1; i < ringBodys.Count; i++)
            {
                List<Body> prev = ringBodys[i - 1];
                List<Body> current = ringBodys[i];

                for (int j = 0; j < prev.Count; j++)
                {
                    Body prevFixture = prev[j];
                    Body currentFixture = current[j];

                    DistanceJoint dj = JointFactory.CreateDistanceJoint(world, prevFixture, currentFixture, Vector2.Zero,
                                                                        Vector2.Zero);
                    dj.Frequency = 4.0f;
                    dj.DampingRatio = 0.5f;
                }
            }
        }

        public void LoadContent(ContentManager content)
        {
            _link = new Sprite(content.Load<Texture2D>("Samples/link"));
            _goo = new Sprite(content.Load<Texture2D>("Samples/goo"));

            _spriteScale = 2f * ConvertUnits.ToDisplayUnits(_radius) / _goo.Texture.Width;
        }

        public void Draw(SpriteBatch batch)
        {
            foreach (Joint j in _world.JointList)
            {
                if (j.Enabled && j.JointType != JointType.FixedMouse)
                {
                    Vector2 pos = ConvertUnits.ToDisplayUnits((j.WorldAnchorA + j.WorldAnchorB) / 2f);
                    Vector2 AtoB = j.WorldAnchorB - j.WorldAnchorA;
                    float distance = ConvertUnits.ToDisplayUnits(AtoB.Length()) + 8f * _spriteScale;
                    Vector2 scale = new Vector2(distance / _link.Texture.Width, _spriteScale);
                    Vector2 unitx = Vector2.UnitX;
                    float angle = (float)MathUtils.VectorAngle(ref unitx, ref AtoB);
                    batch.Draw(_link.Texture, pos, null, Color.White, angle, _link.Origin, scale, SpriteEffects.None, 0f);
                }
            }

            foreach (Body b in _world.BodyList)
            {
                if (b.Enabled && b.FixtureList[0].ShapeType == ShapeType.Circle)
                {
                    batch.Draw(_goo.Texture, ConvertUnits.ToDisplayUnits(b.Position), null,
                               Color.White, 0f, _goo.Origin, _spriteScale, SpriteEffects.None, 0f);
                }
            }
        }
    }
}