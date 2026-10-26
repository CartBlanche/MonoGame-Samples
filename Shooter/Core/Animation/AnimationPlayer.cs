using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Shooter.Animation
{
    /// <summary>Local-space TRS pose for every bone of a skeleton.</summary>
    public sealed class Pose
    {
        public readonly Vector3[] Translation;
        public readonly Quaternion[] Rotation;
        public readonly Vector3[] Scale;

        public Pose(int boneCount)
        {
            Translation = new Vector3[boneCount];
            Rotation = new Quaternion[boneCount];
            Scale = new Vector3[boneCount];
        }

        public int BoneCount
        {
            get { return Translation.Length; }
        }

        public void CopyFrom(Pose other)
        {
            Array.Copy(other.Translation, Translation, Translation.Length);
            Array.Copy(other.Rotation, Rotation, Rotation.Length);
            Array.Copy(other.Scale, Scale, Scale.Length);
        }

        public void SetBind(SkinnedModel model)
        {
            Array.Copy(model.BindTranslation, Translation, Translation.Length);
            Array.Copy(model.BindRotation, Rotation, Rotation.Length);
            Array.Copy(model.BindScale, Scale, Scale.Length);
        }
    }

    /// <summary>Playback of one clip inside a layer (Unity: the runtime part of an Animator state).</summary>
    public sealed class AnimationState
    {
        public AnimationClip Clip;

        /// <summary>Seconds since the state started, NOT wrapped (like Unity's normalizedTime * length).</summary>
        public float Time;

        /// <summary>State speed multiplier (Unity AnimatorState.speed * layer/animator speed).</summary>
        public float Speed = 1f;

        public bool Loop;

        public float Duration
        {
            get { return Clip != null ? Clip.Duration : 0f; }
        }

        /// <summary>Unity-style normalizedTime: 1.0 = one clip length, keeps growing while looping.</summary>
        public float NormalizedTime
        {
            get { return Duration > 0f ? Time / Duration : 0f; }
            set { Time = value * Duration; }
        }

        public bool IsFinished
        {
            get { return !Loop && Time >= Duration; }
        }
    }

    /// <summary>
    /// One animation layer: plays (and cross-fades between) clips, optionally masked to a subset of bones,
    /// either overriding or additively adding on top of the layers below it.
    /// </summary>
    public sealed class AnimationLayer
    {
        private readonly AnimationPlayer _owner;
        private AnimationState _current;
        private AnimationState _previous;
        private Pose _frozen;
        private bool _hasFrozen;
        private bool _fadeFromNothing;
        private float _fadeTime;
        private float _fadeDuration;
        private Pose _scratchA;
        private Pose _scratchB;

        internal AnimationLayer(AnimationPlayer owner, int index)
        {
            _owner = owner;
            Index = index;
            Weight = 1f;
        }

        public int Index { get; private set; }

        /// <summary>Layer weight 0..1 (Unity AnimatorLayer default weight / SetLayerWeight).</summary>
        public float Weight { get; set; }

        /// <summary>Unity blending mode: false = Override, true = Additive.</summary>
        public bool Additive { get; set; }

        /// <summary>Per-bone mask (1 = affected). Null = every bone.</summary>
        public float[] BoneMask { get; set; }

        /// <summary>Multiplied into every state's speed on this layer.</summary>
        public float Speed { get; set; } = 1f;

        public AnimationState Current
        {
            get { return _current; }
        }

        public AnimationState Previous
        {
            get { return _previous; }
        }

        public bool IsInTransition
        {
            get { return _fadeDuration > 0f; }
        }

        /// <summary>0..1 progress of the running cross-fade (1 when none is running).</summary>
        public float TransitionProgress
        {
            get { return _fadeDuration > 0f ? MathHelper.Clamp(_fadeTime / _fadeDuration, 0f, 1f) : 1f; }
        }

        /// <summary>True when the layer currently has a motion (a clip or a fade-out of one).</summary>
        public bool HasMotion
        {
            get { return _current != null || _previous != null || _hasFrozen; }
        }

        /// <summary>Starts a clip immediately, no blending. loop &lt; 0 means "use the clip's Loop flag".</summary>
        public AnimationState Play(AnimationClip clip, float normalizedTime = 0f, float speed = 1f, int loop = -1)
        {
            ClearFade();
            _current = MakeState(clip, normalizedTime, speed, loop);
            return _current;
        }

        /// <summary>Stops the layer's motion immediately (Unity: a state with no motion).</summary>
        public void Stop()
        {
            ClearFade();
            _current = null;
        }

        /// <summary>
        /// Unity Animator.CrossFade / transition. duration is seconds when fixedDuration is true, otherwise a fraction of
        /// the source state's length. normalizedOffset is the destination's start time (transition offset).
        /// clip == null fades the layer out (transition into a state without motion).
        /// </summary>
        public AnimationState CrossFade(
            AnimationClip clip,
            float duration,
            float normalizedOffset = 0f,
            float speed = 1f,
            bool fixedDuration = true,
            int loop = -1)
        {
            if (!fixedDuration && _current != null)
            {
                duration *= _current.Duration;
            }

            if (duration <= 0f || (!HasMotion && clip == null))
            {
                if (clip == null)
                {
                    Stop();
                    return null;
                }

                return Play(clip, normalizedOffset, speed, loop);
            }

            if (IsInTransition)
            {
                // Interrupted: freeze the current blended pose as the new source (Unity "interruption").
                Pose snapshot = new Pose(_owner.Model.BoneCount);
                float infl;
                EvaluateInto(snapshot, out infl);
                _frozen = snapshot;
                _hasFrozen = true;
                _fadeFromNothing = false;
                _previous = null;
            }
            else
            {
                _previous = _current;
                _fadeFromNothing = _current == null;
                _hasFrozen = false;
            }

            _current = clip != null ? MakeState(clip, normalizedOffset, speed, loop) : null;
            _fadeDuration = duration;
            _fadeTime = 0f;
            return _current;
        }

        internal void Advance(float dt)
        {
            dt *= Speed;
            Step(_current, dt);
            Step(_previous, dt);
            if (_fadeDuration > 0f)
            {
                _fadeTime += dt;
                if (_fadeTime >= _fadeDuration)
                {
                    ClearFade();
                }
            }
        }

        private static void Step(AnimationState s, float dt)
        {
            if (s != null)
            {
                s.Time += dt * s.Speed;
            }
        }

        private void ClearFade()
        {
            _previous = null;
            _hasFrozen = false;
            _fadeFromNothing = false;
            _fadeDuration = 0f;
            _fadeTime = 0f;
        }

        private static AnimationState MakeState(AnimationClip clip, float normalizedTime, float speed, int loop)
        {
            AnimationState s = new AnimationState
            {
                Clip = clip,
                Speed = speed,
                Loop = loop < 0 ? clip.Loop : loop != 0
            };
            s.NormalizedTime = normalizedTime;
            return s;
        }

        /// <summary>Evaluates the layer's own motion (current + fade). influence is the fade-in/out factor 0..1.</summary>
        internal bool EvaluateInto(Pose dst, out float influence)
        {
            influence = 1f;
            SkinnedModel model = _owner.Model;
            float p = TransitionProgress;

            if (_current != null)
            {
                _owner.SampleState(_current, dst);
                if (_fadeDuration > 0f)
                {
                    if (_previous != null || _hasFrozen)
                    {
                        if (_scratchB == null)
                        {
                            _scratchB = new Pose(model.BoneCount);
                        }

                        if (_previous != null)
                        {
                            _owner.SampleState(_previous, _scratchB);
                        }
                        else
                        {
                            _scratchB.CopyFrom(_frozen);
                        }

                        AnimationPlayer.Blend(_scratchB, dst, p, dst);
                    }
                    else if (_fadeFromNothing)
                    {
                        influence = p;
                    }
                }

                return true;
            }

            if (_fadeDuration > 0f && (_previous != null || _hasFrozen))
            {
                if (_previous != null)
                {
                    _owner.SampleState(_previous, dst);
                }
                else
                {
                    dst.CopyFrom(_frozen);
                }

                influence = 1f - p;
                return true;
            }

            return false;
        }

        /// <summary>Composes this layer on top of 'accum'.</summary>
        internal void Apply(Pose accum)
        {
            if (Weight <= 0f)
            {
                return;
            }

            if (_scratchA == null)
            {
                _scratchA = new Pose(_owner.Model.BoneCount);
            }

            float influence;
            if (!EvaluateInto(_scratchA, out influence))
            {
                return;
            }

            SkinnedModel model = _owner.Model;
            float w = Weight * influence;
            for (int b = 0; b < accum.BoneCount; b++)
            {
                float m = BoneMask != null ? BoneMask[b] * w : w;
                if (m <= 0f)
                {
                    continue;
                }

                if (!Additive)
                {
                    accum.Translation[b] = Vector3.Lerp(accum.Translation[b], _scratchA.Translation[b], m);
                    accum.Rotation[b] = Quaternion.Slerp(accum.Rotation[b], _scratchA.Rotation[b], m);
                    accum.Scale[b] = Vector3.Lerp(accum.Scale[b], _scratchA.Scale[b], m);
                }
                else
                {
                    // Delta of the clip pose from the bind pose, scaled by weight, applied on top of the accumulated pose.
                    Vector3 dt = _scratchA.Translation[b] - model.BindTranslation[b];
                    Quaternion dr = Quaternion.Concatenate(
                        Quaternion.Inverse(model.BindRotation[b]), _scratchA.Rotation[b]);
                    dr = Quaternion.Slerp(Quaternion.Identity, dr, m);
                    Vector3 bs = model.BindScale[b];
                    Vector3 ds = new Vector3(
                        bs.X != 0f ? _scratchA.Scale[b].X / bs.X : 1f,
                        bs.Y != 0f ? _scratchA.Scale[b].Y / bs.Y : 1f,
                        bs.Z != 0f ? _scratchA.Scale[b].Z / bs.Z : 1f);
                    accum.Translation[b] += dt * m;
                    // Quaternion.Concatenate(a, b) applies a then b.
                    accum.Rotation[b] = Quaternion.Concatenate(dr, accum.Rotation[b]);
                    accum.Scale[b] *= Vector3.Lerp(Vector3.One, ds, m);
                }
            }
        }
    }

    /// <summary>
    /// Plays clips on a SkinnedModel and produces bone matrices. Layer 0 is the base layer (override, weight 1).
    /// Typical use: player.Play(clip) / player.CrossFade(clip, 0.1f); player.Update(dt); player.CopySkinMatricesTo(palette).
    /// </summary>
    public sealed class AnimationPlayer
    {
        private readonly List<AnimationLayer> _layers = new List<AnimationLayer>();
        private readonly Dictionary<AnimationClip, int[]> _bindings = new Dictionary<AnimationClip, int[]>();
        private readonly Pose _pose;
        private readonly Matrix[] _global;
        private readonly Matrix[] _skin;
        private bool _dirty = true;

        public AnimationPlayer(SkinnedModel model)
        {
            Model = model;
            _pose = new Pose(model.BoneCount);
            _global = new Matrix[model.BoneCount];
            _skin = new Matrix[model.BoneCount];
            _layers.Add(new AnimationLayer(this, 0));
            Evaluate();
        }

        public SkinnedModel Model { get; private set; }

        public AnimationLayer BaseLayer
        {
            get { return _layers[0]; }
        }

        public IReadOnlyList<AnimationLayer> Layers
        {
            get { return _layers; }
        }

        /// <summary>Global speed multiplier (Unity Animator.speed).</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>Latest evaluated local-space pose.</summary>
        public Pose LocalPose
        {
            get { return _pose; }
        }

        /// <summary>Adds a layer. maskBones lists bone names that the layer affects (null = all bones).</summary>
        public AnimationLayer AddLayer(float weight, bool additive, IEnumerable<string> maskBones)
        {
            AnimationLayer l = new AnimationLayer(this, _layers.Count);
            l.Weight = weight;
            l.Additive = additive;
            if (maskBones != null)
            {
                float[] mask = new float[Model.BoneCount];
                foreach (string name in maskBones)
                {
                    int i = Model.GetBoneIndex(name);
                    if (i >= 0)
                    {
                        mask[i] = 1f;
                    }
                }

                l.BoneMask = mask;
            }

            _layers.Add(l);
            return l;
        }

        public AnimationState Play(AnimationClip clip, float normalizedTime = 0f, float speed = 1f)
        {
            _dirty = true;
            return BaseLayer.Play(clip, normalizedTime, speed);
        }

        public AnimationState CrossFade(
            AnimationClip clip, float duration, float normalizedOffset = 0f, float speed = 1f, bool fixedDuration = true)
        {
            _dirty = true;
            return BaseLayer.CrossFade(clip, duration, normalizedOffset, speed, fixedDuration);
        }

        public void Update(float dt)
        {
            dt *= Speed;
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].Advance(dt);
            }

            Evaluate();
        }

        /// <summary>Re-evaluates the pose without advancing time (after Play/CrossFade or manual time edits).</summary>
        public void Evaluate()
        {
            _pose.SetBind(Model);
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].Apply(_pose);
            }

            RecomputeGlobals();
        }

        /// <summary>Rebuilds the model-space bone transforms and skin matrices from the current local pose (after a script edited <see cref="LocalPose"/>, like Unity's LateUpdate).</summary>
        public void RecomputeGlobals()
        {
            SkinnedModel m = Model;
            for (int i = 0; i < _global.Length; i++)
            {
                Matrix local = Matrix.CreateScale(_pose.Scale[i])
                    * Matrix.CreateFromQuaternion(Quaternion.Normalize(_pose.Rotation[i]))
                    * Matrix.CreateTranslation(_pose.Translation[i]);
                _global[i] = m.ParentIndices[i] < 0 ? local : local * _global[m.ParentIndices[i]];
                _skin[i] = m.InverseBindPose[i] * _global[i];
            }

            _dirty = false;
        }

        /// <summary>
        /// Sets a bone's model-space rotation (its parent chain is left as evaluated) by editing its local rotation, then rebuilds the
        /// transforms: the equivalent of assigning Transform.rotation in LateUpdate. Row-vector convention: global = local * parentGlobal.
        /// </summary>
        public void SetBoneModelRotation(int bone, Quaternion modelRotation)
        {
            int parent = Model.ParentIndices[bone];
            Quaternion parentRot = parent < 0 ? Quaternion.Identity : Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(NormalizedRotation(_global[parent])));
            _pose.Rotation[bone] = Quaternion.Concatenate(Quaternion.Normalize(modelRotation), Quaternion.Inverse(parentRot));
            RecomputeGlobals();
        }

        /// <summary>Model-space bone transforms of the bind pose (the pose a scene is in before the Animator's first update).</summary>
        public Matrix[] EvaluateBindGlobals()
        {
            Pose bind = new Pose(Model.BoneCount);
            bind.SetBind(Model);
            var result = new Matrix[Model.BoneCount];
            for (int i = 0; i < result.Length; i++)
            {
                Matrix local = Matrix.CreateScale(bind.Scale[i])
                    * Matrix.CreateFromQuaternion(Quaternion.Normalize(bind.Rotation[i]))
                    * Matrix.CreateTranslation(bind.Translation[i]);
                result[i] = Model.ParentIndices[i] < 0 ? local : local * result[Model.ParentIndices[i]];
            }
            return result;
        }

        private static Matrix NormalizedRotation(Matrix m)
        {
            Vector3 x = new Vector3(m.M11, m.M12, m.M13), y = new Vector3(m.M21, m.M22, m.M23), z = new Vector3(m.M31, m.M32, m.M33);
            x.Normalize(); y.Normalize(); z.Normalize();
            return new Matrix(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1);
        }

        /// <summary>Bone matrices for skinning (row-vector: v * skin[i]), length = BoneCount.</summary>
        public Matrix[] SkinMatrices
        {
            get
            {
                if (_dirty)
                {
                    Evaluate();
                }

                return _skin;
            }
        }

        /// <summary>Current model-space bone transforms, length = BoneCount.</summary>
        public Matrix[] GlobalPose
        {
            get
            {
                if (_dirty)
                {
                    Evaluate();
                }

                return _global;
            }
        }

        /// <summary>Copies skin matrices into a palette array (extra entries become identity).</summary>
        public void CopySkinMatricesTo(Matrix[] palette)
        {
            Matrix[] s = SkinMatrices;
            int n = Math.Min(s.Length, palette.Length);
            Array.Copy(s, palette, n);
            for (int i = n; i < palette.Length; i++)
            {
                palette[i] = Matrix.Identity;
            }
        }

        /// <summary>Model-space transform of a named bone (attach VFX / muzzle points here).</summary>
        public Matrix GetBoneTransform(string boneName)
        {
            int i = Model.GetBoneIndex(boneName);
            if (i < 0)
            {
                throw new ArgumentException("Unknown bone: " + boneName);
            }

            return GlobalPose[i];
        }

        internal void SampleState(AnimationState s, Pose dst)
        {
            dst.SetBind(Model);
            AnimationClip clip = s.Clip;
            int[] map = GetBinding(clip);
            float t = clip.WrapTime(s.Time, s.Loop);
            for (int b = 0; b < map.Length; b++)
            {
                int ti = map[b];
                if (ti < 0)
                {
                    continue;
                }

                clip.Tracks[ti].Sample(t, out dst.Translation[b], out dst.Rotation[b], out dst.Scale[b]);
            }
        }

        private int[] GetBinding(AnimationClip clip)
        {
            int[] map;
            if (_bindings.TryGetValue(clip, out map))
            {
                return map;
            }

            map = new int[Model.BoneCount];
            for (int b = 0; b < map.Length; b++)
            {
                map[b] = clip.FindTrack(Model.BoneNames[b]);
            }

            _bindings[clip] = map;
            return map;
        }

        internal static void Blend(Pose a, Pose b, float t, Pose dst)
        {
            for (int i = 0; i < dst.BoneCount; i++)
            {
                dst.Translation[i] = Vector3.Lerp(a.Translation[i], b.Translation[i], t);
                dst.Rotation[i] = Quaternion.Slerp(a.Rotation[i], b.Rotation[i], t);
                dst.Scale[i] = Vector3.Lerp(a.Scale[i], b.Scale[i], t);
            }
        }
    }
}
