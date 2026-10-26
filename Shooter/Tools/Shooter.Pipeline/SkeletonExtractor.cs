using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace Shooter.Pipeline
{
    /// <summary>Finds the skeleton in an imported FBX scene and converts its bind pose.</summary>
    public static class SkeletonExtractor
    {
        /// <summary>
        /// MonoGame's Assimp importer returns plain NodeContent (no BoneContent), so the skeleton root is derived from
        /// the skin weights: the top-most non-root ancestor shared by every weighted bone.
        /// </summary>
        public static NodeContent FindSkeletonRoot(NodeContent scene)
        {
            HashSet<string> weighted = new HashSet<string>(StringComparer.Ordinal);
            CollectWeightedBones(scene, weighted);
            if (weighted.Count == 0)
            {
                throw new InvalidContentException("No skin weights found; this FBX has no skinned mesh.");
            }

            NodeContent root = null;
            foreach (string name in weighted)
            {
                NodeContent n = FindNode(scene, name);
                if (n == null)
                {
                    throw new InvalidContentException("Weighted bone '" + name + "' is not a node in the scene.");
                }

                while (n.Parent != null && n.Parent.Parent != null)
                {
                    n = n.Parent;
                }

                if (root == null)
                {
                    root = n;
                }
                else if (!ReferenceEquals(root, n))
                {
                    throw new InvalidContentException(
                        "Skinned bones live under more than one skeleton root ('" + root.Name + "' and '" + n.Name + "').");
                }
            }

            return root;
        }

        private static void CollectWeightedBones(NodeContent node, HashSet<string> names)
        {
            MeshContent mesh = node as MeshContent;
            if (mesh != null)
            {
                foreach (GeometryContent g in mesh.Geometry)
                {
                    foreach (VertexChannel ch in g.Vertices.Channels)
                    {
                        VertexChannel<BoneWeightCollection> bw = ch as VertexChannel<BoneWeightCollection>;
                        if (bw == null)
                        {
                            continue;
                        }

                        foreach (BoneWeightCollection c in bw)
                        {
                            foreach (BoneWeight w in c)
                            {
                                names.Add(w.BoneName);
                            }
                        }
                    }
                }
            }

            foreach (NodeContent child in node.Children)
            {
                CollectWeightedBones(child, names);
            }
        }

        public static NodeContent FindNode(NodeContent node, string name)
        {
            if (string.Equals(node.Name, name, StringComparison.Ordinal))
            {
                return node;
            }

            foreach (NodeContent child in node.Children)
            {
                NodeContent f = FindNode(child, name);
                if (f != null)
                {
                    return f;
                }
            }

            return null;
        }

        /// <summary>Flattens skeletonRoot's subtree parent-first and converts the bind pose.</summary>
        public static List<SkeletonBoneContent> BuildBones(NodeContent skeletonRoot, SpaceConversion space)
        {
            List<SkeletonBoneContent> bones = new List<SkeletonBoneContent>();
            Add(skeletonRoot, -1, space, bones);
            return bones;
        }

        private static void Add(NodeContent node, int parent, SpaceConversion space, List<SkeletonBoneContent> bones)
        {
            Matrix local = space.Local(node.Transform);
            SkeletonBoneContent b = new SkeletonBoneContent();
            b.Name = node.Name;
            b.ParentIndex = parent;
            b.LocalBindPose = local;
            b.GlobalBindPose = parent < 0 ? local : local * bones[parent].GlobalBindPose;
            b.InverseBindPose = Matrix.Invert(b.GlobalBindPose);
            int index = bones.Count;
            bones.Add(b);
            foreach (NodeContent child in node.Children)
            {
                Add(child, index, space, bones);
            }
        }
    }
}
