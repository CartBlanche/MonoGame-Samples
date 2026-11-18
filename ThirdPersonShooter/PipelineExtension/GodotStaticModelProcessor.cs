using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace ThirdPersonShooter.PipelineExtension;

/// <summary>
/// Static model processor for glb level/prop meshes. Output vertex layout per part: Position, Normal, Tangent (Vector4,
/// w = handedness), TextureCoordinate0 (UV1 is dropped). Tangents are generated per triangle (accumulated, Gram-Schmidt
/// orthogonalised) when the glb has none. Node hierarchy and names are kept; ModelMeshPart.Tag = the glTF/Godot material
/// name (string) used to look up Materials.json glbMaterialMap. No UV or winding flips. Textures are never built here.
/// </summary>
[ContentProcessor(DisplayName = "Godot Static Model - ThirdPersonShooter")]
public class GodotStaticModelProcessor : ModelProcessor
{
    [DefaultValue(true)] public bool GenerateTangents { get; set; } = true;

    public GodotStaticModelProcessor() { GenerateTangentFrames = false; PremultiplyVertexColors = false; }

    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        if (GenerateTangents) AddTangents(input, context);
        var model = base.Process(input, context);
        foreach (var mesh in model.Meshes)
            foreach (var part in mesh.MeshParts)
                part.Tag = part.Material?.Name ?? "";
        long verts = model.Meshes.Sum(m => m.MeshParts.Sum(p => (long)p.NumVertices)), tris = model.Meshes.Sum(m => m.MeshParts.Sum(p => (long)p.PrimitiveCount));
        context.Logger.Log(string.Format("GodotStaticModel {0}: meshes={1} parts={2} verts={3} tris={4}", input.Name, model.Meshes.Count, model.Meshes.Sum(m => m.MeshParts.Count), verts, tris));
        return model;
    }

    protected override MaterialContent ConvertMaterial(MaterialContent material, ContentProcessorContext context)
    {
        var result = base.ConvertMaterial(material, context);
        result.Name = material.Name;
        return result;
    }

    static void AddTangents(NodeContent node, ContentProcessorContext context)
    {
        if (node is MeshContent mesh)
            foreach (var g in mesh.Geometry)
            {
                if (g.Vertices.Channels.Contains(VertexChannelNames.Tangent(0))) continue;
                if (!g.Vertices.Channels.Contains(VertexChannelNames.Normal()) || !g.Vertices.Channels.Contains(VertexChannelNames.TextureCoordinate(0)))
                {
                    context.Logger.Log(string.Format("warning: Geometry in '{0}' lacks normals or UV0: no tangents generated", mesh.Name));
                    continue;
                }
                g.Vertices.Channels.Add(VertexChannelNames.Tangent(0), ComputeTangents(mesh, g));
            }
        foreach (var c in node.Children) AddTangents(c, context);
    }

    static Vector4[] ComputeTangents(MeshContent mesh, GeometryContent g)
    {
        int n = g.Vertices.VertexCount;
        var nrm = g.Vertices.Channels.Get<Vector3>(VertexChannelNames.Normal());
        var uv = g.Vertices.Channels.Get<Vector2>(VertexChannelNames.TextureCoordinate(0));
        var pos = new Vector3[n];
        for (int i = 0; i < n; i++) pos[i] = mesh.Positions[g.Vertices.PositionIndices[i]];
        var tan = new Vector3[n]; var bit = new Vector3[n];
        for (int t = 0; t + 2 < g.Indices.Count; t += 3)
        {
            int i0 = g.Indices[t], i1 = g.Indices[t + 1], i2 = g.Indices[t + 2];
            Vector3 e1 = pos[i1] - pos[i0], e2 = pos[i2] - pos[i0];
            Vector2 d1 = uv[i1] - uv[i0], d2 = uv[i2] - uv[i0];
            float det = d1.X * d2.Y - d2.X * d1.Y;
            if (MathF.Abs(det) < 1e-12f) continue;
            float r = 1f / det;
            // Per-triangle accumulation weighted by triangle area (|e1 x e2|): longer cross product = larger weight.
            float w = Vector3.Cross(e1, e2).Length();
            Vector3 sdir = (e1 * d2.Y - e2 * d1.Y) * r, tdir = (e2 * d1.X - e1 * d2.X) * r;
            if (w > 0) { sdir.Normalize(); tdir.Normalize(); sdir *= w; tdir *= w; }
            foreach (int i in new[] { i0, i1, i2 }) { tan[i] += sdir; bit[i] += tdir; }
        }
        var res = new Vector4[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 nn = nrm[i];
            Vector3 t = tan[i] - nn * Vector3.Dot(nn, tan[i]);
            if (t.LengthSquared() < 1e-12f)
            {   // degenerate UVs: any vector perpendicular to the normal
                t = MathF.Abs(nn.X) < 0.9f ? Vector3.Cross(nn, Vector3.UnitX) : Vector3.Cross(nn, Vector3.UnitY);
            }
            t.Normalize();
            float sign = Vector3.Dot(Vector3.Cross(nn, t), bit[i]) < 0 ? -1f : 1f;
            res[i] = new Vector4(t, sign);
        }
        return res;
    }
}
