using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace ThirdPersonShooter.PipelineExtension;

/// <summary>
/// Skinned model, our own compact format (no ModelContent): pruned skeleton, mesh parts split by bone palette,
/// resampled clips. Binary layout is shared with Core/Animation/SkinnedModelReader.cs (keep FormatVersion in sync).
/// Quaternions are x,y,z,w; matrices are row-vector (XNA) 16 floats; no axis, UV or winding changes.
/// </summary>
public sealed class SkinnedModelContent
{
    public const int FormatVersion = 1;
    public string Name = "";
    public List<BoneData> Bones = new();
    public List<PartData> Parts = new();
    public List<ClipData> Clips = new();

    public sealed class BoneData
    {
        public string Name = ""; public int Parent = -1;
        public float[] T = new float[3], R = { 0, 0, 0, 1 }, S = { 1, 1, 1 };
        public float[] InverseBind = new float[16]; // identity for bones that are not in any palette
    }

    public sealed class PartData
    {
        public string Material = "";
        public int VertexCount;
        // 68 bytes per vertex: Position(3f) Normal(3f) Tangent(4f) UV(2f) Joints(4 bytes, palette slots) Weights(4f)
        public byte[] Vertices = Array.Empty<byte>();
        public int[] Indices = Array.Empty<int>();
        public int[] Palette = Array.Empty<int>(); // skeleton bone index per palette slot
    }

    public sealed class TrackData
    {
        public float[]? T, R, S; // null = bind pose; 1 key = constant; frameCount keys = animated (3/4/3 floats per key)
    }

    public sealed class ClipData
    {
        public string Name = ""; public bool Loop; public float Duration; public int FrameCount; public float Fps;
        public TrackData[] Tracks = Array.Empty<TrackData>(); // one per skeleton bone
    }
}

[ContentTypeWriter]
public sealed class SkinnedModelWriter : ContentTypeWriter<SkinnedModelContent>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform) => "ThirdPersonShooter.SkinnedModelReader, Core";
    public override string GetRuntimeType(TargetPlatform targetPlatform) => "ThirdPersonShooter.SkinnedModel, Core";

    protected override void Write(ContentWriter o, SkinnedModelContent m)
    {
        o.Write(SkinnedModelContent.FormatVersion);
        o.Write(m.Name);
        o.Write(m.Bones.Count);
        foreach (var b in m.Bones)
        {
            o.Write(b.Name); o.Write(b.Parent);
            foreach (var f in b.T) o.Write(f); foreach (var f in b.R) o.Write(f); foreach (var f in b.S) o.Write(f);
            foreach (var f in b.InverseBind) o.Write(f);
        }
        o.Write(m.Parts.Count);
        foreach (var p in m.Parts)
        {
            o.Write(p.Material); o.Write(p.VertexCount); o.Write(p.Vertices.Length); o.Write(p.Vertices);
            o.Write(p.Indices.Length); foreach (var i in p.Indices) o.Write(i);
            o.Write(p.Palette.Length); foreach (var i in p.Palette) o.Write(i);
        }
        o.Write(m.Clips.Count);
        foreach (var c in m.Clips)
        {
            o.Write(c.Name); o.Write(c.Loop); o.Write(c.Duration); o.Write(c.FrameCount); o.Write(c.Fps);
            foreach (var t in c.Tracks)
            {
                o.Write((byte)((t.T != null ? 1 : 0) | (t.R != null ? 2 : 0) | (t.S != null ? 4 : 0)));
                foreach (var arr in new[] { t.T, t.R, t.S })
                    if (arr != null) { o.Write(arr.Length); foreach (var f in arr) o.Write(f); }
            }
        }
    }
}
