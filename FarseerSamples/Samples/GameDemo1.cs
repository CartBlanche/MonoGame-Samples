using System;
using System.Collections.Generic;
using System.Text;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FarseerPhysics.SamplesFramework
{
    internal class GameDemo1 : PhysicsGameScreen, IDemoScreen
    {
        private float _acceleration;
        private Body _board;
        private Sprite _box;
        private List<Body> _boxes;

        private Sprite _bridge;
        private List<Body> _bridgeSegments;
        private Body _car;
        private Sprite _carBody;
        private Body _ground;
        private float _hzBack;
        private float _hzFront;
        private float _maxSpeed;

        private float _scale;
        private LineJoint _springBack;
        private LineJoint _springFront;
        private Sprite _teeter;
        private Sprite _wheel;
        private Body _wheelBack;
        private Body _wheelFront;
        private float _zeta;

        #region IDemoScreen Members

        public string GetTitle()
        {
            return "Racing Car";
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TODO: Add sample description!");
            sb.AppendLine(string.Empty);
            sb.AppendLine("GamePad:");
            sb.AppendLine("  - Exit to menu: Back button");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Keyboard:");
            sb.AppendLine("  - Exit to menu: Escape");
            return sb.ToString();
        }

        #endregion

        public override void LoadContent()
        {
            base.LoadContent();

            World.Gravity = new Vector2(0f, 10f);

            HasCursor = false;
            EnableCameraControl = true;
            HasVirtualStick = true;

            _hzFront = 8.5f;
            _hzBack = 5.0f;
            _zeta = 0.85f;
            _maxSpeed = 50.0f;

#if WINDOWS_PHONE
            _scale = 2f / 3f;
#else
            _scale = 1f;
#endif

            // terrain
            _ground = new Body(World);
            {
                Vertices terrain = new Vertices();
                terrain.Add(new Vector2(-20f, -5f));
                terrain.Add(new Vector2(-20f, 0f));
                terrain.Add(new Vector2(20f, 0f));
                terrain.Add(new Vector2(25f, -0.25f));
                terrain.Add(new Vector2(30f, -1f));
                terrain.Add(new Vector2(35f, -4f));
                terrain.Add(new Vector2(40f, 0f));
                terrain.Add(new Vector2(45f, 0f));
                terrain.Add(new Vector2(50f, 1f));
                terrain.Add(new Vector2(55f, 2f));
                terrain.Add(new Vector2(60f, 2f));
                terrain.Add(new Vector2(65f, 1.25f));
                terrain.Add(new Vector2(70f, 0f));
                terrain.Add(new Vector2(75f, -0.3f));
                terrain.Add(new Vector2(80f, -1.5f));
                terrain.Add(new Vector2(85f, -3.5f));
                terrain.Add(new Vector2(90f, 0f));
                terrain.Add(new Vector2(95f, 0.5f));
                terrain.Add(new Vector2(100f, 1f));
                terrain.Add(new Vector2(105f, 2f));
                terrain.Add(new Vector2(110f, 2.5f));
                terrain.Add(new Vector2(115f, 1.3f));
                terrain.Add(new Vector2(120f, 0f));
                terrain.Add(new Vector2(160f, 0f));
                terrain.Add(new Vector2(159f, 10f));
                terrain.Add(new Vector2(201f, 10f));
                terrain.Add(new Vector2(200f, 0f));
                terrain.Add(new Vector2(240f, 0f));
                terrain.Add(new Vector2(250f, -5f));
                terrain.Add(new Vector2(250f, 10f));
                terrain.Add(new Vector2(270f, 10f));
                terrain.Add(new Vector2(270f, 0));
                terrain.Add(new Vector2(310f, 0));
                terrain.Add(new Vector2(310f, -5));

                for (int i = 0; i < terrain.Count - 1; ++i)
                {
                    FixtureFactory.AttachEdge(terrain[i], terrain[i + 1], _ground);
                }

                _ground.Friction = 0.6f;
            }

            // teeter board
            {
                _board = new Body(World);
                _board.BodyType = BodyType.Dynamic;
                _board.Position = new Vector2(140.0f, -1.0f);

                PolygonShape box = new PolygonShape(1f);
                box.SetAsBox(10.0f, 0.25f);
                _teeter =
                    new Sprite(ScreenManager.Assets.TextureFromShape(box, MaterialType.Pavement, Color.LightGray, 1.2f));

                _board.CreateFixture(box);

                RevoluteJoint teeterAxis = JointFactory.CreateRevoluteJoint(_ground, _board, Vector2.Zero);
                teeterAxis.LowerLimit = -8.0f * Settings.Pi / 180.0f;
                teeterAxis.UpperLimit = 8.0f * Settings.Pi / 180.0f;
                teeterAxis.LimitEnabled = true;
                World.AddJoint(teeterAxis);

                _board.ApplyAngularImpulse(-100.0f);
            }

            // bridge
            {
                _bridgeSegments = new List<Body>();

                const int segmentCount = 20;
                PolygonShape shape = new PolygonShape(1f);
                shape.SetAsBox(1.0f, 0.125f);
                _bridge =
                    new Sprite(ScreenManager.Assets.TextureFromShape(shape, MaterialType.Dots, Color.SandyBrown, 1f));

                Body prevBody = _ground;
                for (int i = 0; i < segmentCount; ++i)
                {
                    Body body = new Body(World);
                    body.BodyType = BodyType.Dynamic;
                    body.Position = new Vector2(161f + 2f * i, 0.125f);
                    Fixture fix = body.CreateFixture(shape);
                    fix.Friction = 0.6f;
                    JointFactory.CreateRevoluteJoint(World, prevBody, body, -Vector2.UnitX);

                    prevBody = body;
                    _bridgeSegments.Add(body);
                }
                JointFactory.CreateRevoluteJoint(World, _ground, prevBody, Vector2.UnitX);
            }

            // boxes
            {
                _boxes = new List<Body>();
                PolygonShape box = new PolygonShape(1f);
                box.SetAsBox(0.5f, 0.5f);
                _box =
                    new Sprite(ScreenManager.Assets.TextureFromShape(box, MaterialType.Squares, Color.SaddleBrown, 2f));

                Body body = new Body(World);
                body.BodyType = BodyType.Dynamic;
                body.Position = new Vector2(220f, -0.5f);
                body.CreateFixture(box);
                _boxes.Add(body);

                body = new Body(World);
                body.BodyType = BodyType.Dynamic;
                body.Position = new Vector2(220f, -1.5f);
                body.CreateFixture(box);
                _boxes.Add(body);

                body = new Body(World);
                body.BodyType = BodyType.Dynamic;
                body.Position = new Vector2(220f, -2.5f);
                body.CreateFixture(box);
                _boxes.Add(body);
            }

            // car
            {
                Vertices vertices = new Vertices(8);
                vertices.Add(new Vector2(-2.5f, 0.08f));
                vertices.Add(new Vector2(-2.375f, -0.46f));
                vertices.Add(new Vector2(-0.58f, -0.92f));
                vertices.Add(new Vector2(0.46f, -0.92f));
                vertices.Add(new Vector2(2.5f, -0.17f));
                vertices.Add(new Vector2(2.5f, 0.205f));
                vertices.Add(new Vector2(2.3f, 0.33f));
                vertices.Add(new Vector2(-2.25f, 0.35f));

                PolygonShape chassis = new PolygonShape(vertices, 2f);

                _car = new Body(World);
                _car.BodyType = BodyType.Dynamic;
                _car.Position = new Vector2(0.0f, -1.0f);
                _car.CreateFixture(chassis);

                _wheelBack = new Body(World);
                _wheelBack.BodyType = BodyType.Dynamic;
                _wheelBack.Position = new Vector2(-1.709f, -0.78f);
                Fixture fix = _wheelBack.CreateFixture(new CircleShape(0.5f, 0.8f));
                fix.Friction = 0.9f;

                _wheelFront = new Body(World);
                _wheelFront.BodyType = BodyType.Dynamic;
                _wheelFront.Position = new Vector2(1.54f, -0.8f);
                _wheelFront.CreateFixture(new CircleShape(0.5f, 1f));

                Vector2 axis = new Vector2(0.0f, -1.2f);
                _springBack = new LineJoint(_car, _wheelBack, _wheelBack.Position, axis);
                _springBack.MotorSpeed = 0.0f;
                _springBack.MaxMotorTorque = 20.0f;
                _springBack.MotorEnabled = true;
                _springBack.Frequency = _hzBack;
                _springBack.DampingRatio = _zeta;
                World.AddJoint(_springBack);

                _springFront = new LineJoint(_car, _wheelFront, _wheelFront.Position, axis);
                _springFront.MotorSpeed = 0.0f;
                _springFront.MaxMotorTorque = 10.0f;
                _springFront.MotorEnabled = false;
                _springFront.Frequency = _hzFront;
                _springFront.DampingRatio = _zeta;
                World.AddJoint(_springFront);

                _carBody = new Sprite(ScreenManager.Content.Load<Texture2D>("Samples/car"),
                                      AssetCreator.CalculateOrigin(_car) / _scale);
                _wheel = new Sprite(ScreenManager.Content.Load<Texture2D>("Samples/wheel"));
            }

            Camera.MinRotation = -0.05f;
            Camera.MaxRotation = 0.05f;

            Camera.TrackingBody = _car;
            Camera.EnableTracking = true;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            _springBack.MotorSpeed = Math.Sign(_acceleration) *
                                     MathHelper.SmoothStep(0f, _maxSpeed, Math.Abs(_acceleration));
            if (Math.Abs(_springBack.MotorSpeed) < _maxSpeed * 0.06f)
            {
                _springBack.MotorEnabled = false;
            }
            else
            {
                _springBack.MotorEnabled = true;
            }
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void HandleInput(InputHelper input, GameTime gameTime)
        {
            if (input.VirtualState.ThumbSticks.Left.X > 0.5f)
            {
                _acceleration = Math.Min(_acceleration + (float)(2.0 * gameTime.ElapsedGameTime.TotalSeconds), 1f);
            }
            else if (input.VirtualState.ThumbSticks.Left.X < -0.5f)
            {
                _acceleration = Math.Max(_acceleration - (float)(2.0 * gameTime.ElapsedGameTime.TotalSeconds), -1f);
            }
            else if (input.VirtualState.Buttons.A == ButtonState.Pressed)
            {
                _acceleration = 0f;
            }
            else
            {
                _acceleration -= Math.Sign(_acceleration) * (float)(2.0 * gameTime.ElapsedGameTime.TotalSeconds);
            }

            base.HandleInput(input, gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin(0, null, null, null, null, null, Camera.View);
            // draw car
            ScreenManager.SpriteBatch.Draw(_wheel.Texture, ConvertUnits.ToDisplayUnits(_wheelBack.Position), null,
                                           Color.White, _wheelBack.Rotation, _wheel.Origin, _scale, SpriteEffects.None,
                                           0f);
            ScreenManager.SpriteBatch.Draw(_wheel.Texture, ConvertUnits.ToDisplayUnits(_wheelFront.Position), null,
                                           Color.White, _wheelFront.Rotation, _wheel.Origin, _scale, SpriteEffects.None,
                                           0f);
            ScreenManager.SpriteBatch.Draw(_carBody.Texture, ConvertUnits.ToDisplayUnits(_car.Position), null,
                                           Color.White, _car.Rotation, _carBody.Origin, _scale, SpriteEffects.None, 0f);
            // draw teeter
            ScreenManager.SpriteBatch.Draw(_teeter.Texture, ConvertUnits.ToDisplayUnits(_board.Position), null,
                                           Color.White, _board.Rotation, _teeter.Origin, 1f, SpriteEffects.None, 0f);
            // draw bridge
            for (int i = 0; i < _bridgeSegments.Count; ++i)
            {
                ScreenManager.SpriteBatch.Draw(_bridge.Texture, ConvertUnits.ToDisplayUnits(_bridgeSegments[i].Position),
                                               null,
                                               Color.White, _bridgeSegments[i].Rotation, _bridge.Origin, 1f,
                                               SpriteEffects.None, 0f);
            }
            // draw boxes
            for (int i = 0; i < _boxes.Count; ++i)
            {
                ScreenManager.SpriteBatch.Draw(_box.Texture, ConvertUnits.ToDisplayUnits(_boxes[i].Position), null,
                                               Color.White, _boxes[i].Rotation, _box.Origin, 1f, SpriteEffects.None, 0f);
            }
            ScreenManager.SpriteBatch.End();

            ScreenManager.LineBatch.Begin(Camera.SimProjection, Camera.SimView);
            // draw ground
            for (int i = 0; i < _ground.FixtureList.Count; ++i)
            {
                ScreenManager.LineBatch.DrawLineShape(_ground.FixtureList[i].Shape, Color.Black);
            }
            ScreenManager.LineBatch.End();
            base.Draw(gameTime);
        }
    }
}