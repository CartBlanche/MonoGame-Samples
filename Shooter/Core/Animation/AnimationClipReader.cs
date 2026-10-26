using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Shooter.Animation
{
    /// <summary>Reads AnimationClip from .xnb. Pairs with Shooter.Pipeline.AnimationClipWriter.</summary>
    public sealed class AnimationClipReader : ContentTypeReader<AnimationClip>
    {
        public const int FormatVersion = 1;

        protected override AnimationClip Read(ContentReader input, AnimationClip existingInstance)
        {
            int version = input.ReadInt32();
            if (version != FormatVersion)
            {
                throw new InvalidOperationException("Unsupported AnimationClip format version " + version);
            }

            AnimationClip clip = new AnimationClip();
            clip.Name = input.ReadString();
            clip.Duration = input.ReadSingle();
            clip.FrameRate = input.ReadSingle();
            clip.Loop = input.ReadBoolean();
            clip.HasRootMotion = input.ReadBoolean();
            clip.RootMotionDelta = input.ReadVector3();

            int trackCount = input.ReadInt32();
            clip.Tracks = new BoneTrack[trackCount];
            for (int t = 0; t < trackCount; t++)
            {
                BoneTrack track = new BoneTrack();
                track.BoneName = input.ReadString();
                int keys = input.ReadInt32();
                track.Times = new float[keys];
                track.Translations = new Vector3[keys];
                track.Rotations = new Quaternion[keys];
                track.Scales = new Vector3[keys];
                for (int k = 0; k < keys; k++)
                {
                    track.Times[k] = input.ReadSingle();
                    track.Translations[k] = input.ReadVector3();
                    track.Rotations[k] = input.ReadQuaternion();
                    track.Scales[k] = input.ReadVector3();
                }

                clip.Tracks[t] = track;
            }

            return clip;
        }
    }
}
