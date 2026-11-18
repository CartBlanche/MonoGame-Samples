using System.ComponentModel;
using System.Numerics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using ThirdPersonShooter.Glb;

namespace ThirdPersonShooter.PipelineExtension;

/// <summary>
/// glb (player/red_robot) -> SkinnedModelContent. Input is the GlbImporter NodeContent; only its source filename is used,
/// the glb is re-read with GlbFile (skins and animations are not part of the NodeContent).
/// Steps: weighted-joint pruning (+ forced bones + ancestors); mesh parts split so each palette has at most MaxPaletteBones;
/// Byte4 joints (palette slots) + Vector4 weights; tangents generated; clips on the whitelist (godot "-cycle" suffix stripped
/// and flagged as loop, "-noexp" dropped) resampled at ClipFps, constant tracks collapsed to one key.
/// </summary>
[ContentProcessor(DisplayName = "Godot Skinned Model - ThirdPersonShooter")]
public class GodotSkinnedModelProcessor : ContentProcessor<NodeContent, SkinnedModelContent>
{
    [DefaultValue("Idle,Idlecombat,Idlecombatrest,running_nogun,running_gun,strafe_left,strafe_right,strafe_front,strafe_back,jump_2_upwards,jump_4_falling,jump_5_hardlanding,AIM-Up,AIM-Down,AIM-Center,eyes")]
    public string ClipWhitelist { get; set; } = "Idle,Idlecombat,Idlecombatrest,running_nogun,running_gun,strafe_left,strafe_right,strafe_front,strafe_back,jump_2_upwards,jump_4_falling,jump_5_hardlanding,AIM-Up,AIM-Down,AIM-Center,eyes";
    [DefaultValue("root,hand.R,neck.001,upper_arm.R,eyes,eyelid.u.L,eyelid.u.R,eyelid.d.L,eyelid.d.R")]
    public string ForceKeepBones { get; set; } = "root,hand.R,neck.001,upper_arm.R,eyes,eyelid.u.L,eyelid.u.R,eyelid.d.L,eyelid.d.R";
    [DefaultValue(72)] public int MaxPaletteBones { get; set; } = 72;
    [DefaultValue(30)] public int ClipFps { get; set; } = 30;

    public override SkinnedModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        string path = input.Identity!.SourceFilename;
        var glb = GlbFile.Load(path);
        if (glb.Skins.Count == 0) throw new InvalidContentException("glb has no skin: " + path);
        var skin = glb.Skins[0];
        var res = new SkinnedModelContent { Name = Path.GetFileNameWithoutExtension(path) };

