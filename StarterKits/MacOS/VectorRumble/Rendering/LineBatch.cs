#region File Description
//-----------------------------------------------------------------------------
// LineBatch.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace VectorRumble
{
    /// <summary>
    /// Batches line "draw" calls from the game, and renders them at one time.
    /// </summary>
    class LineBatch
    {
        #region Constants
        /// <summary>
        /// The maximum number of vertices in the LineBatch vertex array.
        /// </summary>
        const int maxVertexCount = 512;
        #endregion

        #region Fields
        /// <summary>
        /// The graphics device that renders the lines.
        /// </summary>
        GraphicsDevice graphicsDevice;
        
        /// <summary>
        /// The effect applied to the lines.
        /// </summary>
        BasicEffect effect;
        
        /// <summary>
        /// The vertex declaration which is defines the line vertices.
        /// </summary>
        VertexDeclaration vertexDeclaration;
        
        /// <summary>
        /// The line vertices.
        /// </summary>
        VertexPositionColor[] vertices;

        /// <summary>
        /// The current index being "drawn" into the array.
        /// </summary>
        int currentIndex;

        /// <summary>
        /// The current number of lines to be draw.
        /// </summary>
        int lineCount;
		
		///
		public static BlendState LineBlendState = new BlendState()
        {
			AlphaBlendFunction = BlendFunction.Add,
			ColorSourceBlend = Blend.SourceAlpha,
			ColorDestinationBlend = Blend.InverseSourceAlpha,
        };
        #endregion

        #region Initialization
        public LineBatch(GraphicsDevice graphicsDevice)
        {
            // assign the graphics device parameter after safety-checking
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice");
            }
            this.graphicsDevice = graphicsDevice;
			

            // create and configure the effect
            this.effect = new BasicEffect(graphicsDevice);
            this.effect.VertexColorEnabled = true;
            this.effect.TextureEnabled = false;
            this.effect.LightingEnabled = false;
            // configure the effect
            this.effect.World = Matrix.Identity;
            this.effect.View = Matrix.CreateLookAt(Vector3.Zero, Vector3.Forward,
                Vector3.Up);

			var vd = VertexPositionColor.VertexDeclaration.GetVertexElements();
            // create the vertex declaration
            this.vertexDeclaration = new VertexDeclaration(vd);

            // create the vertex array
            this.vertices = new VertexPositionColor[maxVertexCount];
        }

        /// <summary>
        /// Set the new projection for the line batch effect.
        /// </summary>
        /// <param name="projection"></param>
        public void SetProjection(Matrix projection)
        {
            if (effect != null)
            {
                effect.Projection = projection;
            }
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Configures the object and the device to begin drawing lines.
        /// </summary>
        public void Begin()
        {
            // reset the counters
            currentIndex = 0;
            lineCount = 0;
        }


        /// <summary>
        /// Draw a line from one point to another with the same color.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        /// <param name="color">The color throughout the line.</param>
        public void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            DrawLine(
                new VertexPositionColor(new Vector3(start, 0f), color),
                new VertexPositionColor(new Vector3(end, 0f), color));
        }


        /// <summary>
        /// Draw a line from one point to another with different colors at each end.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        /// <param name="startColor">The color at the starting point.</param>
        /// <param name="endColor">The color at the ending point.</param>
        public void DrawLine(Vector2 start, Vector2 end, 
            Color startColor, Color endColor)
        {
            DrawLine(
                new VertexPositionColor(new Vector3(start, 0f), startColor),
                new VertexPositionColor(new Vector3(end, 0f), endColor));
        }


        /// <summary>
        /// Draws a line from one vertex to another.
        /// </summary>
        /// <param name="start">The starting vertex.</param>
        /// <param name="end">The ending vertex.</param>
        public void DrawLine(VertexPositionColor start, VertexPositionColor end)
        {
            if (currentIndex >= (vertices.Length - 2))
            {
                End();
                Begin();
            }

            vertices[currentIndex++] = start;
            vertices[currentIndex++] = end;

            lineCount++;
        }


        /// <summary>
        /// Draws the given polygon.
        /// </summary>
        /// <param name="polygon">The polygon to render.</param>
        /// <param name="color">The color to use when drawing the polygon.</param>
        public void DrawPolygon(VectorPolygon polygon, Color color)
        {
            DrawPolygon(polygon, color, false);
        }


        /// <summary>
        /// Draws the given polygon.
        /// </summary>
        /// <param name="polygon">The polygon to render.</param>
        /// <param name="color">The color to use when drawing the polygon.</param>
        /// <param name="dashed">If true, the polygon will be "dashed".</param>
        public void DrawPolygon(VectorPolygon polygon, Color color, bool dashed)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException("polygon");
            }
            int step = (dashed == true) ? 2 : 1;
            for (int i = 0; i < polygon.TransformedPoints.Length; i += step)
            {
                if (currentIndex >= vertices.Length - 2)
                {
                    End();
                    Begin();
                }
                vertices[currentIndex].Position.X = 
                    polygon.TransformedPoints[i % polygon.TransformedPoints.Length].X;
                vertices[currentIndex].Position.Y = 
                    polygon.TransformedPoints[i % polygon.TransformedPoints.Length].Y;
                vertices[currentIndex++].Color = color;
                vertices[currentIndex].Position.X = 
                    polygon.TransformedPoints[(i + 1) % 
                        polygon.TransformedPoints.Length].X;
                vertices[currentIndex].Position.Y =
                    polygon.TransformedPoints[(i + 1) % 
                        polygon.TransformedPoints.Length].Y;
                vertices[currentIndex++].Color = color;
                lineCount++;
            }
        }

        /// <summary>
        /// Draws the given polygon, in defined segments.
        /// </summary>
        /// <param name="aPolygon">The polygon to render.</param>
        /// <param name="aColor">The color to use when drawing the polygon.</param>
        /// <param name="aDashed">If true, the polygon will be "dashed".</param>
        /// <param name="aStartSeg">Start of segment drawing.</param>
        /// <param name="aEndSeg">End of segment drawing.</param>
        public void DrawPolygonSegments(VectorPolygon aPolygon, Color aColor, bool aDashed, int aStartSeg, int aEndSeg)
        {
            if (aPolygon == null || aEndSeg > aPolygon.TransformedPoints.Length)
            {
                throw new ArgumentNullException("polygon");
            }
            int step = (aDashed == true) ? 2 : 1;
            for (int i = aStartSeg; i < aEndSeg; i += step)
            {
                if (currentIndex >= vertices.Length - 2)
                {
                    End();
                    Begin();
                }
                vertices[currentIndex].Position.X =
                    aPolygon.TransformedPoints[i % aPolygon.TransformedPoints.Length].X;
                vertices[currentIndex].Position.Y =
                    aPolygon.TransformedPoints[i % aPolygon.TransformedPoints.Length].Y;
                vertices[currentIndex++].Color = aColor;
                vertices[currentIndex].Position.X =
                    aPolygon.TransformedPoints[(i + 1) %
                        aPolygon.TransformedPoints.Length].X;
                vertices[currentIndex].Position.Y =
                    aPolygon.TransformedPoints[(i + 1) %
                        aPolygon.TransformedPoints.Length].Y;
                vertices[currentIndex++].Color = aColor;
                lineCount++;
            }
        }

        /// <summary>
        /// Ends the batch of lines, submitting them to the graphics device.
        /// </summary>
        public void End()
        {
            // if we don't have any vertices, then we can exit early
            if (currentIndex == 0)
            {
                return;
            }

            // configure the graphics device to render our lines			
            graphicsDevice.BlendState = LineBlendState;

            // run the effect
			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				graphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, vertices, 0, lineCount);
			}
        }
        #endregion

        #region Destruction
        /// <summary>
        /// Disposes of the object, cleanly releasing graphics resources.
        /// </summary>
        public void Dispose()
        {
            if (effect != null)
            {
                effect.Dispose();
                effect = null;
            }
            if (vertexDeclaration != null)
            {
                vertexDeclaration.Dispose();
                vertexDeclaration = null;
            }
        }
        #endregion
    }
}
