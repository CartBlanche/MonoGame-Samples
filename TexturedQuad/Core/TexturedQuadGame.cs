using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;

namespace TexturedQuad
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class TexturedQuadGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private SpriteFont font;
        VertexPositionNormalTexture[] cubeVertices;
        short[] cubeIndices;
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;
        int[] faceTextureIndices = new int[6];
        VertexDeclaration vertexDeclaration;
        Matrix View, Projection;
        Texture2D[] textures;
        BasicEffect cubeEffect;
        float rotation = 0f;
        KeyboardState previousKeyboardState;

        /// <summary>
        /// Initializes a new instance of the <see cref="TexturedQuadGame"/> class and sets up the graphics device manager and content root.
        /// </summary>
        public TexturedQuadGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Initializes the game, sets up the cube geometry, view, and projection matrices.
        /// </summary>
        protected override void Initialize()
        {
            // Set up cube geometry (24 vertices, 36 indices)
            cubeVertices = new VertexPositionNormalTexture[24];
            cubeIndices = new short[36];
            CreateCubeGeometry();
            vertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.VertexDeclaration, 24, BufferUsage.WriteOnly);
            vertexBuffer.SetData(cubeVertices);
            indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, 36, BufferUsage.WriteOnly);
            indexBuffer.SetData(cubeIndices);
            for (int i = 0; i < 6; i++) faceTextureIndices[i] = i % 4;
            View = Matrix.CreateLookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.Up);
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 4.0f / 3.0f, 1, 500);
            base.Initialize();
        }

        /// <summary>
        /// Loads all game content, including the four textures and sets up the effect and vertex declaration.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            font = Content.Load<SpriteFont>("font");

            textures = new Texture2D[4];
            textures[0] = Content.Load<Texture2D>("Glass");
            textures[1] = Content.Load<Texture2D>("GlassPane");
            textures[2] = Content.Load<Texture2D>("GlassPane1");
            textures[3] = Content.Load<Texture2D>("GlassPane2");

            cubeEffect = new BasicEffect(graphics.GraphicsDevice);
            cubeEffect.World = Matrix.Identity;
            cubeEffect.View = View;
            cubeEffect.Projection = Projection;
            cubeEffect.TextureEnabled = true;

            vertexDeclaration = new VertexDeclaration(new VertexElement[]
                {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                    new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
                }
            );
        }

        /// <summary>
        /// Updates the game logic, handles input for exiting, switching textures, and rotates the cube.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
#if !__IOS__
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();
#endif
            // Change texture on Space key release
            KeyboardState state = Keyboard.GetState();
            if ((state.IsKeyUp(Keys.Space) && previousKeyboardState.IsKeyDown(Keys.Space))
            || (GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed))
            {
                // Advance the current texture index for all faces
                int next = (faceTextureIndices[0] + 1) % textures.Length;
                for (int i = 0; i < faceTextureIndices.Length; i++)
                    faceTextureIndices[i] = next;
            }
            previousKeyboardState = state;

            // Rotate the cube
            rotation += (float)gameTime.ElapsedGameTime.TotalSeconds;
            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the current frame, rendering the rotating cube with the selected texture on all faces.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.MonoGameOrange);

            Matrix world = Matrix.CreateRotationY(rotation) * Matrix.CreateRotationX(rotation * 0.7f);
            cubeEffect.View = View;
            cubeEffect.Projection = Projection;
            // All faces use the same texture (current index)
            int currentTexture = faceTextureIndices[0];
            cubeEffect.World = world;
            cubeEffect.Texture = textures[currentTexture];
            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            GraphicsDevice.Indices = indexBuffer;
            foreach (EffectPass pass in cubeEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
            }

            spriteBatch.Begin();
            spriteBatch.DrawString(font, "Press Space to change texture", new Vector2(20, 20), Color.Black);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Creates the geometry for a unit cube centered at the origin.
        /// </summary>
        private void CreateCubeGeometry()
        {
            // 24 vertices (4 per face) so each face can have its own texture coordinates
            Vector3[] faceNormals = new Vector3[] {
                Vector3.Forward, Vector3.Backward, Vector3.Left, Vector3.Right, Vector3.Up, Vector3.Down
            };
            Vector3[][] faceVerts = new Vector3[][] {
                // Front
                new Vector3[] { new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f) },
                // Back
                new Vector3[] { new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f) },
                // Left
                new Vector3[] { new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f) },
                // Right
                new Vector3[] { new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f) },
                // Top
                new Vector3[] { new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f) },
                // Bottom
                new Vector3[] { new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f) }
            };
            Vector2[] texCoords = new Vector2[] {
                new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)
            };
            for (int face = 0; face < 6; face++)
            {
                for (int v = 0; v < 4; v++)
                {
                    cubeVertices[face * 4 + v] = new VertexPositionNormalTexture(
                        faceVerts[face][v], faceNormals[face], texCoords[v]);
                }
            }
            // Indices for 12 triangles (2 per face)
            short[] inds = new short[] {
                0,1,2, 0,2,3,      // Front
                4,5,6, 4,6,7,      // Back
                8,9,10, 8,10,11,   // Left
                12,13,14, 12,14,15,// Right
                16,17,18, 16,18,19,// Top
                20,21,22, 20,22,23 // Bottom
            };
            for (int i = 0; i < 36; i++) cubeIndices[i] = inds[i];
        }
    }
}