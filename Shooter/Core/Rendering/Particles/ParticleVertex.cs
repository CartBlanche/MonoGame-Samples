using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter.Rendering.Particles
{
    /// <summary>Billboard particle vertex (matches VSInBillboard in Effects/Particles.fx). Stride 48.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ParticleVertex : IVertexType
    {
        public Vector3 Position;
        public Vector4 Color;       // linear rgb + alpha (may be HDR)
        public Vector2 UV;
        public Vector3 UV2Blend;    // second flip-book frame uv + blend factor (unused: no material enables flip-book blending)

        public static readonly VertexDeclaration Declaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(36, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1));

        VertexDeclaration IVertexType.VertexDeclaration => Declaration;
    }

    /// <summary>The static-model vertex layout used by the content pipeline (stride 56), used for the built-in particle cube.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct StaticMeshVertex : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public Vector3 Tangent;
        public Vector3 Binormal;

        public static readonly VertexDeclaration Declaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(32, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
            new VertexElement(44, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0));

        VertexDeclaration IVertexType.VertexDeclaration => Declaration;
    }
}
