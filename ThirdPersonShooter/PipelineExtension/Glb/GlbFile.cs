// Dependency-light glTF 2.0 binary (.glb) reader. System.Text.Json + System.Numerics only, so it can be
// linked into both the content pipeline extension (net8.0) and Tools/GlbCheck (net10.0).
// Conventions: no axis, UV or winding changes. Matrices are System.Numerics row-vector (translation in M41..M43),
// which is the same memory layout as the glTF column-major 16 floats.
using System.Numerics;
using System.Text.Json;

namespace ThirdPersonShooter.Glb;

public sealed class GlbPrimitive
{
    public int Mode = 4;
    public int MaterialIndex = -1;
    public string? MaterialName;
    public Vector3[] Positions = Array.Empty<Vector3>();
    public Vector3[]? Normals;
    public Vector4[]? Tangents;
    public Vector2[]? Uv0;
    public Vector2[]? Uv1;
    public (byte, byte, byte, byte)[]? Joints;   // JOINTS_0 (u8 or u16 narrowed to byte; see JointsOverflow)
    public bool JointsOverflow;                  // true if any joint index > 255 was clamped
    public Vector4[]? Weights;
    public uint[] Indices = Array.Empty<uint>(); // triangle list; generated 0..n-1 when the primitive is not indexed
    public int TriangleCount => Indices.Length / 3;
}

public sealed class GlbMesh
{
    public string Name = "";
    public List<GlbPrimitive> Primitives = new();
}

public sealed class GlbNode
{
    public int Index;
    public string Name = "";
    public int Mesh = -1, Skin = -1, Light = -1;
    public Vector3 Translation;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;
    public Matrix4x4 Local = Matrix4x4.Identity;  // composed (S * R * T, row-vector) or the node's explicit matrix
    public Matrix4x4 World = Matrix4x4.Identity;  // composed through parents
    public int Parent = -1;
    public List<int> Children = new();
}

public sealed class GlbSkin
{
    public string Name = "";
    public int Skeleton = -1;
    public int[] Joints = Array.Empty<int>();
    public Matrix4x4[] InverseBind = Array.Empty<Matrix4x4>();
}

public sealed class GlbChannel
{
    public int Node;
    public string Path = "";           // translation | rotation | scale | weights
    public string Interpolation = "LINEAR";
    public float[] Times = Array.Empty<float>();
    public float[] Values = Array.Empty<float>(); // flattened (3 or 4 floats per key; CUBICSPLINE has 3x)
}

public sealed class GlbAnimation
{
    public string Name = "";
    public List<GlbChannel> Channels = new();
    public float Duration;
}

public sealed class GlbLight
{
    public string Name = "";
    public string Type = "point";
    public Vector3 Color = Vector3.One;
    public float Intensity = 1, Range;
    public float InnerCone, OuterCone;
}

public sealed class GlbFile
{
    public string Path = "";
    public string[] MaterialNames = Array.Empty<string>();
    public List<GlbMesh> Meshes = new();
    public List<GlbNode> Nodes = new();
    public List<GlbSkin> Skins = new();
    public List<GlbAnimation> Animations = new();
    public List<GlbLight> Lights = new();
    public List<int[]> Scenes = new();
    public int DefaultScene;

    public int TotalVertices => Meshes.Sum(m => m.Primitives.Sum(p => p.Positions.Length));
    public int TotalTriangles => Meshes.Sum(m => m.Primitives.Sum(p => p.TriangleCount));
    public int TotalPrimitives => Meshes.Sum(m => m.Primitives.Count);
    public int TotalChannels => Animations.Sum(a => a.Channels.Count);

    public static GlbFile Load(string path) => Parse(File.ReadAllBytes(path), path);

    public static GlbFile Parse(byte[] data, string path = "")
    {
        if (data.Length < 20 || BitConverter.ToUInt32(data, 0) != 0x46546C67) throw new InvalidDataException("Not a GLB (magic).");
        if (BitConverter.ToUInt32(data, 4) != 2) throw new InvalidDataException("Only glTF 2.0 GLB supported.");
        int off = 12; ReadOnlyMemory<byte> json = default; ReadOnlyMemory<byte> bin = default;
        while (off + 8 <= data.Length)
        {
            int len = (int)BitConverter.ToUInt32(data, off);
            uint type = BitConverter.ToUInt32(data, off + 4);
            var chunk = new ReadOnlyMemory<byte>(data, off + 8, len);
            if (type == 0x4E4F534A) json = chunk; else if (type == 0x004E4942 && bin.IsEmpty) bin = chunk;
            off += 8 + ((len + 3) & ~3);
        }
        if (json.IsEmpty) throw new InvalidDataException("No JSON chunk.");
        using var doc = JsonDocument.Parse(json);
        return new Builder(doc.RootElement, bin.Span.ToArray(), path).Build();
    }