        // ---- which joints carry weight -----------------------------------------------------------------
        var skinned = glb.Nodes.Where(n => n.Skin == 0 && n.Mesh >= 0).ToList();
        var weighted = new HashSet<int>(); // skin joint indices
        foreach (var p in skinned.SelectMany(n => glb.Meshes[n.Mesh].Primitives))
        {
            if (p.Joints == null || p.Weights == null) throw new InvalidContentException("skinned primitive without JOINTS_0/WEIGHTS_0");
            for (int i = 0; i < p.Joints.Length; i++)
            {
                var j = p.Joints[i]; var w = p.Weights[i];
                if (w.X > 0) weighted.Add(j.Item1); if (w.Y > 0) weighted.Add(j.Item2); if (w.Z > 0) weighted.Add(j.Item3); if (w.W > 0) weighted.Add(j.Item4);
            }
        }
        var keepNodes = new HashSet<int>(weighted.Select(j => skin.Joints[j]));
        var forced = new HashSet<string>(ForceKeepBones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        foreach (var n in glb.Nodes) if (forced.Contains(n.Name) && skin.Joints.Contains(n.Index)) keepNodes.Add(n.Index);
        foreach (var k in keepNodes.ToList()) for (int a = glb.Nodes[k].Parent; a >= 0; a = glb.Nodes[a].Parent) keepNodes.Add(a);

        // ---- skeleton in parent-first (DFS) order --------------------------------------------------------
        var boneOfNode = new Dictionary<int, int>();
        void Visit(int ni)
        {
            if (!keepNodes.Contains(ni)) return;
            var n = glb.Nodes[ni];
            int jointIdx = Array.IndexOf(skin.Joints, ni);
            var b = new SkinnedModelContent.BoneData
            {
                Name = n.Name, Parent = n.Parent >= 0 && boneOfNode.TryGetValue(n.Parent, out var pb) ? pb : -1,
                T = new[] { n.Translation.X, n.Translation.Y, n.Translation.Z },
                R = new[] { n.Rotation.X, n.Rotation.Y, n.Rotation.Z, n.Rotation.W },
                S = new[] { n.Scale.X, n.Scale.Y, n.Scale.Z },
                InverseBind = Flat(jointIdx >= 0 ? skin.InverseBind[jointIdx] : Matrix4x4.Identity)
            };
            boneOfNode[ni] = res.Bones.Count; res.Bones.Add(b);
            foreach (var c in n.Children) Visit(c);
        }
        foreach (var r in glb.Scenes[glb.DefaultScene]) Visit(r);
        context.Logger.Log(string.Format("GodotSkinnedModel {0}: joints={1} weighted={2} kept skeleton bones={3}", res.Name, skin.Joints.Length, weighted.Count, res.Bones.Count));

        // ---- parts, split by palette -----------------------------------------------------------------
        foreach (var node in skinned)
            foreach (var prim in glb.Meshes[node.Mesh].Primitives.Where(p => p.Indices.Length > 0))
                SplitPrimitive(prim, skin, boneOfNode, res, context);
        int maxPal = res.Parts.Max(p => p.Palette.Length);
        context.Logger.Log(string.Format("GodotSkinnedModel {0}: parts={1} max palette={2} (limit {3}) verts={4} tris={5}", res.Name, res.Parts.Count, maxPal, MaxPaletteBones,
            res.Parts.Sum(p => p.VertexCount), res.Parts.Sum(p => p.Indices.Length / 3)));

        // ---- clips ----------------------------------------------------------------------------------
        var wl = new HashSet<string>(ClipWhitelist.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var nodeOfBone = boneOfNode.ToDictionary(kv => kv.Value, kv => kv.Key);
        foreach (var anim in glb.Animations)
        {
            if (anim.Name.EndsWith("-noexp")) continue;
            bool loop = anim.Name.EndsWith("-cycle");
            string name = loop ? anim.Name[..^6] : anim.Name;
            if (!wl.Contains(name)) continue;
            res.Clips.Add(ResampleClip(glb, anim, name, loop, res, nodeOfBone, ClipFps, context));
        }
        foreach (var w in wl) if (!res.Clips.Any(c => c.Name == w)) context.Logger.Log(string.Format("warning: whitelisted clip '{0}' not found in {1}", w, res.Name));
        context.Logger.Log(string.Format("GodotSkinnedModel {0}: clips={1} [{2}]", res.Name, res.Clips.Count, string.Join(", ", res.Clips.Select(c => $"{c.Name}:{c.FrameCount}f"))));
        return res;
    }

    static float[] Flat(Matrix4x4 m) => new[] { m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44 };

    void SplitPrimitive(GlbPrimitive p, GlbSkin skin, Dictionary<int, int> boneOfNode, SkinnedModelContent res, ContentProcessorContext ctx)
    {
        var tangents = p.Tangents ?? ComputeTangents(p);
        int V = p.Positions.Length;
        // bone indices (skeleton space) per vertex, only for non-zero weights
        int[][] vb = new int[V][];
        var jw = new float[V][];
        for (int i = 0; i < V; i++)
        {
            var j = p.Joints![i]; var w = p.Weights![i];
            var js = new[] { j.Item1, j.Item2, j.Item3, j.Item4 }; var ws = new[] { w.X, w.Y, w.Z, w.W };
            float sum = ws.Sum(); if (sum <= 0) { ws[0] = 1; sum = 1; }
            for (int k = 0; k < 4; k++) ws[k] /= sum;
            vb[i] = js.Select((jj, k) => ws[k] > 0 ? boneOfNode[skin.Joints[jj]] : -1).ToArray();
            jw[i] = ws;
        }
        var groups = new List<(HashSet<int> bones, List<int> tris)> { (new HashSet<int>(), new List<int>()) };
        for (int t = 0; t + 2 < p.Indices.Length; t += 3)
        {
            var tb = new HashSet<int>();
            for (int k = 0; k < 3; k++) foreach (var b in vb[p.Indices[t + k]]) if (b >= 0) tb.Add(b);
            var g = groups[^1];
            if (g.bones.Count + tb.Count(b => !g.bones.Contains(b)) > MaxPaletteBones) { g = (new HashSet<int>(), new List<int>()); groups.Add(g); }
            g.bones.UnionWith(tb); g.tris.Add(t);
        }
        foreach (var (bones, tris) in groups)
        {
            var palette = bones.OrderBy(b => b).ToArray();
            var slot = new Dictionary<int, int>(); for (int i = 0; i < palette.Length; i++) slot[palette[i]] = i;
            var remap = new Dictionary<uint, int>(); var order = new List<uint>(); var idx = new List<int>();
            foreach (int t in tris)
                for (int k = 0; k < 3; k++)
                {
                    uint v = p.Indices[t + k];
                    if (!remap.TryGetValue(v, out int nv)) { nv = order.Count; remap[v] = nv; order.Add(v); }
                    idx.Add(nv);
                }
            var ms = new MemoryStream(); var bw = new BinaryWriter(ms);
            foreach (uint v in order)
            {
                var pos = p.Positions[v]; var nrm = p.Normals != null ? p.Normals[v] : Vector3.UnitY; var tan = tangents[v]; var uv = p.Uv0 != null ? p.Uv0[v] : Vector2.Zero;
                foreach (var f in new[] { pos.X, pos.Y, pos.Z, nrm.X, nrm.Y, nrm.Z, tan.X, tan.Y, tan.Z, tan.W, uv.X, uv.Y }) bw.Write(f);
                for (int k = 0; k < 4; k++) bw.Write((byte)(vb[v][k] >= 0 ? slot[vb[v][k]] : 0));
                for (int k = 0; k < 4; k++) bw.Write(jw[v][k]);
            }
            res.Parts.Add(new SkinnedModelContent.PartData { Material = p.MaterialName ?? "", VertexCount = order.Count, Vertices = ms.ToArray(), Indices = idx.ToArray(), Palette = palette });
        }
        ctx.Logger.Log(string.Format("  primitive '{0}': verts={1} -> {2} part(s) with palettes [{3}]", p.MaterialName, V, groups.Count, string.Join(",", groups.Select(g => g.bones.Count))));
    }

    // ---- clips -----------------------------------------------------------------------------------------
    static SkinnedModelContent.ClipData ResampleClip(GlbFile glb, GlbAnimation anim, string name, bool loop, SkinnedModelContent res, Dictionary<int, int> nodeOfBone, int fps, ContentProcessorContext ctx)
    {
        float dur = anim.Duration;
        int frames = Math.Max(1, (int)MathF.Ceiling(dur * fps - 1e-3f) + 1);
        var clip = new SkinnedModelContent.ClipData { Name = name, Loop = loop, Duration = dur, FrameCount = frames, Fps = fps, Tracks = new SkinnedModelContent.TrackData[res.Bones.Count] };
        var chans = anim.Channels.GroupBy(c => c.Node).ToDictionary(g => g.Key, g => g.ToList());
        for (int b = 0; b < res.Bones.Count; b++)
        {
            var tr = new SkinnedModelContent.TrackData(); clip.Tracks[b] = tr;
            if (!chans.TryGetValue(nodeOfBone[b], out var list)) continue;
            foreach (var ch in list)
            {
                if (ch.Interpolation != "LINEAR" && ch.Interpolation != "STEP") ctx.Logger.Log(string.Format("warning: clip {0}: interpolation {1} treated as linear", name, ch.Interpolation));
                int comp = ch.Path == "rotation" ? 4 : 3;
                var vals = new float[frames * comp];
                for (int f = 0; f < frames; f++)
                {
                    float t = MathF.Min(f / (float)fps, dur);
                    Sample(ch, comp, t, vals.AsSpan(f * comp, comp));
                }
                var arr = Collapse(vals, comp, frames);
                if (ch.Path == "translation") tr.T = arr; else if (ch.Path == "rotation") tr.R = arr; else if (ch.Path == "scale") tr.S = arr;
            }
        }
        return clip;
    }

    static void Sample(GlbChannel ch, int comp, float t, Span<float> o)
    {
        var times = ch.Times; int n = times.Length;
        if (n == 1 || t <= times[0]) { Read(ch, comp, 0, o); return; }
        if (t >= times[n - 1]) { Read(ch, comp, n - 1, o); return; }
        int hi = Array.BinarySearch(times, t); if (hi < 0) hi = ~hi; int lo = hi - 1;
        if (hi == 0) { Read(ch, comp, 0, o); return; }
        float a = (t - times[lo]) / (times[hi] - times[lo]);
        Span<float> v0 = stackalloc float[4], v1 = stackalloc float[4];
        Read(ch, comp, lo, v0); Read(ch, comp, hi, v1);
        if (ch.Interpolation == "STEP") { v0[..comp].CopyTo(o); return; }
        if (comp == 4)
        {
            var q = Quaternion.Normalize(Quaternion.Slerp(new Quaternion(v0[0], v0[1], v0[2], v0[3]), new Quaternion(v1[0], v1[1], v1[2], v1[3]), a));
            o[0] = q.X; o[1] = q.Y; o[2] = q.Z; o[3] = q.W;
        }
        else for (int i = 0; i < 3; i++) o[i] = v0[i] + (v1[i] - v0[i]) * a;
    }

    static void Read(GlbChannel ch, int comp, int key, Span<float> o)
    {
        int stride = ch.Interpolation == "CUBICSPLINE" ? 3 : 1, off = (key * stride + (stride == 3 ? 1 : 0)) * comp;
        for (int i = 0; i < comp; i++) o[i] = ch.Values[off + i];
    }

    static float[] Collapse(float[] v, int comp, int frames)
    {
        bool constant = true;
        for (int f = 1; f < frames && constant; f++)
        {
            if (comp == 4)
            {
                float d = v[0] * v[f * 4] + v[1] * v[f * 4 + 1] + v[2] * v[f * 4 + 2] + v[3] * v[f * 4 + 3];
                if (1 - MathF.Abs(d) > 1e-7f) constant = false;
            }
            else for (int i = 0; i < 3; i++) if (MathF.Abs(v[f * 3 + i] - v[i]) > 1e-5f) constant = false;
        }
        return constant ? v[..comp] : v;
    }

    // ---- tangents (per vertex, area weighted, Gram-Schmidt; same algorithm as GodotStaticModelProcessor) ----
    static Vector4[] ComputeTangents(GlbPrimitive p)
    {
        int n = p.Positions.Length;
        var tan = new Vector3[n]; var bit = new Vector3[n];
        if (p.Uv0 != null && p.Normals != null)
            for (int t = 0; t + 2 < p.Indices.Length; t += 3)
            {
                uint i0 = p.Indices[t], i1 = p.Indices[t + 1], i2 = p.Indices[t + 2];
                Vector3 e1 = p.Positions[i1] - p.Positions[i0], e2 = p.Positions[i2] - p.Positions[i0];
                Vector2 d1 = p.Uv0[i1] - p.Uv0[i0], d2 = p.Uv0[i2] - p.Uv0[i0];
                float det = d1.X * d2.Y - d2.X * d1.Y;
                if (MathF.Abs(det) < 1e-12f) continue;
                float r = 1f / det, w = Vector3.Cross(e1, e2).Length();
                Vector3 s = (e1 * d2.Y - e2 * d1.Y) * r, tt = (e2 * d1.X - e1 * d2.X) * r;
                if (w > 0 && s.LengthSquared() > 0 && tt.LengthSquared() > 0) { s = Vector3.Normalize(s) * w; tt = Vector3.Normalize(tt) * w; }
                foreach (var i in new[] { i0, i1, i2 }) { tan[i] += s; bit[i] += tt; }
            }
        var res = new Vector4[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 nn = p.Normals != null ? p.Normals[i] : Vector3.UnitY;
            Vector3 t = tan[i] - nn * Vector3.Dot(nn, tan[i]);
            if (t.LengthSquared() < 1e-12f) t = MathF.Abs(nn.X) < 0.9f ? Vector3.Cross(nn, Vector3.UnitX) : Vector3.Cross(nn, Vector3.UnitY);
            t = Vector3.Normalize(t);
            res[i] = new Vector4(t, Vector3.Dot(Vector3.Cross(nn, t), bit[i]) < 0 ? -1f : 1f);
        }
        return res;
    }
}
