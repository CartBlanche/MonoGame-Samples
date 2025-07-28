//-----------------------------------------------------------------------------
// CustomVertex.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace UseCustomVertex
{
    //[StructLayout(LayoutKind.Sequential)]
    public struct CustomVertex : IVertexType
    {
        Vector3 vertexPosition;
        Vector2 vertexTextureCoordinate;

        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        );

        //The constructor for the custom vertex. This allows similar 
        //initialization of custom vertex arrays as compared to arrays of a 
        //standard vertex type, such as VertexPositionColor.
        public CustomVertex(Vector3 pos, Vector2 textureCoordinate)
        {
            vertexPosition = pos;
            vertexTextureCoordinate = textureCoordinate;
        }

        //Public methods for accessing the components of the custom vertex.
        public Vector3 Position
        {
            get { return vertexPosition; }
            set { vertexPosition = value; }
        }

        public Vector2 TextureCoordinate
        {
            get { return vertexTextureCoordinate; }
            set { vertexTextureCoordinate = value; }
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }
}
