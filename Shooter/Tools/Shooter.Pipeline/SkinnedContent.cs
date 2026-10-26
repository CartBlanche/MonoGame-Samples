using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Shooter.Pipeline
{
    /// <summary>One skeleton node (bone or helper joint), parent-first order.</summary>
    public sealed class SkeletonBoneContent
    {
        public string Name;
        public int ParentIndex;
        public Matrix LocalBindPose;
        public Matrix GlobalBindPose;
        public Matrix InverseBindPose;
    }

    public struct SkinnedVertexContent
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Vector4 Tangent;
        public byte Index0, Index1, Index2, Index3;
        public Vector4 Weights;
    }

    public sealed class SkinnedMeshPartContent
    {
        public string Name;
        public string MaterialName;
        public string DiffuseTexture;
        public SkinnedVertexContent[] Vertices;
        public int[] Indices;
    }

    /// <summary>Processor output for a skinned FBX; written by SkinnedModelWriter.</summary>
    public sealed class SkinnedModelContent
    {
        public string Name;
        public List<SkeletonBoneContent> Bones = new List<SkeletonBoneContent>();
        public List<SkinnedMeshPartContent> Parts = new List<SkinnedMeshPartContent>();
    }

    public sealed class BoneTrackContent
    {
        public string BoneName;
        public float[] Times;
        public Vector3[] Translations;
        public Quaternion[] Rotations;
        public Vector3[] Scales;
    }

    /// <summary>Processor output for one animation clip; written by AnimationClipWriter.</summary>
    public sealed class AnimationClipContent
    {
        public string Name;
        public float Duration;
        public float FrameRate;
        public bool Loop;
        public bool HasRootMotion;
        public Vector3 RootMotionDelta;
        public List<BoneTrackContent> Tracks = new List<BoneTrackContent>();
    }
}
