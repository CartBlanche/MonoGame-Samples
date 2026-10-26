using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace Shooter.Pipeline
{
    /// <summary>
    /// NodeContent (FBX take) -> AnimationClipContent.
    ///
    /// Storage choice: the source keys are kept as authored (Maya exports one key per frame at 30 fps, so this is
    /// already a dense sampling), converted to node-local translation / rotation / scale per bone, rotation signs made
    /// continuous, and constant tracks collapsed to a single key. The runtime interpolates linearly (slerp for
    /// rotation). Nothing is resampled, so playback matches the source exactly at every authored frame.
    /// Every take in the Unity project is exactly the clip range selected in the .meta files (verified), so the whole
    /// take is exported.
    /// </summary>
    [ContentProcessor(DisplayName = "Animation Clip - Shooter")]
    public sealed class AnimationClipProcessor : ContentProcessor<NodeContent, AnimationClipContent>
    {
        [DefaultValue(CoordinateSpace.MonoGame)]
        public CoordinateSpace Space { get; set; } = CoordinateSpace.MonoGame;

        [DefaultValue(0.01f)]
        public float Scale { get; set; } = 0.01f;

        /// <summary>Clip name; empty = text after '@' in the file name (Unity's name for model@clip.fbx).</summary>
        [DefaultValue("")]
        public string ClipName { get; set; } = "";

        /// <summary>Unity "Loop Time" flag from the .meta (clipAnimations[].loopTime).</summary>
        [DefaultValue(false)]
        public bool LoopTime { get; set; }

        /// <summary>Source frame rate; 0 = detect from key spacing.</summary>
        [DefaultValue(0f)]
        public float FrameRate { get; set; }

        /// <summary>Tracks whose keys differ by less than this are collapsed to one key.</summary>
        [DefaultValue(1e-5f)]
        public float ConstantEpsilon { get; set; } = 1e-5f;

        public override AnimationClipContent Process(NodeContent input, ContentProcessorContext context)
        {
            SpaceConversion space = new SpaceConversion(Space, Scale);

            AnimationClipContent clip = new AnimationClipContent();
            clip.Name = ClipName;
            if (string.IsNullOrEmpty(clip.Name))
            {
                string file = Path.GetFileNameWithoutExtension(input.Identity.SourceFilename ?? "clip");
                int at = file.IndexOf('@');
                clip.Name = at >= 0 ? file.Substring(at + 1) : file;
            }

            clip.Loop = LoopTime;

            List<NodeContent> animated = new List<NodeContent>();
            Collect(input, animated);
            if (animated.Count == 0)
            {
                throw new InvalidContentException("No animation channels in " + input.Identity.SourceFilename);
            }

            double duration = 0;
            double minStep = double.MaxValue;
            foreach (NodeContent node in animated)
            {
                AnimationChannel channel = FirstChannel(node);
                for (int k = 0; k < channel.Count; k++)
                {
                    duration = Math.Max(duration, channel[k].Time.TotalSeconds);
                    if (k > 0)
                    {
                        double step = channel[k].Time.TotalSeconds - channel[k - 1].Time.TotalSeconds;
                        if (step > 1e-6)
                        {
                            minStep = Math.Min(minStep, step);
                        }
                    }
                }
            }

            clip.Duration = (float)duration;
            clip.FrameRate = FrameRate > 0f ? FrameRate : (minStep < double.MaxValue ? (float)Math.Round(1.0 / minStep) : 30f);

            foreach (NodeContent node in animated)
            {
                clip.Tracks.Add(BuildTrack(node, FirstChannel(node), space));
            }

            // Root motion: translation of the skeleton root bone (direct child of the scene root, e.g. Robot_Root_C_Jnt,
            // which is the avatar's rootMotionBoneName) over the clip. None of the shipped clips animate it.
            foreach (BoneTrackContent t in clip.Tracks)
            {
                NodeContent n = animated.Find(a => a.Name == t.BoneName);
                if (n != null && n.Parent != null && n.Parent.Parent == null)
                {
                    clip.RootMotionDelta = t.Translations[t.Translations.Length - 1] - t.Translations[0];
                    clip.HasRootMotion = clip.RootMotionDelta.Length() > 0.1f * Scale; // 0.1 source units (1 mm for cm files)
                }
            }

            context.Logger.LogMessage(
                "Clip '{0}': {1} tracks, {2:0.###}s @ {3} fps, loop={4}, rootMotion={5}",
                clip.Name, clip.Tracks.Count, clip.Duration, clip.FrameRate, clip.Loop, clip.HasRootMotion);
            return clip;
        }

        private static void Collect(NodeContent node, List<NodeContent> list)
        {
            if (node.Animations.Count > 0)
            {
                list.Add(node);
            }

            foreach (NodeContent c in node.Children)
            {
                Collect(c, list);
            }
        }

        private static AnimationChannel FirstChannel(NodeContent node)
        {
            foreach (KeyValuePair<string, AnimationContent> a in node.Animations)
            {
                foreach (KeyValuePair<string, AnimationChannel> c in a.Value.Channels)
                {
                    return c.Value;
                }
            }

            throw new InvalidContentException("Node '" + node.Name + "' has an empty animation.");
        }

        private BoneTrackContent BuildTrack(NodeContent node, AnimationChannel channel, SpaceConversion space)
        {
            int n = channel.Count;
            float[] times = new float[n];
            Vector3[] ts = new Vector3[n];
            Quaternion[] rs = new Quaternion[n];
            Vector3[] ss = new Vector3[n];

            for (int k = 0; k < n; k++)
            {
                times[k] = (float)channel[k].Time.TotalSeconds;
                Matrix local = space.Local(channel[k].Transform);
                Vector3 s, t;
                Quaternion r;
                if (!local.Decompose(out s, out r, out t))
                {
                    t = local.Translation;
                    s = Vector3.One;
                    r = k > 0 ? rs[k - 1] : Quaternion.Identity;
                }

                r = Quaternion.Normalize(r);
                if (k > 0 && Quaternion.Dot(r, rs[k - 1]) < 0f)
                {
                    r = new Quaternion(-r.X, -r.Y, -r.Z, -r.W);
                }

                ts[k] = t;
                rs[k] = r;
                ss[k] = s;
            }

            bool constant = true;
            float e = ConstantEpsilon;
            for (int k = 1; k < n && constant; k++)
            {
                constant = (ts[k] - ts[0]).LengthSquared() < e * e
                    && (ss[k] - ss[0]).LengthSquared() < e * e
                    && Math.Abs(Quaternion.Dot(rs[k], rs[0])) > 1f - e * e;
            }

            BoneTrackContent track = new BoneTrackContent();
            track.BoneName = node.Name;
            if (constant)
            {
                track.Times = new[] { 0f };
                track.Translations = new[] { ts[0] };
                track.Rotations = new[] { rs[0] };
                track.Scales = new[] { ss[0] };
            }
            else
            {
                track.Times = times;
                track.Translations = ts;
                track.Rotations = rs;
                track.Scales = ss;
            }

            return track;
        }
    }
}
