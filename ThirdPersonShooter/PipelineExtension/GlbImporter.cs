using System.Numerics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using ThirdPersonShooter.Glb;
using XVector2 = Microsoft.Xna.Framework.Vector2;
using XVector3 = Microsoft.Xna.Framework.Vector3;
using XVector4 = Microsoft.Xna.Framework.Vector4;
using XMatrix = Microsoft.Xna.Framework.Matrix;

namespace ThirdPersonShooter.PipelineExtension;

/// <summary>
/// glTF 2.0 binary importer. Produces a NodeContent tree that mirrors the glTF scene (node names and local
/// transforms kept; a mesh node becomes a MeshContent with one GeometryContent per primitive). No axis, UV or
/// winding changes. Per-primitive material = BasicMaterialContent whose Name is the glTF material name (the Godot
/// material id; textures are NOT in the glb, see Tools/GodotExtract Materials.json glbMaterialMap).
/// Skins, animations and KHR lights are exposed by <see cref="GlbFile"/> but not emitted here (next milestone).
/// </summary>
[ContentImporter(new[] { ".glb" }, DisplayName = "GLB Importer - ThirdPersonShooter", DefaultProcessor = "GodotStaticModelProcessor")]
public class GlbImporter : ContentImporter<NodeContent>
{
    public static XMatrix ToXna(Matrix4x4 m) => new(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);

    public override NodeContent Import(string filename, ContentImporterContext context)
    {
        var glb = GlbFile.Load(filename);
        var root = new NodeContent { Name = Path.GetFileNameWithoutExtension(filename), Identity = new ContentIdentity(filename) };
        var scene = glb.Scenes[Math.Min(glb.DefaultScene, glb.Scenes.Count - 1)];
        foreach (var n in scene) root.Children.Add(BuildNode(glb, glb.Nodes[n], filename));
        // Geometry is emitted ONCE per glTF mesh (props.glb/structure.glb instance meshes up to 100s of times), as a
        // mesh node "mesh/<name>" directly under the root with an identity transform. Instance nodes keep their name and
        // local transform and carry a child marker node "@<meshName>" naming the mesh they instance; the scene baker/runtime
        // composes world = node chain, then looks the mesh up by name.
        var materials = new Dictionary<string, BasicMaterialContent>();
        for (int i = 0; i < glb.Meshes.Count; i++)
        {
            if (!glb.Nodes.Any(n => n.Mesh == i) || !glb.Meshes[i].Primitives.Any(p => p.Mode is 4 or 5 or 6)) continue;
            var m = BuildMesh(glb.Meshes[i], MeshName(glb, i), filename, materials);
            m.Name = "mesh/" + MeshName(glb, i);
            root.Children.Add(m);
        }
        context.Logger.Log(string.Format("GlbImporter: {0} nodes, {1} meshes, {2} prims, {3} verts, {4} tris", glb.Nodes.Count, glb.Meshes.Count, glb.TotalPrimitives, glb.TotalVertices, glb.TotalTriangles));
        return root;
    }

    static string MeshName(GlbFile glb, int i) => glb.Meshes[i].Name.Length > 0 ? glb.Meshes[i].Name : "mesh" + i;

    static NodeContent BuildNode(GlbFile glb, GlbNode n, string file)
    {
        var node = new NodeContent
        {
            Name = n.Name.Length > 0 ? n.Name : "node" + n.Index,
            Identity = new ContentIdentity(file),
            Transform = ToXna(n.Local)
        };
        if (n.Mesh >= 0) node.Children.Add(new NodeContent { Name = "@" + MeshName(glb, n.Mesh), Identity = node.Identity });
        foreach (var c in n.Children) node.Children.Add(BuildNode(glb, glb.Nodes[c], file));
        return node;
    }

    static MeshContent BuildMesh(GlbMesh gm, string name, string file, Dictionary<string, BasicMaterialContent> mats)
    {
        var mesh = new MeshContent { Name = name, Identity = new ContentIdentity(file) };
        foreach (var p in gm.Primitives)
        {
            if (p.Indices.Length == 0) continue;
            int baseIdx = mesh.Positions.Count;
            foreach (var v in p.Positions) mesh.Positions.Add(new XVector3(v.X, v.Y, v.Z));
            var geom = new GeometryContent { Identity = mesh.Identity };
            for (int i = 0; i < p.Positions.Length; i++) geom.Vertices.Add(baseIdx + i);
            foreach (var idx in p.Indices) geom.Indices.Add((int)idx);

            if (p.Normals != null) geom.Vertices.Channels.Add(VertexChannelNames.Normal(), p.Normals.Select(v => new XVector3(v.X, v.Y, v.Z)).ToArray());
            if (p.Uv0 != null) geom.Vertices.Channels.Add(VertexChannelNames.TextureCoordinate(0), p.Uv0.Select(v => new XVector2(v.X, v.Y)).ToArray());
            if (p.Tangents != null) geom.Vertices.Channels.Add(VertexChannelNames.Tangent(0), p.Tangents.Select(v => new XVector4(v.X, v.Y, v.Z, v.W)).ToArray());

            string matName = p.MaterialName ?? "";
            if (!mats.TryGetValue(matName, out var mat)) mats[matName] = mat = new BasicMaterialContent { Name = matName };
            geom.Material = mat;
            mesh.Geometry.Add(geom);
        }
        return mesh;
    }
}
