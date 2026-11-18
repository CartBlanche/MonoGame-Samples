using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>One vertex of a skinned part (68 bytes). Joints are palette slots of the owning part.</summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SkinnedVertex : IVertexType
    {
        public Vector3 Position; public Vector3 Normal; public Vector4 Tangent; public Vector2 UV;
        public Microsoft.Xna.Framework.Graphics.PackedVector.Byte4 Joints; public Vector4 Weights;
        public static readonly VertexDeclaration Declaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.Tangent, 0),
            new VertexElement(40, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(48, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0),
            new VertexElement(52, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0));
        VertexDeclaration IVertexType.VertexDeclaration => Declaration;
    }

    public sealed class SkinnedPart
    {
        public string Material;
        public VertexBuffer Vertices; public IndexBuffer Indices; public int PrimitiveCount;
        public int[] Palette; // skeleton bone per palette slot
    }

    /// <summary>Per-bone keys of a clip: length 0 = bind pose, 1 = constant, FrameCount = animated.</summary>
    public sealed class SkinnedTrack { public Vector3[] T; public Quaternion[] R; public Vector3[] S; }

    public sealed class SkinnedClip
    {
        public string Name; public bool Loop; public float Duration, Fps; public int FrameCount;
        public SkinnedTrack[] Tracks;
    }

    public sealed class SkinnedModel
    {
        public string Name;
        public string[] BoneNames; public int[] Parent;
        public Vector3[] BindT; public Quaternion[] BindR; public Vector3[] BindS;
        public Matrix[] InverseBind;
        public List<SkinnedPart> Parts = new List<SkinnedPart>();
        public Dictionary<string, SkinnedClip> Clips = new Dictionary<string, SkinnedClip>();
        public int BoneIndex(string name) => Array.IndexOf(BoneNames, name);
    }

    /// <summary>Reads Models/player.xnb written by PipelineExtension.SkinnedModelWriter (FormatVersion 1).</summary>
    public sealed class SkinnedModelReader : ContentTypeReader<SkinnedModel>
    {
        static Vector3 V3(ContentReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        static Quaternion Q(ContentReader r) => new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        protected override SkinnedModel Read(ContentReader r, SkinnedModel existing)
        {
            int version = r.ReadInt32();
            if (version != 1) throw new InvalidOperationException("SkinnedModel format " + version);
            var m = new SkinnedModel { Name = r.ReadString() };
            int nb = r.ReadInt32();
            m.BoneNames = new string[nb]; m.Parent = new int[nb]; m.BindT = new Vector3[nb]; m.BindR = new Quaternion[nb]; m.BindS = new Vector3[nb]; m.InverseBind = new Matrix[nb];
            for (int i = 0; i < nb; i++)
            {
                m.BoneNames[i] = r.ReadString(); m.Parent[i] = r.ReadInt32();
                m.BindT[i] = V3(r); m.BindR[i] = Q(r); m.BindS[i] = V3(r);
                m.InverseBind[i] = new Matrix(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(),
                                              r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            }
            var gds = (IGraphicsDeviceService)r.ContentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
            int np = r.ReadInt32();
            for (int i = 0; i < np; i++)
            {
                var p = new SkinnedPart { Material = r.ReadString() };
                int vc = r.ReadInt32(); int bytes = r.ReadInt32();
                var verts = new SkinnedVertex[vc];
                for (int v = 0; v < vc; v++)
                {
                    verts[v].Position = V3(r); verts[v].Normal = V3(r);
                    verts[v].Tangent = new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    verts[v].UV = new Vector2(r.ReadSingle(), r.ReadSingle());
                    byte b0 = r.ReadByte(), b1 = r.ReadByte(), b2 = r.ReadByte(), b3 = r.ReadByte();
                    verts[v].Joints = new Microsoft.Xna.Framework.Graphics.PackedVector.Byte4(b0, b1, b2, b3);
                    verts[v].Weights = new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                }
                int ic = r.ReadInt32(); var idx = new int[ic];
                for (int k = 0; k < ic; k++) idx[k] = r.ReadInt32();
                int pc = r.ReadInt32(); p.Palette = new int[pc];
                for (int k = 0; k < pc; k++) p.Palette[k] = r.ReadInt32();
                p.PrimitiveCount = ic / 3;
                if (gds != null) // headless loads (self-test, AI) have no graphics device: keep skeleton + clips only
                {
                    p.Vertices = new VertexBuffer(gds.GraphicsDevice, SkinnedVertex.Declaration, vc, BufferUsage.WriteOnly); p.Vertices.SetData(verts);
                    p.Indices = new IndexBuffer(gds.GraphicsDevice, IndexElementSize.ThirtyTwoBits, ic, BufferUsage.WriteOnly); p.Indices.SetData(idx);
                }
                m.Parts.Add(p);
            }
            int nc = r.ReadInt32();
            for (int c = 0; c < nc; c++)
            {
                var clip = new SkinnedClip { Name = r.ReadString(), Loop = r.ReadBoolean(), Duration = r.ReadSingle(), FrameCount = r.ReadInt32(), Fps = r.ReadSingle(), Tracks = new SkinnedTrack[nb] };
                for (int b = 0; b < nb; b++)
                {
                    byte flags = r.ReadByte(); var t = new SkinnedTrack { T = new Vector3[0], R = new Quaternion[0], S = new Vector3[0] };
                    if ((flags & 1) != 0) { int n = r.ReadInt32() / 3; t.T = new Vector3[n]; for (int k = 0; k < n; k++) t.T[k] = V3(r); }
                    if ((flags & 2) != 0) { int n = r.ReadInt32() / 4; t.R = new Quaternion[n]; for (int k = 0; k < n; k++) t.R[k] = Q(r); }
                    if ((flags & 4) != 0) { int n = r.ReadInt32() / 3; t.S = new Vector3[n]; for (int k = 0; k < n; k++) t.S[k] = V3(r); }
                    clip.Tracks[b] = t;
                }
                m.Clips[clip.Name] = clip;
            }
            return m;
        }
    }
}
