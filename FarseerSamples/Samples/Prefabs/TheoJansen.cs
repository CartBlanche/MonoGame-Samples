using System.Collections.Generic;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    public class TheoJansenWalker
    {
        private Sprite _body;
        private Body _chassis;
        private Sprite _engine;
        private Sprite _leftLeg;
        private Body[] _leftLegs;
        private Sprite _leftShoulder;
        private Body[] _leftShoulders;
        private RevoluteJoint _motorJoint;
        private bool _motorOn;
        private float _motorSpeed;
        private Vector2 _position;
        private Sprite _rightLeg;
        private Body[] _rightLegs;

        private Sprite _rightShoulder;
        private Body[] _rightShoulders;
        private PhysicsGameScreen _screen;

        private List<DistanceJoint> _walkerJoints;
        private Body _wheel;

        public TheoJansenWalker(World world, PhysicsGameScreen screen, Vector2 position)
        {
            _position = position;
            _motorSpeed = 2.0f;
            _motorOn = true;
            _screen = screen;

            _walkerJoints = new List<DistanceJoint>();

            _leftShoulders = new Body[3];
            _rightShoulders = new Body[3];
            _leftLegs = new Body[3];
            _rightLegs = new Body[3];

            Vector2 pivot = new Vector2(0f, -0.8f);

            // Chassis
            {
                PolygonShape shape = new PolygonShape(1f);
                shape.SetAsBox(2.5f, 1.0f);

                _body =
                    new Sprite(_screen.ScreenManager.Assets.TextureFromShape(shape, MaterialType.Blank,
                                                                             Color.Beige, 1f));

                _chassis = BodyFactory.CreateBody(world);
                _chassis.BodyType = BodyType.Dynamic;
                _chassis.Position = pivot + _position;

                Fixture fixture = _chassis.CreateFixture(shape);
                fixture.CollisionGroup = -1;
            }

            {
                CircleShape shape = new CircleShape(1.6f, 1f);
                _engine =
                    new Sprite(_screen.ScreenManager.Assets.TextureFromShape(shape, MaterialType.Waves,
                                                                             Color.Beige * 0.8f, 1f));

                _wheel = BodyFactory.CreateBody(world);
                _wheel.BodyType = BodyType.Dynamic;
                _wheel.Position = pivot + _position;

                Fixture fixture = _wheel.CreateFixture(shape);
                fixture.CollisionGroup = -1;
            }

            {
                _motorJoint = new RevoluteJoint(_wheel, _chassis, _wheel.GetLocalPoint(_chassis.Position), Vector2.Zero);
                _motorJoint.CollideConnected = false;
                _motorJoint.MotorSpeed = _motorSpeed;
                _motorJoint.MaxMotorTorque = 400f;
                _motorJoint.MotorEnabled = _motorOn;
                world.AddJoint(_motorJoint);
            }

            Vector2 wheelAnchor = pivot + new Vector2(0f, 0.8f);

            CreateLegTextures();

            CreateLeg(world, -1f, wheelAnchor, 0);
            CreateLeg(world, 1f, wheelAnchor, 0);

            _leftLeg.Origin = AssetCreator.CalculateOrigin(_leftLegs[0]);
            _leftShoulder.Origin = AssetCreator.CalculateOrigin(_leftShoulders[0]);
            _rightLeg.Origin = AssetCreator.CalculateOrigin(_rightLegs[0]);
            _rightShoulder.Origin = AssetCreator.CalculateOrigin(_rightShoulders[0]);

            _wheel.SetTransform(_wheel.Position, 120f * Settings.Pi / 180f);
            CreateLeg(world, -1f, wheelAnchor, 1);
            CreateLeg(world, 1f, wheelAnchor, 1);

            _wheel.SetTransform(_wheel.Position, -120f * Settings.Pi / 180f);
            CreateLeg(world, -1f, wheelAnchor, 2);
            CreateLeg(world, 1f, wheelAnchor, 2);
        }

        public void Reverse()
        {
            _motorSpeed *= -1f;
            _motorJoint.MotorSpeed = _motorSpeed;
        }

        private void CreateLeg(World world, float s, Vector2 wheelAnchor, int index)
        {
            Vector2 p1 = new Vector2(5.4f * s, 6.1f);
            Vector2 p2 = new Vector2(7.2f * s, 1.2f);
            Vector2 p3 = new Vector2(4.3f * s, 1.9f);
            Vector2 p4 = new Vector2(3.1f * s, -0.8f);
            Vector2 p5 = new Vector2(6.0f * s, -1.5f);
            Vector2 p6 = new Vector2(2.5f * s, -3.7f);

            PolygonShape poly1 = new PolygonShape(1f);
            PolygonShape poly2 = new PolygonShape(2f);

            Vertices vertices = new Vertices(3);

            if (s < 0f)
            {
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);
                poly1.Set(vertices);

                vertices[0] = Vector2.Zero;
                vertices[1] = p5 - p4;
                vertices[2] = p6 - p4;
                poly2.Set(vertices);
            }
            else
            {
                vertices.Add(p1);
                vertices.Add(p3);
                vertices.Add(p2);
                poly1.Set(vertices);

                vertices[0] = Vector2.Zero;
                vertices[1] = p6 - p4;
                vertices[2] = p5 - p4;
                poly2.Set(vertices);
            }

            Body body1 = BodyFactory.CreateBody(world);
            body1.BodyType = BodyType.Dynamic;
            body1.Position = _position;
            body1.AngularDamping = 10f;
            if (s < 0f)
            {
                _leftLegs[index] = body1;
            }
            else
            {
                _rightLegs[index] = body1;
            }

            Body body2 = BodyFactory.CreateBody(world);
            body2.BodyType = BodyType.Dynamic;
            body2.Position = p4 + _position;
            body2.AngularDamping = 10f;
            if (s < 0f)
            {
                _leftShoulders[index] = body2;
            }
            else
            {
                _rightShoulders[index] = body2;
            }

            Fixture f1 = body1.CreateFixture(poly1);
            f1.CollisionGroup = -1;

            Fixture f2 = body2.CreateFixture(poly2);
            f2.CollisionGroup = -1;

            // Using a soft distanceraint can reduce some jitter.
            // It also makes the structure seem a bit more fluid by
            // acting like a suspension system.
            DistanceJoint djd = new DistanceJoint(body1, body2, body1.GetLocalPoint(p2 + _position),
                                                  body2.GetLocalPoint(p5 + _position));
            djd.DampingRatio = 0.5f;
            djd.Frequency = 10f;

            world.AddJoint(djd);
            _walkerJoints.Add(djd);

            DistanceJoint djd2 = new DistanceJoint(body1, body2, body1.GetLocalPoint(p3 + _position),
                                                   body2.GetLocalPoint(p4 + _position));
            djd2.DampingRatio = 0.5f;
            djd2.Frequency = 10f;

            world.AddJoint(djd2);
            _walkerJoints.Add(djd2);

            DistanceJoint djd3 = new DistanceJoint(body1, _wheel, body1.GetLocalPoint(p3 + _position),
                                                   _wheel.GetLocalPoint(wheelAnchor + _position));
            djd3.DampingRatio = 0.5f;
            djd3.Frequency = 10f;

            world.AddJoint(djd3);
            _walkerJoints.Add(djd3);

            DistanceJoint djd4 = new DistanceJoint(body2, _wheel, body2.GetLocalPoint(p6 + _position),
                                                   _wheel.GetLocalPoint(wheelAnchor + _position));
            djd4.DampingRatio = 0.5f;
            djd4.Frequency = 10f;

            world.AddJoint(djd4);
            _walkerJoints.Add(djd4);

            Vector2 anchor = p4 - new Vector2(0f, -0.8f);
            RevoluteJoint rjd = new RevoluteJoint(body2, _chassis, body2.GetLocalPoint(_chassis.GetWorldPoint(anchor)),
                                                  anchor);
            world.AddJoint(rjd);
        }

        private void CreateLegTextures()
        {
            Vector2 p1 = new Vector2(-5.4f, 6.1f);
            Vector2 p2 = new Vector2(-7.2f, 1.2f);
            Vector2 p3 = new Vector2(-4.3f, 1.9f);
            Vector2 p4 = new Vector2(-2.9f, -0.7f);
            Vector2 p5 = new Vector2(0.6f, -2.9f);

            Vertices vertices = new Vertices(3);

            vertices.Add(p1);
            vertices.Add(p2);
            vertices.Add(p3);
            _leftLeg =
                new Sprite(_screen.ScreenManager.Assets.TextureFromVertices(vertices, MaterialType.Blank,
                                                                            Color.IndianRed * 0.8f, 1f));

            vertices[0] = Vector2.Zero;
            vertices[1] = p4;
            vertices[2] = p5;
            _leftShoulder =
                new Sprite(_screen.ScreenManager.Assets.TextureFromVertices(vertices, MaterialType.Blank,
                                                                            Color.Beige * 0.8f, 1f));

            p1.X *= -1f;
            p2.X *= -1f;
            p3.X *= -1f;
            p4.X *= -1f;
            p5.X *= -1f;

            vertices[0] = p1;
            vertices[1] = p3;
            vertices[2] = p2;
            _rightLeg =
                new Sprite(_screen.ScreenManager.Assets.TextureFromVertices(vertices, MaterialType.Blank,
                                                                            Color.IndianRed * 0.8f, 1f));

            vertices[0] = Vector2.Zero;
            vertices[1] = p5;
            vertices[2] = p4;
            _rightShoulder =
                new Sprite(_screen.ScreenManager.Assets.TextureFromVertices(vertices, MaterialType.Blank,
                                                                            Color.Beige * 0.8f, 1f));
        }

        public void Draw()
        {
            LineBatch _batch = _screen.ScreenManager.LineBatch;
            SpriteBatch _spriteBatch = _screen.ScreenManager.SpriteBatch;

            _spriteBatch.Begin(0, null, null, null, null, null, _screen.Camera.View);
            _spriteBatch.Draw(_body.Texture, ConvertUnits.ToDisplayUnits(_chassis.Position), null,
                              Color.White, _chassis.Rotation, _body.Origin, 1f, SpriteEffects.None, 0f);
            _spriteBatch.End();

            _batch.Begin(_screen.Camera.SimProjection, _screen.Camera.SimView);
            for (int i = 0; i < _walkerJoints.Count; ++i)
            {
                _batch.DrawLine(_walkerJoints[i].WorldAnchorA, _walkerJoints[i].WorldAnchorB, Color.DarkRed);
            }
            _batch.End();

            _spriteBatch.Begin(0, null, null, null, null, null, _screen.Camera.View);
            for (int i = 0; i < 3; ++i)
            {
                _spriteBatch.Draw(_leftLeg.Texture, ConvertUnits.ToDisplayUnits(_leftLegs[i].Position), null,
                                  Color.White, _leftLegs[i].Rotation, _leftLeg.Origin, 1f, SpriteEffects.None, 0f);
                _spriteBatch.Draw(_leftShoulder.Texture, ConvertUnits.ToDisplayUnits(_leftShoulders[i].Position), null,
                                  Color.White, _leftShoulders[i].Rotation, _leftShoulder.Origin, 1f, SpriteEffects.None,
                                  0f);
                _spriteBatch.Draw(_rightLeg.Texture, ConvertUnits.ToDisplayUnits(_rightLegs[i].Position), null,
                                  Color.White, _rightLegs[i].Rotation, _rightLeg.Origin, 1f, SpriteEffects.None, 0f);
                _spriteBatch.Draw(_rightShoulder.Texture, ConvertUnits.ToDisplayUnits(_rightShoulders[i].Position), null,
                                  Color.White, _rightShoulders[i].Rotation, _rightShoulder.Origin, 1f,
                                  SpriteEffects.None, 0f);
            }
            _spriteBatch.Draw(_engine.Texture, ConvertUnits.ToDisplayUnits(_wheel.Position), null,
                              Color.White, _wheel.Rotation, _engine.Origin, 1f, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }
    }
}