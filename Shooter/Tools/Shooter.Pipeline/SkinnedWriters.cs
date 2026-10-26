using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Shooter.Pipeline
{
    /// <summary>
    /// Writes SkinnedModelContent. The runtime counterpart is Shooter.Animation.SkinnedModelReader (in Shooter.Core);
    /// keep the field order identical.
    /// </summary>
    [ContentTypeWriter]
    public sealed class SkinnedModelWriter : ContentTypeWriter<SkinnedModelContent>
    {
        public const int FormatVersion = 1;

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "Shooter.Animation.SkinnedModelReader, Shooter.Core";
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "Shooter.Animation.SkinnedModel, Shooter.Core";
        }

        protected override void Write(ContentWriter output, SkinnedModelContent value)
        {
            output.Write(FormatVersion);
            output.Write(value.Name ?? "");

            output.Write(value.Bones.Count);
            foreach (SkeletonBoneContent b in value.Bones)
            {
                output.Write(b.Name);
                output.Write(b.ParentIndex);
                output.Write(b.LocalBindPose);
                output.Write(b.InverseBindPose);
            }

            output.Write(value.Parts.Count);
            foreach (SkinnedMeshPartContent p in value.Parts)
            {
                output.Write(p.Name ?? "");
                output.Write(p.MaterialName ?? "");
                output.Write(p.DiffuseTexture ?? "");

                output.Write(p.Vertices.Length);
                foreach (SkinnedVertexContent v in p.Vertices)
                {
                    output.Write(v.Position);
                    output.Write(v.Normal);
                    output.Write(v.TextureCoordinate);
                    output.Write(v.Tangent);
                    output.Write(v.Index0);
                    output.Write(v.Index1);
                    output.Write(v.Index2);
                    output.Write(v.Index3);
                    output.Write(v.Weights);
                }

                bool wide = p.Vertices.Length > ushort.MaxValue;
                output.Write(p.Indices.Length);
                output.Write(wide);
                foreach (int i in p.Indices)
                {
                    if (wide)
                    {
                        output.Write(i);
                    }
                    else
                    {
                        output.Write((ushort)i);
                    }
                }
            }
        }
    }

    /// <summary>Writes AnimationClipContent; runtime counterpart is Shooter.Animation.AnimationClipReader.</summary>
    [ContentTypeWriter]
    public sealed class AnimationClipWriter : ContentTypeWriter<AnimationClipContent>
    {
        public const int FormatVersion = 1;

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "Shooter.Animation.AnimationClipReader, Shooter.Core";
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return "Shooter.Animation.AnimationClip, Shooter.Core";
        }

        protected override void Write(ContentWriter output, AnimationClipContent value)
        {
            output.Write(FormatVersion);
            output.Write(value.Name ?? "");
            output.Write(value.Duration);
            output.Write(value.FrameRate);
            output.Write(value.Loop);
            output.Write(value.HasRootMotion);
            output.Write(value.RootMotionDelta);

            output.Write(value.Tracks.Count);
            foreach (BoneTrackContent t in value.Tracks)
            {
                output.Write(t.BoneName);
                output.Write(t.Times.Length);
                for (int k = 0; k < t.Times.Length; k++)
                {
                    output.Write(t.Times[k]);
                    output.Write(t.Translations[k]);
                    output.Write(t.Rotations[k]);
                    output.Write(t.Scales[k]);
                }
            }
        }
    }
}
