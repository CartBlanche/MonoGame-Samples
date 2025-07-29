using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Colored3DCube
{
	public class Game1 : Game
	{

		GraphicsDeviceManager graphics;
		KeyboardState currentKeys;
		BasicEffect basicEffect;

		Matrix worldMatrix, viewMatrix, projectionMatrix;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";

			graphics.PreferredBackBufferWidth = 480;
			graphics.PreferredBackBufferHeight = 640;

#if ___MOBILE___
			graphics.IsFullScreen = true;
#endif
		}

		protected override void Initialize()
		{
			base.Initialize();
		}

		protected override void LoadContent()
		{

			// setup our graphics scene matrices
			worldMatrix = Matrix.Identity;
			viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up);
			projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, 10);

			// Setup our basic effect
			basicEffect = new BasicEffect(GraphicsDevice);
			basicEffect.World = worldMatrix;
			basicEffect.View = viewMatrix;
			basicEffect.Projection = projectionMatrix;
			basicEffect.VertexColorEnabled = true;

			CreateCubeVertexBuffer();
			CreateCubeIndexBuffer();
		}

		protected override void UnloadContent()
		{
		}

		protected override void Update(GameTime gameTime)
		{
			currentKeys = Keyboard.GetState();

			//Press Esc To Exit
			if (currentKeys.IsKeyDown(Keys.Escape))
				this.Exit();

			// --- Scaling logic ---
			// Calculate scale based on the smaller of width/height, so cube fits in window
			float minDim = Math.Min(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
			float scale = minDim / 640f; // 640 is the original height, adjust as needed

			// --- Rotation logic ---
			float rotX = 0f, rotY = 0f;
			if (currentKeys.IsKeyDown(Keys.Up))
				rotX -= 0.05f;
			if (currentKeys.IsKeyDown(Keys.Down))
				rotX += 0.05f;
			if (currentKeys.IsKeyDown(Keys.Left))
				rotY -= 0.05f;
			if (currentKeys.IsKeyDown(Keys.Right))
				rotY += 0.05f;

			// Apply rotation to a persistent matrix
			// Store rotation state between frames
			if (rotX != 0f || rotY != 0f)
			{
				rotationMatrix *= Matrix.CreateRotationX(rotX) * Matrix.CreateRotationY(rotY);
			}

			// Combine scaling and rotation for worldMatrix
			worldMatrix = Matrix.CreateScale(scale) * rotationMatrix;

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.MonoGameOrange);

			GraphicsDevice.SetVertexBuffer(vertices);
			GraphicsDevice.Indices = indices;

			//RasterizerState rasterizerState1 = new RasterizerState ();
			//rasterizerState1.CullMode = CullMode.None;
			//graphics.GraphicsDevice.RasterizerState = rasterizerState1;

			basicEffect.World = worldMatrix;

			foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
			{
				pass.Apply();

				GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, number_of_indices / 3);

			}
			base.Draw(gameTime);
		}

		const int number_of_vertices = 8;
		const int number_of_indices = 36;
		VertexBuffer vertices;

		// Store rotation state between frames
		Matrix rotationMatrix = Matrix.Identity;

		void CreateCubeVertexBuffer()
		{
			VertexPositionColor[] cubeVertices = new VertexPositionColor[number_of_vertices];

			cubeVertices[0].Position = new Vector3(-1, -1, -1);
			cubeVertices[1].Position = new Vector3(-1, -1, 1);
			cubeVertices[2].Position = new Vector3(1, -1, 1);
			cubeVertices[3].Position = new Vector3(1, -1, -1);
			cubeVertices[4].Position = new Vector3(-1, 1, -1);
			cubeVertices[5].Position = new Vector3(-1, 1, 1);
			cubeVertices[6].Position = new Vector3(1, 1, 1);
			cubeVertices[7].Position = new Vector3(1, 1, -1);

			cubeVertices[0].Color = Color.Black;
			cubeVertices[1].Color = Color.Red;
			cubeVertices[2].Color = Color.Yellow;
			cubeVertices[3].Color = Color.Green;
			cubeVertices[4].Color = Color.Blue;
			cubeVertices[5].Color = Color.Magenta;
			cubeVertices[6].Color = Color.White;
			cubeVertices[7].Color = Color.Cyan;

			vertices = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, number_of_vertices, BufferUsage.WriteOnly);
			vertices.SetData<VertexPositionColor>(cubeVertices);
		}

		IndexBuffer indices;

		void CreateCubeIndexBuffer()
		{
			UInt16[] cubeIndices = new UInt16[number_of_indices];

			//bottom face
			cubeIndices[0] = 0;
			cubeIndices[1] = 2;
			cubeIndices[2] = 3;
			cubeIndices[3] = 0;
			cubeIndices[4] = 1;
			cubeIndices[5] = 2;

			//top face
			cubeIndices[6] = 4;
			cubeIndices[7] = 6;
			cubeIndices[8] = 5;
			cubeIndices[9] = 4;
			cubeIndices[10] = 7;
			cubeIndices[11] = 6;

			//front face
			cubeIndices[12] = 5;
			cubeIndices[13] = 2;
			cubeIndices[14] = 1;
			cubeIndices[15] = 5;
			cubeIndices[16] = 6;
			cubeIndices[17] = 2;

			//back face
			cubeIndices[18] = 0;
			cubeIndices[19] = 7;
			cubeIndices[20] = 4;
			cubeIndices[21] = 0;
			cubeIndices[22] = 3;
			cubeIndices[23] = 7;

			//left face
			cubeIndices[24] = 0;
			cubeIndices[25] = 4;
			cubeIndices[26] = 1;
			cubeIndices[27] = 1;
			cubeIndices[28] = 4;
			cubeIndices[29] = 5;

			//right face
			cubeIndices[30] = 2;
			cubeIndices[31] = 6;
			cubeIndices[32] = 3;
			cubeIndices[33] = 3;
			cubeIndices[34] = 6;
			cubeIndices[35] = 7;

			indices = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, number_of_indices, BufferUsage.WriteOnly);
			indices.SetData<UInt16>(cubeIndices);

		}
	}
}