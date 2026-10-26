using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Shooter.Animation
{
    /// <summary>
    /// Reads SkinnedModel from .xnb. Pairs with Shooter.Pipeline.SkinnedModelWriter - keep the binary layout in sync
    /// (see SKINNING-NOTES.md). If no IGraphicsDeviceService is registered (tools/tests) only CPU data is created.
    /// </summary>
    public sealed class SkinnedModelReader : ContentTypeReader<SkinnedModel>
    {
        public const int FormatVersion = 1;

        protected override SkinnedModel Read(ContentReader input, SkinnedModel existingInstance)
        {
            int version = input.ReadInt32();
            if (version != FormatVersion)
            {
                throw new InvalidOperationException("Unsupported SkinnedModel format version " + version);
            }

            SkinnedModel model = new SkinnedModel();
            model.Name = input.ReadString();

            int boneCount = input.ReadInt32();
            model.BoneNames = new string[boneCount];
            model.ParentIndices = new int[boneCount];
            model.BindLocalPose = new Matrix[boneCount];
            model.InverseBindPose = new Matrix[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                model.BoneNames[i] = input.ReadString();
                model.ParentIndices[i] = input.ReadInt32();
                model.BindLocalPose[i] = input.ReadMatrix();
                model.InverseBindPose[i] = input.ReadMatrix();
            }

            int partCount = input.ReadInt32();
            model.Parts = new SkinnedMeshPart[partCount];
            IGraphicsDeviceService gds = input.ContentManager.ServiceProvider != null
                ? input.ContentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService
                : null;

            for (int p = 0; p < partCount; p++)
            {
                SkinnedMeshPart part = new SkinnedMeshPart();
                part.Name = input.ReadString();
                part.MaterialName = input.ReadString();
                part.DiffuseTexture = input.ReadString();

                int vc = input.ReadInt32();
                part.Vertices = new VertexPositionNormalTextureTangentBoneWeights[vc];
                Vector3 min = new Vector3(float.MaxValue);
                Vector3 max = new Vector3(float.MinValue);
                for (int v = 0; v < vc; v++)
                {
                    VertexPositionNormalTextureTangentBoneWeights vert;
                    vert.Position = input.ReadVector3();
                    vert.Normal = input.ReadVector3();
                    vert.TextureCoordinate = input.ReadVector2();
                    vert.Tangent = input.ReadVector4();
                    byte b0 = input.ReadByte();
                    byte b1 = input.ReadByte();
                    byte b2 = input.ReadByte();
                    byte b3 = input.ReadByte();
                    vert.BoneIndices = new Byte4 { PackedValue = (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24)) };
                    vert.BoneWeights = input.ReadVector4();
                    part.Vertices[v] = vert;
                    min = Vector3.Min(min, vert.Position);
                    max = Vector3.Max(max, vert.Position);
                }

                part.Bounds = new BoundingBox(min, max);

                int ic = input.ReadInt32();
                bool wide = input.ReadBoolean();
                part.Indices = new int[ic];
                for (int i = 0; i < ic; i++)
                {
                    part.Indices[i] = wide ? input.ReadInt32() : input.ReadUInt16();
                }

                if (gds != null && gds.GraphicsDevice != null)
                {
                    part.CreateBuffers(gds.GraphicsDevice);
                }

                model.Parts[p] = part;
            }

            model.Initialize();
            return model;
        }
    }
}
