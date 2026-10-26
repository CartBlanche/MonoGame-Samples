using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter.Animation
{
    /// <summary>One draw call worth of skinned geometry (one FBX geometry = one material).</summary>
    public sealed class SkinnedMeshPart
    {
        public string Name;
        public string MaterialName;

        /// <summary>Texture file name recorded in the FBX material (informational; textures are separate content).</summary>
        public string DiffuseTexture;

        public VertexPositionNormalTextureTangentBoneWeights[] Vertices;
        public int[] Indices;
        public BoundingBox Bounds;

        public VertexBuffer VertexBuffer;
        public IndexBuffer IndexBuffer;

        public int PrimitiveCount
        {
            get { return Indices.Length / 3; }
        }

        /// <summary>Creates GPU buffers. Called by the reader when a graphics device service is available.</summary>
        public void CreateBuffers(GraphicsDevice device)
        {
            VertexBuffer = new VertexBuffer(
                device, VertexPositionNormalTextureTangentBoneWeights.VertexDeclaration, Vertices.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(Vertices);

            if (Vertices.Length <= ushort.MaxValue)
            {
                ushort[] idx = new ushort[Indices.Length];
                for (int i = 0; i < idx.Length; i++)
                {
                    idx[i] = (ushort)Indices[i];
                }

                IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, idx.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(idx);
            }
            else
            {
                IndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, Indices.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(Indices);
            }
        }

        /// <summary>Sets buffers and issues DrawIndexedPrimitives. The caller applies the effect pass first.</summary>
        public void Draw(GraphicsDevice device)
        {
            if (VertexBuffer == null)
            {
                return;
            }

            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, PrimitiveCount);
        }
    }

    /// <summary>
    /// Skinned mesh + skeleton. All transforms are row-vector (XNA) matrices in MonoGame world space
    /// (see SKINNING-NOTES.md: MonoGame space = RotationY(180) of the FBX data, scaled 0.01).
    /// Bones are listed parent-first, so GlobalPose[i] can be computed in a single forward pass.
    /// </summary>
    public sealed class SkinnedModel
    {
        /// <summary>Size of the bone matrix palette the vertex format / shader is assumed to support.</summary>
        public const int MaxBones = 64;

        public string Name;
        public string[] BoneNames;
        public int[] ParentIndices;

        /// <summary>Bind pose, local space (relative to parent), per bone.</summary>
        public Matrix[] BindLocalPose;

        /// <summary>Inverse of the bind pose in model space, per bone (skin = InverseBindPose * currentGlobal).</summary>
        public Matrix[] InverseBindPose;

        public Vector3[] BindTranslation;
        public Quaternion[] BindRotation;
        public Vector3[] BindScale;

        public SkinnedMeshPart[] Parts;
        public BoundingBox Bounds;

        private Dictionary<string, int> _boneLookup;

        public int BoneCount
        {
            get { return BoneNames.Length; }
        }

        public int GetBoneIndex(string name)
        {
            int i;
            return _boneLookup.TryGetValue(name, out i) ? i : -1;
        }

        /// <summary>Fills decomposed bind data, lookup tables and bounds. Called by the reader.</summary>
        public void Initialize()
        {
            int n = BoneNames.Length;
            if (n > MaxBones)
            {
                throw new InvalidOperationException(
                    "Skeleton '" + Name + "' has " + n + " bones; the supported palette is " + MaxBones + ".");
            }

            _boneLookup = new Dictionary<string, int>(n, StringComparer.Ordinal);
            BindTranslation = new Vector3[n];
            BindRotation = new Quaternion[n];
            BindScale = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                _boneLookup[BoneNames[i]] = i;
                Vector3 s;
                Quaternion r;
                Vector3 t;
                BindLocalPose[i].Decompose(out s, out r, out t);
                BindScale[i] = s;
                BindRotation[i] = r;
                BindTranslation[i] = t;
            }

            bool first = true;
            for (int p = 0; p < Parts.Length; p++)
            {
                if (first)
                {
                    Bounds = Parts[p].Bounds;
                    first = false;
                }
                else
                {
                    Bounds = BoundingBox.CreateMerged(Bounds, Parts[p].Bounds);
                }
            }
        }

        /// <summary>Bind pose in model space for every bone.</summary>
        public Matrix[] ComputeBindGlobalPose()
        {
            Matrix[] g = new Matrix[BoneCount];
            for (int i = 0; i < g.Length; i++)
            {
                g[i] = ParentIndices[i] < 0 ? BindLocalPose[i] : BindLocalPose[i] * g[ParentIndices[i]];
            }

            return g;
        }

        /// <summary>CPU skinning of one vertex (used by tools/tests; the game skins on the GPU).</summary>
        public static Vector3 SkinPosition(
            ref VertexPositionNormalTextureTangentBoneWeights v, Matrix[] skinMatrices)
        {
            uint p = v.BoneIndices.PackedValue;
            Vector3 result = Vector3.Zero;
            float[] w = { v.BoneWeights.X, v.BoneWeights.Y, v.BoneWeights.Z, v.BoneWeights.W };
            for (int k = 0; k < 4; k++)
            {
                if (w[k] <= 0f)
                {
                    continue;
                }

                int bi = (int)((p >> (8 * k)) & 0xFF);
                result += Vector3.Transform(v.Position, skinMatrices[bi]) * w[k];
            }

            return result;
        }
    }
}
