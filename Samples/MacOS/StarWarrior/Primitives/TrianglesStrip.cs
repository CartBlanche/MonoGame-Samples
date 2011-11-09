using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarWarrior.Primitives
{
    public class TrianglesStrip
    {
        PrimitiveBatch batch;
        List<Vector2> point = new List<Vector2>();
        Color color = Color.White;
        bool fill = true;
        RasterizerState state;
        GraphicsDevice device;


        public TrianglesStrip(GraphicsDevice device,PrimitiveBatch primitiveBatch)
        {
            this.device = device;
            this.batch = primitiveBatch;
            state = new RasterizerState();
            state.CullMode = CullMode.CullCounterClockwiseFace;
            state.FillMode = FillMode.WireFrame;
        }

        public void AddPoint(float x1, float y1)
        {
            point.Add(new Vector2(x1,y1));            
        }

        public void SetColor(Color color)
        {
            this.color = color;
        }

        public void SetFillMode(bool fill)
        {
            this.fill = fill;
        }

        public void Draw(Vector2 transform)
        {
            if (fill == false)
            {
                device.RasterizerState = state;
            }

            if (point.Count < 3)
            {
                throw new InvalidOperationException("precisa de 3 pontos pelo menos para poder desenhar os triangulos");
            }

            batch.Begin(PrimitiveType.TriangleStrip);
            foreach (var item in point)
            {
                batch.AddVertex(item + transform, color);       
            }
            batch.End();

            if (fill == false)
            {
                device.RasterizerState = RasterizerState.CullCounterClockwise;
            }
        }

    }
}
