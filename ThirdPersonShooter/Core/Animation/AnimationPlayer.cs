using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Generic skeletal pose blender + root-motion extractor for a <see cref="SkinnedModel"/> (no player-specific code: the red robot reuses it).
    /// Per frame the owner (a state machine such as <see cref="PlayerAnimator"/>) calls BeginFrame, Add(...) once per playing clip
    /// (clip, new time, previous time, blend weight), optionally AddAdditive(...) and Finish. Finish normalises the weights, blends TRS
    /// (lerp T/S, nlerp R with sign fix), applies additive layers, composes the hierarchy and fills <see cref="Skin"/> (inverseBind * modelPose).
    /// Root motion (Godot AnimationTree.get_root_motion_position): every Add() also accumulates weight * (rootT(now) - rootT(prev)) of the
    /// "root" bone track (loop wrap handled), moved into model space through the root's parent chain (bone scales, armature rotation).
    /// The root bone itself is kept at its bind translation in the visual pose (Godot removes the root-motion track from the pose),
    /// so the mesh never moves; the owner moves the body with <see cref="RootMotion"/>.
    /// </summary>
    public sealed class AnimationPlayer
    {
        readonly SkinnedModel _m;
        readonly Vector3[] _t, _s; readonly Quaternion[] _r;
        readonly Vector3[] _accT, _accS; readonly Quaternion[] _accR; float _accW;
        readonly Matrix[] _model;
        public readonly Matrix[] Skin;
        readonly int _root; readonly Matrix _rootParentToModel;
        // legacy single-clip playback (--fly, --pose)
        public SkinnedClip Clip; public float Time; public bool Loop = true;

        /// <summary>Root-bone translation delta of the last frame in model space (glb axes: front +Z), summed over the Add()ed clips.</summary>
        public Vector3 RootMotion { get; private set; }
        /// <summary>Extra multiplier on <see cref="RootMotion"/> (plan 03 section 3 "RootMotionScale"). The bone-chain scale (Robot_Skeleton 0.803991) is already included.</summary>
        public float RootMotionScale = 1f;
        public int RootBone => _root;

        /// <summary>Root bone yaw delta of the last frame about model +Y (radians, Godot sign: positive turns +Z toward +X). Only filled when <see cref="ExtractRootRotation"/> is set (robots: AnimationTree.get_root_motion_rotation).</summary>
        public float RootYaw { get; private set; }
        /// <summary>Opt-in (the player ignores root rotation): sum weight * root rotation delta and keep the root bone at bind rotation in the visual pose.</summary>
        public bool ExtractRootRotation;
        float _yawAcc;

        public AnimationPlayer(SkinnedModel m, string rootBone = "root")
        {
            _m = m; int n = m.BoneNames.Length;
            _t = new Vector3[n]; _r = new Quaternion[n]; _s = new Vector3[n]; _model = new Matrix[n]; Skin = new Matrix[n];
            _sT = new Vector3[n]; _sS = new Vector3[n]; _sR = new Quaternion[n];
            _accT = new Vector3[n]; _accR = new Quaternion[n]; _accS = new Vector3[n];
            _root = m.BoneIndex(rootBone);
            // chain of the root's parents in the bind pose: root local translation -> model space
            _rootParentToModel = Matrix.Identity;
            if (_root >= 0 && m.Parent[_root] >= 0)
            {
                var chain = new List<int>(); for (int b = m.Parent[_root]; b >= 0; b = m.Parent[b]) chain.Add(b);
                Matrix mat = Matrix.Identity;
                for (int i = chain.Count - 1; i >= 0; i--) { int b = chain[i]; mat = Matrix.CreateScale(m.BindS[b]) * Matrix.CreateFromQuaternion(m.BindR[b]) * Matrix.CreateTranslation(m.BindT[b]) * mat; }
                mat.Translation = Vector3.Zero; _rootParentToModel = mat; // directions only
            }
        }

        // ---------------------------------------------------------------- blending API
        public void BeginFrame()
        {
            Array.Clear(_accT); Array.Clear(_accS); Array.Clear(_accR); _accW = 0; RootMotion = Vector3.Zero; _delta = Vector3.Zero; _yawAcc = 0; RootYaw = 0; _overlays.Clear();
        }
        Vector3 _delta;

        /// <summary>Adds one playing clip. Times are un-wrapped playback times (loop wrap/clamp handled here); weight need not be normalised.</summary>
        public void Add(SkinnedClip c, float time, float prevTime, float weight)
        {
            if (weight <= 1e-5f) return;
            Sample(c, time, out var T, out var R, out var S);
            int n = _t.Length;
            for (int b = 0; b < n; b++)
            {
                _accT[b] += T[b] * weight; _accS[b] += S[b] * weight;
                var q = R[b]; if (_accW > 0 && Quaternion.Dot(q, _accR[b]) < 0) q = new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
                _accR[b] = new Quaternion(_accR[b].X + q.X * weight, _accR[b].Y + q.Y * weight, _accR[b].Z + q.Z * weight, _accR[b].W + q.W * weight);
            }
            _accW += weight;
            _delta += RootDelta(c, prevTime, time) * weight;
            if (ExtractRootRotation) _yawAcc += RootYawDelta(c, prevTime, time) * weight;
        }

        /// <summary>Godot AnimationNodeBlend2 (filtered) / OneShot: after the base blend, lerp the listed bones (null = all) toward the weighted blend of <paramref name="clips"/> (each at its own time) by <paramref name="amount"/>. Applied in call order.</summary>
        readonly List<(SkinnedClip[] clips, float[] times, float[] weights, float amount, int[] bones)> _overlays = new List<(SkinnedClip[], float[], float[], float, int[])>();
        public void AddOverlay(SkinnedClip[] clips, float[] times, float[] weights, float amount, int[] bones) { if (amount > 1e-5f) _overlays.Add((clips, times, weights, amount, bones)); }

        void ApplyOverlay(SkinnedClip[] clips, float[] times, float[] weights, float amount, int[] bones)
        {
            int n = _t.Length; float wsum = 0; for (int i = 0; i < clips.Length; i++) wsum += weights[i]; if (wsum <= 1e-6f) return;
            var oT = new Vector3[n]; var oS = new Vector3[n]; var oR = new Quaternion[n]; bool first = true;
            for (int i = 0; i < clips.Length; i++)
            {
                if (weights[i] <= 1e-6f) continue;
                Sample(clips[i], times[i], out var T, out var R, out var S); float w = weights[i] / wsum;
                for (int b = 0; b < n; b++)
                {
                    oT[b] += T[b] * w; oS[b] += S[b] * w;
                    var q = R[b]; if (!first && Quaternion.Dot(q, oR[b]) < 0) q = new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
                    oR[b] = new Quaternion(oR[b].X + q.X * w, oR[b].Y + q.Y * w, oR[b].Z + q.Z * w, oR[b].W + q.W * w);
                }
                first = false;
            }
            for (int k = 0; k < (bones?.Length ?? n); k++)
            {
                int b = bones != null ? bones[k] : k; if (b < 0) continue;
                var q = Quaternion.Normalize(oR[b]); if (Quaternion.Dot(q, _r[b]) < 0) q = new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
                _t[b] = Vector3.Lerp(_t[b], oT[b], amount); _s[b] = Vector3.Lerp(_s[b], oS[b], amount); _r[b] = Quaternion.Normalize(Quaternion.Slerp(_r[b], q, amount));
            }
        }

        /// <summary>Root-bone rotation change between two un-wrapped times as a yaw about model +Y (delta = prev^-1 * now in root-local space, axis moved through the parent chain).</summary>
        public float RootYawDelta(SkinnedClip c, float prev, float now)
        {
            if (_root < 0 || c.Tracks[_root].R.Length < 2 || c.Duration <= 0) return 0f;
            float D = c.Duration;
            if (!c.Loop) return YawBetween(c, MathHelper.Clamp(prev, 0, D), MathHelper.Clamp(now, 0, D));
            float k0 = MathF.Floor(prev / D), k1 = MathF.Floor(now / D); float t0 = prev - k0 * D, t1 = now - k1 * D;
            if (k1 <= k0) return YawBetween(c, t0, t1);
            return YawBetween(c, t0, D) + (k1 - k0 - 1) * YawBetween(c, 0, D) + YawBetween(c, 0, t1);
        }

        Quaternion RootRotAt(SkinnedClip c, float t)
        {
            var tr = c.Tracks[_root].R; if (tr.Length == 1) return tr[0];
            float f = t * c.Fps; int i0 = Math.Min((int)f, c.FrameCount - 1), i1 = Math.Min(i0 + 1, c.FrameCount - 1);
            return Quaternion.Normalize(Quaternion.Slerp(tr[i0], tr[i1], f - i0));
        }

        float YawBetween(SkinnedClip c, float t0, float t1)
        {
            if (t1 == t0) return 0f;
            var d = Quaternion.Normalize(Quaternion.Concatenate(RootRotAt(c, t1), Quaternion.Inverse(RootRotAt(c, t0)))); // Hamilton prev^-1 * now (XNA Concatenate(a,b) = b*a)
            if (d.W < 0) d = new Quaternion(-d.X, -d.Y, -d.Z, -d.W);
            float ang = 2f * MathF.Acos(MathHelper.Clamp(d.W, -1f, 1f)); float s = MathF.Sqrt(Math.Max(1f - d.W * d.W, 0f)); if (s < 1e-6f) return 0f;
            Vector3 axis = Vector3.TransformNormal(new Vector3(d.X, d.Y, d.Z) / s, _rootParentToModel); axis.Normalize();
            return ang * axis.Y;
        }

        /// <summary>Godot AnimationNodeAdd: rest-relative additive clip on the given bones only (R: base * slerp(I, rest^-1 * clip, amount)).</summary>
        readonly List<(SkinnedClip c, float time, float amount, int[] bones)> _additive = new List<(SkinnedClip, float, float, int[])>();
        public void AddAdditive(SkinnedClip c, float time, float amount, int[] bones) { if (Math.Abs(amount) > 1e-5f) _additive.Add((c, time, amount, bones)); }

        public void Finish()
        {
            int n = _t.Length;
            for (int b = 0; b < n; b++)
            {
                if (_accW <= 0) { _t[b] = _m.BindT[b]; _r[b] = _m.BindR[b]; _s[b] = _m.BindS[b]; continue; }
                float inv = 1f / _accW; _t[b] = _accT[b] * inv; _s[b] = _accS[b] * inv; _r[b] = Quaternion.Normalize(_accR[b]);
            }
            foreach (var a in _additive)
            {
                Sample(a.c, a.time, out var T, out var R, out var S);
                foreach (int b in a.bones)
                {
                    if (b < 0) continue;
                    var d = Quaternion.Normalize(Quaternion.Inverse(_m.BindR[b]) * R[b]);
                    if (d.W < 0) d = new Quaternion(-d.X, -d.Y, -d.Z, -d.W);
                    _r[b] = Quaternion.Normalize(_r[b] * Quaternion.Slerp(Quaternion.Identity, d, Math.Abs(a.amount)));
                    _t[b] += (T[b] - _m.BindT[b]) * Math.Abs(a.amount);
                }
            }
            _additive.Clear();
            foreach (var o in _overlays) ApplyOverlay(o.clips, o.times, o.weights, o.amount, o.bones);
            _overlays.Clear();
            if (_root >= 0) _t[_root] = _m.BindT[_root]; // root-motion track is not applied to the visual pose
            if (_root >= 0 && ExtractRootRotation) { _r[_root] = _m.BindR[_root]; _s[_root] = _m.BindS[_root]; RootYaw = _yawAcc; }
            RootMotion = Vector3.TransformNormal(_delta, _rootParentToModel) * RootMotionScale;
            Compose();
        }

        // ---------------------------------------------------------------- sampling
        Vector3[] _sT, _sS; Quaternion[] _sR;
        void Sample(SkinnedClip c, float time, out Vector3[] T, out Quaternion[] R, out Vector3[] S)
        {
            float t = WrapTime(c, time);
            float f = t * c.Fps; int i0 = Math.Min((int)f, c.FrameCount - 1), i1 = Math.Min(i0 + 1, c.FrameCount - 1); float a = f - i0;
            var st = _sT; var sr = _sR; var ss = _sS;
            for (int b = 0; b < _t.Length; b++)
            {
                var tr = c.Tracks[b];
                st[b] = tr.T.Length == 0 ? _m.BindT[b] : tr.T.Length == 1 ? tr.T[0] : Vector3.Lerp(tr.T[i0], tr.T[i1], a);
                sr[b] = tr.R.Length == 0 ? _m.BindR[b] : tr.R.Length == 1 ? tr.R[0] : Quaternion.Normalize(Quaternion.Slerp(tr.R[i0], tr.R[i1], a));
                ss[b] = tr.S.Length == 0 ? _m.BindS[b] : tr.S.Length == 1 ? tr.S[0] : Vector3.Lerp(tr.S[i0], tr.S[i1], a);
            }
            T = st; R = sr; S = ss;
        }

        static float WrapTime(SkinnedClip c, float time)
        {
            if (c.Duration <= 0) return 0;
            return c.Loop ? time - MathF.Floor(time / c.Duration) * c.Duration : MathHelper.Clamp(time, 0, c.Duration);
        }

        Vector3 RootAt(SkinnedClip c, float t)
        {
            var tr = c.Tracks[_root].T;
            if (tr.Length == 0) return _m.BindT[_root]; if (tr.Length == 1) return tr[0];
            float f = t * c.Fps; int i0 = Math.Min((int)f, c.FrameCount - 1), i1 = Math.Min(i0 + 1, c.FrameCount - 1);
            return Vector3.Lerp(tr[i0], tr[i1], f - i0);
        }

        /// <summary>Root-bone local translation change between two un-wrapped times (loop wrap adds the end-to-start jump back in).</summary>
        public Vector3 RootDelta(SkinnedClip c, float prev, float now)
        {
            if (_root < 0 || c.Tracks[_root].T.Length < 2 || c.Duration <= 0) return Vector3.Zero;
            float D = c.Duration;
            if (!c.Loop) return RootAt(c, MathHelper.Clamp(now, 0, D)) - RootAt(c, MathHelper.Clamp(prev, 0, D));
            float k0 = MathF.Floor(prev / D), k1 = MathF.Floor(now / D);
            float t0 = prev - k0 * D, t1 = now - k1 * D;
            if (k1 <= k0) return RootAt(c, t1) - RootAt(c, t0);
            return (RootAt(c, D) - RootAt(c, t0)) + (k1 - k0 - 1) * (RootAt(c, D) - RootAt(c, 0)) + (RootAt(c, t1) - RootAt(c, 0));
        }

        /// <summary>Root-local direction/translation -> model space (parent chain, RootMotionScale). Test/diagnostic aid.</summary>
        public Vector3 RootToModel(Vector3 local) => Vector3.TransformNormal(local, _rootParentToModel) * RootMotionScale;

        void Compose()
        {
            int n = _t.Length;
            for (int b = 0; b < n; b++) // bones are stored parent-first
            {
                Matrix local = Matrix.CreateScale(_s[b]) * Matrix.CreateFromQuaternion(_r[b]) * Matrix.CreateTranslation(_t[b]);
                _model[b] = _m.Parent[b] >= 0 ? local * _model[_m.Parent[b]] : local;
                Skin[b] = _m.InverseBind[b] * _model[b];
            }
        }

        // ---------------------------------------------------------------- legacy single-clip playback (free camera, --pose)
        public void Play(string name, bool? loop = null)
        {
            if (!_m.Clips.TryGetValue(name, out var c)) throw new ArgumentException("no clip '" + name + "'");
            Clip = c; Time = 0; Loop = loop ?? c.Loop;
        }
        public void Update(float dt) { float p = Time; Time += dt; BeginFrame(); Add(Clip, Time, p, 1f); Finish(); }
        public void SetPose(string name, float time) { Play(name); Time = time; Evaluate(); }
        public void Evaluate() { BeginFrame(); Add(Clip, Time, Time, 1f); Finish(); }

        /// <summary>Model-space pose matrix of a bone (e.g. hand.R for the muzzle).</summary>
        public Matrix BoneModel(int bone) => _model[bone];
    }
}