    private sealed class Builder
    {
        readonly JsonElement r; readonly byte[] bin; readonly string path;
        public Builder(JsonElement root, byte[] bin, string path) { r = root; this.bin = bin; this.path = path; }

        static JsonElement? Get(JsonElement e, string n) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) ? v : null;
        static IEnumerable<JsonElement> Arr(JsonElement? e) => e is { ValueKind: JsonValueKind.Array } a ? a.EnumerateArray() : Enumerable.Empty<JsonElement>();
        static int I(JsonElement e, string n, int d = -1) => Get(e, n) is { } v ? v.GetInt32() : d;
        static string S(JsonElement e, string n) => Get(e, n) is { ValueKind: JsonValueKind.String } v ? v.GetString()! : "";
        static float F(JsonElement e, string n, float d = 0) => Get(e, n) is { } v ? v.GetSingle() : d;

        // ---- accessor reading -------------------------------------------------------------------------
        float[] ReadFloats(int acc, int expect, out int count)
        {
            var a = Get(r, "accessors")!.Value[acc];
            count = I(a, "count");
            int ct = I(a, "componentType");
            bool norm = Get(a, "normalized") is { ValueKind: JsonValueKind.True };
            int n = CompsOf(a);
            if (n != expect && expect > 0) throw new InvalidDataException($"Accessor {acc}: expected {expect} comps, got {n}");
            int csize = ct is 5120 or 5121 ? 1 : ct is 5122 or 5123 ? 2 : 4;
            var res = new float[count * n];
            if (Get(a, "bufferView") is not { } bvIdx) return res; // all zero (no sparse support)
            var bv = Get(r, "bufferViews")!.Value[bvIdx.GetInt32()];
            int start = I(bv, "byteOffset", 0) + I(a, "byteOffset", 0);
            int stride = I(bv, "byteStride", 0); if (stride == 0) stride = csize * n;
            for (int i = 0; i < count; i++)
                for (int c = 0; c < n; c++)
                {
                    int p = start + i * stride + c * csize;
                    float v = ct switch
                    {
                        5126 => BitConverter.ToSingle(bin, p),
                        5121 => norm ? bin[p] / 255f : bin[p],
                        5123 => norm ? BitConverter.ToUInt16(bin, p) / 65535f : BitConverter.ToUInt16(bin, p),
                        5120 => norm ? Math.Max((sbyte)bin[p] / 127f, -1f) : (sbyte)bin[p],
                        5122 => norm ? Math.Max(BitConverter.ToInt16(bin, p) / 32767f, -1f) : BitConverter.ToInt16(bin, p),
                        5125 => BitConverter.ToUInt32(bin, p),
                        _ => throw new InvalidDataException("componentType " + ct)
                    };
                    res[i * n + c] = v;
                }
            return res;
        }
        static int CompsOf(JsonElement a) => S(a, "type") switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, "MAT4" => 16, _ => 0 };

        uint[] ReadIndices(int acc)
        {
            var a = Get(r, "accessors")!.Value[acc];
            int count = I(a, "count"), ct = I(a, "componentType");
            var bv = Get(r, "bufferViews")!.Value[I(a, "bufferView")];
            int start = I(bv, "byteOffset", 0) + I(a, "byteOffset", 0);
            int csize = ct == 5121 ? 1 : ct == 5123 ? 2 : 4;
            int stride = I(bv, "byteStride", 0); if (stride == 0) stride = csize;
            var res = new uint[count];
            for (int i = 0; i < count; i++)
            {
                int p = start + i * stride;
                res[i] = ct == 5121 ? bin[p] : ct == 5123 ? BitConverter.ToUInt16(bin, p) : BitConverter.ToUInt32(bin, p);
            }
            return res;
        }

        static Vector3[] V3(float[] f) { var o = new Vector3[f.Length / 3]; for (int i = 0; i < o.Length; i++) o[i] = new(f[3 * i], f[3 * i + 1], f[3 * i + 2]); return o; }
        static Vector2[] V2(float[] f) { var o = new Vector2[f.Length / 2]; for (int i = 0; i < o.Length; i++) o[i] = new(f[2 * i], f[2 * i + 1]); return o; }
        static Vector4[] V4(float[] f) { var o = new Vector4[f.Length / 4]; for (int i = 0; i < o.Length; i++) o[i] = new(f[4 * i], f[4 * i + 1], f[4 * i + 2], f[4 * i + 3]); return o; }

        public GlbFile Build()
        {
            var g = new GlbFile { Path = path };
            var mats = Arr(Get(r, "materials")).Select(m => S(m, "name")).ToArray();
            g.MaterialNames = mats;

            foreach (var m in Arr(Get(r, "meshes")))
            {
                var mesh = new GlbMesh { Name = S(m, "name") };
                foreach (var p in Arr(Get(m, "primitives")))
                {
                    var at = Get(p, "attributes")!.Value;
                    var prim = new GlbPrimitive { Mode = I(p, "mode", 4), MaterialIndex = I(p, "material") };
                    if (prim.MaterialIndex >= 0 && prim.MaterialIndex < mats.Length) prim.MaterialName = mats[prim.MaterialIndex];
                    prim.Positions = V3(ReadFloats(I(at, "POSITION"), 3, out _));
                    if (Get(at, "NORMAL") is { } nrm) prim.Normals = V3(ReadFloats(nrm.GetInt32(), 3, out _));
                    if (Get(at, "TANGENT") is { } tan) prim.Tangents = V4(ReadFloats(tan.GetInt32(), 4, out _));
                    if (Get(at, "TEXCOORD_0") is { } t0) prim.Uv0 = V2(ReadFloats(t0.GetInt32(), 2, out _));
                    if (Get(at, "TEXCOORD_1") is { } t1) prim.Uv1 = V2(ReadFloats(t1.GetInt32(), 2, out _));
                    if (Get(at, "JOINTS_0") is { } jn)
                    {
                        var f = ReadFloats(jn.GetInt32(), 4, out int c);
                        prim.Joints = new (byte, byte, byte, byte)[c];
                        for (int i = 0; i < c; i++)
                        {
                            if (f[4 * i] > 255 || f[4 * i + 1] > 255 || f[4 * i + 2] > 255 || f[4 * i + 3] > 255) prim.JointsOverflow = true;
                            prim.Joints[i] = ((byte)f[4 * i], (byte)f[4 * i + 1], (byte)f[4 * i + 2], (byte)f[4 * i + 3]);
                        }
                    }
                    if (Get(at, "WEIGHTS_0") is { } wt) prim.Weights = V4(ReadFloats(wt.GetInt32(), 4, out _));
                    if (Get(p, "indices") is { } ix) prim.Indices = ReadIndices(ix.GetInt32());
                    else { prim.Indices = new uint[prim.Positions.Length]; for (int i = 0; i < prim.Indices.Length; i++) prim.Indices[i] = (uint)i; }
                    prim.Indices = ToTriangleList(prim.Indices, prim.Mode);
                    mesh.Primitives.Add(prim);
                }
                g.Meshes.Add(mesh);
            }

            // KHR_lights_punctual definitions
            var lightDefs = Get(r, "extensions") is { } ext && Get(ext, "KHR_lights_punctual") is { } kl ? Arr(Get(kl, "lights")).ToList() : new();
            foreach (var l in lightDefs)
            {
                var gl = new GlbLight { Name = S(l, "name"), Type = S(l, "type"), Intensity = F(l, "intensity", 1), Range = F(l, "range") };
                if (Get(l, "color") is { } c) { var a = c.EnumerateArray().Select(x => x.GetSingle()).ToArray(); gl.Color = new(a[0], a[1], a[2]); }
                if (Get(l, "spot") is { } sp) { gl.InnerCone = F(sp, "innerConeAngle"); gl.OuterCone = F(sp, "outerConeAngle", MathF.PI / 4); }
                g.Lights.Add(gl);
            }

            int ni = 0;
            foreach (var n in Arr(Get(r, "nodes")))
            {
                var gn = new GlbNode { Index = ni++, Name = S(n, "name"), Mesh = I(n, "mesh"), Skin = I(n, "skin") };
                if (Get(n, "extensions") is { } ne && Get(ne, "KHR_lights_punctual") is { } nl) gn.Light = I(nl, "light");
                if (Get(n, "matrix") is { } mx)
                {
                    var a = mx.EnumerateArray().Select(x => x.GetSingle()).ToArray();
                    gn.Local = new Matrix4x4(a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7], a[8], a[9], a[10], a[11], a[12], a[13], a[14], a[15]);
                    Matrix4x4.Decompose(gn.Local, out gn.Scale, out gn.Rotation, out gn.Translation);
                }
                else
                {
                    if (Get(n, "translation") is { } t) { var a = t.EnumerateArray().Select(x => x.GetSingle()).ToArray(); gn.Translation = new(a[0], a[1], a[2]); }
                    if (Get(n, "rotation") is { } q) { var a = q.EnumerateArray().Select(x => x.GetSingle()).ToArray(); gn.Rotation = Quaternion.Normalize(new(a[0], a[1], a[2], a[3])); }
                    if (Get(n, "scale") is { } s) { var a = s.EnumerateArray().Select(x => x.GetSingle()).ToArray(); gn.Scale = new(a[0], a[1], a[2]); }
                    gn.Local = Matrix4x4.CreateScale(gn.Scale) * Matrix4x4.CreateFromQuaternion(gn.Rotation) * Matrix4x4.CreateTranslation(gn.Translation);
                }
                foreach (var c in Arr(Get(n, "children"))) gn.Children.Add(c.GetInt32());
                g.Nodes.Add(gn);
            }
            foreach (var n in g.Nodes) foreach (var c in n.Children) g.Nodes[c].Parent = n.Index;

            foreach (var s in Arr(Get(r, "scenes"))) g.Scenes.Add(Arr(Get(s, "nodes")).Select(x => x.GetInt32()).ToArray());
            g.DefaultScene = Math.Max(0, I(r, "scene", 0));
            if (g.Scenes.Count == 0) g.Scenes.Add(g.Nodes.Where(n => n.Parent < 0).Select(n => n.Index).ToArray());
            foreach (var root in g.Scenes[Math.Min(g.DefaultScene, g.Scenes.Count - 1)]) ComposeWorld(g, root, Matrix4x4.Identity);

            foreach (var s in Arr(Get(r, "skins")))
            {
                var sk = new GlbSkin { Name = S(s, "name"), Skeleton = I(s, "skeleton"), Joints = Arr(Get(s, "joints")).Select(x => x.GetInt32()).ToArray() };
                if (Get(s, "inverseBindMatrices") is { } ib)
                {
                    var f = ReadFloats(ib.GetInt32(), 16, out int c);
                    sk.InverseBind = new Matrix4x4[c];
                    for (int i = 0; i < c; i++)
                    {
                        int o = i * 16;
                        sk.InverseBind[i] = new Matrix4x4(f[o], f[o + 1], f[o + 2], f[o + 3], f[o + 4], f[o + 5], f[o + 6], f[o + 7], f[o + 8], f[o + 9], f[o + 10], f[o + 11], f[o + 12], f[o + 13], f[o + 14], f[o + 15]);
                    }
                }
                else sk.InverseBind = Enumerable.Repeat(Matrix4x4.Identity, sk.Joints.Length).ToArray();
                g.Skins.Add(sk);
            }

            foreach (var a in Arr(Get(r, "animations")))
            {
                var ga = new GlbAnimation { Name = S(a, "name") };
                var samplers = Arr(Get(a, "samplers")).ToList();
                foreach (var ch in Arr(Get(a, "channels")))
                {
                    var tgt = Get(ch, "target")!.Value;
                    if (Get(tgt, "node") is null) continue;
                    var smp = samplers[I(ch, "sampler")];
                    var gc = new GlbChannel { Node = I(tgt, "node"), Path = S(tgt, "path"), Interpolation = Get(smp, "interpolation") is { } ip ? ip.GetString()! : "LINEAR" };
                    gc.Times = ReadFloats(I(smp, "input"), 1, out _);
                    gc.Values = ReadFloats(I(smp, "output"), 0, out _);
                    if (gc.Times.Length > 0) ga.Duration = Math.Max(ga.Duration, gc.Times[^1]);
                    ga.Channels.Add(gc);
                }
                g.Animations.Add(ga);
            }
            return g;
        }

        static void ComposeWorld(GlbFile g, int idx, Matrix4x4 parent)
        {
            var n = g.Nodes[idx];
            n.World = n.Local * parent;
            foreach (var c in n.Children) ComposeWorld(g, c, n.World);
        }

        static uint[] ToTriangleList(uint[] idx, int mode)
        {
            if (mode == 4) return idx;
            var o = new List<uint>();
            if (mode == 5) for (int i = 0; i + 2 < idx.Length; i++) { if (i % 2 == 0) { o.Add(idx[i]); o.Add(idx[i + 1]); o.Add(idx[i + 2]); } else { o.Add(idx[i + 1]); o.Add(idx[i]); o.Add(idx[i + 2]); } }
            else if (mode == 6) for (int i = 1; i + 1 < idx.Length; i++) { o.Add(idx[0]); o.Add(idx[i]); o.Add(idx[i + 1]); }
            else return Array.Empty<uint>(); // points/lines: not triangles
            return o.ToArray();
        }
    }
}
