using System.Text.Json.Nodes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Shooter.Pipeline;

namespace Shooter.CharacterExport;

/// <summary>Writes the real bind-pose transforms of every node of a rig FBX, in the pipeline's Unity space.</summary>
public static class SkeletonExporter
{
    public static JsonObject Export(string name, NodeContent scene, SpaceConversion space, string spaceDescription)
    {
        NodeContent skeletonRoot = SkeletonExtractor.FindSkeletonRoot(scene);
        List<SkeletonBoneContent> bones = SkeletonExtractor.BuildBones(skeletonRoot, space);

        JsonObject o = new JsonObject();
        o["name"] = name;
        o["format"] = "shooter.skeleton/1";
        o["space"] = spaceDescription;
        o["scale"] = space.Scale;
        o["mirrorX"] = space.MirrorX;
        o["unityFromThis"] = "F = diag(1,1,-1): position (x,y,-z), quaternion (-x,-y,z,w), scale unchanged";
        o["matrixConvention"] = "16 floats, row-major, XNA row-vector convention (translation in elements 12..14)";
        o["skeletonRoot"] = skeletonRoot.Name;

        JsonArray arr = new JsonArray();
        string[] paths = new string[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            SkeletonBoneContent b = bones[i];
            paths[i] = b.ParentIndex < 0 ? b.Name : paths[b.ParentIndex] + "/" + b.Name;
            JsonObject jb = new JsonObject();
            jb["index"] = i;
            jb["name"] = b.Name;
            jb["parent"] = b.ParentIndex;
            jb["path"] = paths[i];
            jb["local"] = Trs(b.LocalBindPose);
            jb["global"] = Trs(b.GlobalBindPose);
            Vector3 gp = b.GlobalBindPose.Translation;
            jb["unityGlobalPosition"] = Vec(gp.X, gp.Y, -gp.Z);
            arr.Add(jb);
        }

        o["bones"] = arr;

        // Non-skeleton nodes (mesh holders such as "Top", "EyeBall") for completeness.
        JsonArray others = new JsonArray();
        foreach (NodeContent n in scene.Children)
        {
            if (ReferenceEquals(n, skeletonRoot))
            {
                continue;
            }

            JsonObject jn = new JsonObject();
            jn["name"] = n.Name;
            jn["isMesh"] = n is MeshContent;
            jn["local"] = Trs(space.Local(n.Transform));
            others.Add(jn);
        }

        o["otherNodes"] = others;
        return o;
    }

    private static JsonObject Trs(Matrix m)
    {
        Vector3 s, t;
        Quaternion r;
        m.Decompose(out s, out r, out t);
        r = Quaternion.Normalize(r);
        JsonObject o = new JsonObject();
        o["position"] = Vec(t.X, t.Y, t.Z);
        o["rotation"] = new JsonArray(R(r.X), R(r.Y), R(r.Z), R(r.W));
        o["scale"] = Vec(s.X, s.Y, s.Z);
        o["matrix"] = new JsonArray(
            R(m.M11), R(m.M12), R(m.M13), R(m.M14),
            R(m.M21), R(m.M22), R(m.M23), R(m.M24),
            R(m.M31), R(m.M32), R(m.M33), R(m.M34),
            R(m.M41), R(m.M42), R(m.M43), R(m.M44));
        return o;
    }

    private static JsonArray Vec(float x, float y, float z)
    {
        return new JsonArray(R(x), R(y), R(z));
    }

    private static double R(float f)
    {
        return Math.Round(f, 6);
    }
}
