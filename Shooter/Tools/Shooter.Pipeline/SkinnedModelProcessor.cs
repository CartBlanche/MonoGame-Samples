using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace Shooter.Pipeline
{
    /// <summary>
    /// NodeContent (skinned FBX) -> SkinnedModelContent: skeleton (hierarchy, bind pose, inverse bind pose),
    /// one mesh part per FBX geometry with position, normal, UV, tangent and 4 bone indices/weights.
    /// </summary>
    [ContentProcessor(DisplayName = "Skinned Model - Shooter")]
    public sealed class SkinnedModelProcessor : ContentProcessor<NodeContent, SkinnedModelContent>
    {
        public const int MaxInfluences = 4;

        [DefaultValue(CoordinateSpace.MonoGame)]
        public CoordinateSpace Space { get; set; } = CoordinateSpace.MonoGame;

        /// <summary>Uniform scale. Unity applies 0.01 to centimetre FBX files (.meta useFileScale).</summary>
        [DefaultValue(0.01f)]
        public float Scale { get; set; } = 0.01f;

        /// <summary>Generate tangents from the UVs (Unity: tangentImportMode = calculate).</summary>
        [DefaultValue(true)]
        public bool GenerateTangents { get; set; } = true;

        /// <summary>
        /// Reverse triangle winding. Off by default: MonoGame's Assimp importer already delivers clockwise front faces and the
        /// default MonoGame space conversion is a proper rotation (RotationY(180)), so index order never needs to change.
        /// </summary>
        [DefaultValue(false)]
        public bool SwapWindingOrder { get; set; }

        public override SkinnedModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            SpaceConversion space = new SpaceConversion(Space, Scale);

            NodeContent skeletonRoot = SkeletonExtractor.FindSkeletonRoot(input);
            SkinnedModelContent output = new SkinnedModelContent();
            output.Name = System.IO.Path.GetFileNameWithoutExtension(input.Identity.SourceFilename ?? "") is string n && n.Length > 0 ? n : (input.Name ?? "");
            output.Bones = SkeletonExtractor.BuildBones(skeletonRoot, space);

            Dictionary<string, int> boneIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < output.Bones.Count; i++)
            {
                boneIndex[output.Bones[i].Name] = i;
            }

            ProcessMeshes(input, space, boneIndex, output, context);
            if (output.Parts.Count == 0)
            {
                throw new InvalidContentException("No skinned meshes found in " + input.Identity.SourceFilename);
            }

            return output;
        }

        private void ProcessMeshes(
            NodeContent node,
            SpaceConversion space,
            Dictionary<string, int> boneIndex,
            SkinnedModelContent output,
            ContentProcessorContext context)
        {
            MeshContent mesh = node as MeshContent;
            if (mesh != null)
            {
                ProcessMesh(mesh, space, boneIndex, output, context);
            }

            foreach (NodeContent child in node.Children)
            {
                ProcessMeshes(child, space, boneIndex, output, context);
            }
        }

        private void ProcessMesh(
            MeshContent mesh,
            SpaceConversion space,
            Dictionary<string, int> boneIndex,
            SkinnedModelContent output,
            ContentProcessorContext context)
        {
            if (mesh.AbsoluteTransform != Matrix.Identity)
            {
                context.Logger.LogWarning(null, mesh.Identity,
                    "Mesh '{0}' has a non-identity node transform; skinned meshes are assumed to be in bind space and it is ignored.",
                    mesh.Name);
            }

            // 1. convert geometry into the target space (in place; positions are shared with the geometries)
            for (int i = 0; i < mesh.Positions.Count; i++)
            {
                mesh.Positions[i] = space.Position(mesh.Positions[i]);
            }

            foreach (GeometryContent geom in mesh.Geometry)
            {
                if (geom.Vertices.Channels.Contains(VertexChannelNames.Normal()))
                {
                    VertexChannel<Vector3> normals = geom.Vertices.Channels.Get<Vector3>(VertexChannelNames.Normal());
                    for (int i = 0; i < normals.Count; i++)
                    {
                        normals[i] = Vector3.Normalize(space.Direction(normals[i]));
                    }
                }
            }

            // 2. tangents (after conversion so handedness is right for the final space)
            bool hasTangents = false;
            if (GenerateTangents)
            {
                try
                {
                    MeshHelper.CalculateTangentFrames(
                        mesh,
                        VertexChannelNames.TextureCoordinate(0),
                        VertexChannelNames.Tangent(0),
                        VertexChannelNames.Binormal(0));
                    hasTangents = true;
                }
                catch (Exception ex)
                {
                    context.Logger.LogWarning(null, mesh.Identity, "Tangent generation failed for '{0}': {1}", mesh.Name, ex.Message);
                }
            }

            // 3. emit one part per geometry
            int partIndex = 0;
            foreach (GeometryContent geom in mesh.Geometry)
            {
                output.Parts.Add(BuildPart(mesh, geom, partIndex++, hasTangents, boneIndex, context, SwapWindingOrder));
            }
        }

        private static SkinnedMeshPartContent BuildPart(
            MeshContent mesh,
            GeometryContent geom,
            int partIndex,
            bool hasTangents,
            Dictionary<string, int> boneIndex,
            ContentProcessorContext context,
            bool swapWinding)
        {
            VertexChannelCollection channels = geom.Vertices.Channels;
            VertexChannel<Vector3> normals = channels.Contains(VertexChannelNames.Normal())
                ? channels.Get<Vector3>(VertexChannelNames.Normal()) : null;
            VertexChannel<Vector2> uvs = channels.Contains(VertexChannelNames.TextureCoordinate(0))
                ? channels.Get<Vector2>(VertexChannelNames.TextureCoordinate(0)) : null;
            VertexChannel<Vector3> tangents = hasTangents && channels.Contains(VertexChannelNames.Tangent(0))
                ? channels.Get<Vector3>(VertexChannelNames.Tangent(0)) : null;
            VertexChannel<Vector3> binormals = hasTangents && channels.Contains(VertexChannelNames.Binormal(0))
                ? channels.Get<Vector3>(VertexChannelNames.Binormal(0)) : null;
            VertexChannel<BoneWeightCollection> weights = channels.Contains(VertexChannelNames.Weights(0))
                ? channels.Get<BoneWeightCollection>(VertexChannelNames.Weights(0)) : null;
            if (weights == null)
            {
                throw new InvalidContentException("Geometry in mesh '" + mesh.Name + "' has no bone weights.");
            }

            SkinnedMeshPartContent part = new SkinnedMeshPartContent();
            part.Name = partIndex == 0 && mesh.Geometry.Count == 1 ? mesh.Name : mesh.Name + "_" + partIndex;
            part.MaterialName = geom.Material != null ? geom.Material.Name : "";
            part.DiffuseTexture = "";
            if (geom.Material != null)
            {
                foreach (KeyValuePair<string, ExternalReference<TextureContent>> kv in geom.Material.Textures)
                {
                    if (kv.Value != null && (kv.Key == "Texture" || part.DiffuseTexture.Length == 0))
                    {
                        part.DiffuseTexture = System.IO.Path.GetFileName(kv.Value.Filename ?? "");
                    }
                }
            }

            int vc = geom.Vertices.VertexCount;
            part.Vertices = new SkinnedVertexContent[vc];
            int unweighted = 0;
            for (int v = 0; v < vc; v++)
            {
                SkinnedVertexContent sv = new SkinnedVertexContent();
                sv.Position = geom.Vertices.Positions[v];
                sv.Normal = normals != null ? normals[v] : Vector3.Up;
                sv.TextureCoordinate = uvs != null ? uvs[v] : Vector2.Zero;

                if (tangents != null && binormals != null)
                {
                    Vector3 n = sv.Normal;
                    Vector3 t = tangents[v];
                    t -= n * Vector3.Dot(n, t);
                    t = t.LengthSquared() > 1e-12f ? Vector3.Normalize(t) : Vector3.UnitX;
                    float w = Vector3.Dot(Vector3.Cross(n, t), binormals[v]) < 0f ? -1f : 1f;
                    sv.Tangent = new Vector4(t, w);
                }
                else
                {
                    sv.Tangent = new Vector4(1f, 0f, 0f, 1f);
                }

                BoneWeightCollection bw = weights[v];
                if (bw.Count > MaxInfluences)
                {
                    bw.NormalizeWeights(MaxInfluences);
                }
                else if (bw.Count > 0)
                {
                    bw.NormalizeWeights();
                }

                float[] w4 = new float[4];
                byte[] i4 = new byte[4];
                int k = 0;
                for (int j = 0; j < bw.Count && k < 4; j++)
                {
                    int bi;
                    if (!boneIndex.TryGetValue(bw[j].BoneName, out bi))
                    {
                        throw new InvalidContentException("Vertex references unknown bone '" + bw[j].BoneName + "'.");
                    }

                    i4[k] = (byte)bi;
                    w4[k] = bw[j].Weight;
                    k++;
                }

                if (k == 0)
                {
                    unweighted++;
                    i4[0] = 0;
                    w4[0] = 1f;
                }

                sv.Index0 = i4[0];
                sv.Index1 = i4[1];
                sv.Index2 = i4[2];
                sv.Index3 = i4[3];
                sv.Weights = new Vector4(w4[0], w4[1], w4[2], w4[3]);
                part.Vertices[v] = sv;
            }

            if (unweighted > 0)
            {
                context.Logger.LogWarning(null, mesh.Identity,
                    "{0} vertices in '{1}' had no bone weights and were bound to bone 0.", unweighted, mesh.Name);
            }

            part.Indices = new int[geom.Indices.Count];
            for (int i = 0; i < part.Indices.Length; i++)
            {
                part.Indices[i] = geom.Indices[i];
            }

            if (swapWinding)
            {
                for (int i = 0; i + 2 < part.Indices.Length; i += 3)
                {
                    int tmp = part.Indices[i + 1];
                    part.Indices[i + 1] = part.Indices[i + 2];
                    part.Indices[i + 2] = tmp;
                }
            }

            return part;
        }
    }
}
