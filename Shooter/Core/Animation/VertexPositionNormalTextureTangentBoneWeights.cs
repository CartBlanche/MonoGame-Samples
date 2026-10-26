using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Shooter.Animation
{
    /// <summary>
    /// Skinned vertex: position, normal, UV, tangent (xyz + handedness in w) and four bone influences.
    /// Stride is 68 bytes.
    ///
    /// Layout (HLSL semantics):
    ///   offset  0  float3  POSITION0
    ///   offset 12  float3  NORMAL0
    ///   offset 24  float2  TEXCOORD0
    ///   offset 32  float4  TANGENT0        (w = +1/-1, bitangent = cross(normal, tangent.xyz) * tangent.w)
    ///   offset 48  byte4   BLENDINDICES0   (unsigned bytes, read in the shader as float4 0..255 - NOT normalised)
    ///   offset 52  float4  BLENDWEIGHT0    (sum == 1)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionNormalTextureTangentBoneWeights : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Vector4 Tangent;
        public Byte4 BoneIndices;
        public Vector4 BoneWeights;

        public const int SizeInBytes = 68;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Tangent, 0),
            new VertexElement(48, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0),
            new VertexElement(52, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0));

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        public VertexPositionNormalTextureTangentBoneWeights(
            Vector3 position, Vector3 normal, Vector2 uv, Vector4 tangent, Byte4 boneIndices, Vector4 boneWeights)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = uv;
            Tangent = tangent;
            BoneIndices = boneIndices;
            BoneWeights = boneWeights;
        }

        /// <summary>Unpacks the four bone indices (x = lowest byte).</summary>
        public Point4 GetBoneIndices()
        {
            uint p = BoneIndices.PackedValue;
            return new Point4((int)(p & 0xFF), (int)((p >> 8) & 0xFF), (int)((p >> 16) & 0xFF), (int)((p >> 24) & 0xFF));
        }

        public readonly struct Point4
        {
            public readonly int X, Y, Z, W;

            public Point4(int x, int y, int z, int w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }
        }
    }
}
