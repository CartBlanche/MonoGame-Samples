using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Hand-coded port of the AnimationTree in enemies/red_robot/red_robot.tscn (BlendTree "52", red_robot.tscn:10139-10206):
    ///   state Transition (idle / turn_left / turn_right / walk, xfade 0.5 s, reset = true) -> Blend2 "aiming" (filtered to the three cannon bones,
    ///   input 1 = BlendSpace2D "aim" of the 5 static Cannon-* poses) -> OneShot hit1 -> hit2 -> hit3 (fadeout 0.5 s, clips Damage_take1/take2/heavy).
    /// Clip names: the glb "-cycle" suffix is stripped by the content processor (Idle-cycle = "Idle"), as Godot's importer does.
    /// Root motion is the "MASTER" bone (tscn:10784 root_motion_track): translation (model space) and yaw, both read from <see cref="Pose"/>.
    /// </summary>
    public sealed class RobotAnimator
    {
        public enum Loco { Idle = 0, TurnLeft = 1, TurnRight = 2, Walk = 3 }

        public const float XFadeTime = 0.5f;          // tscn:10159 AnimationNodeTransition xfade_time
        public const float HitFadeout = 0.5f;         // tscn:10145-10153 OneShot fadeout_time
        public const float AimPrepareTime = 0.5f;     // red_robot.gd:17 AIM_PREPARE_TIME (blend_amount = aim_preparing / 0.5)

        public readonly AnimationPlayer Pose;
        public readonly SkinnedModel Model;
        /// <summary>parameters/state/transition_request.</summary>
        public Loco Request = Loco.Idle;
        /// <summary>parameters/aiming/blend_amount (0..1).</summary>
        public float AimAmount = 1f;
        /// <summary>parameters/aim/blend_position, initial value from the tscn (red_robot.tscn:10786).</summary>
        public Vector2 AimPos = new Vector2(0.00479996f, 0.126638f);
        /// <summary>True while the shoot clip has AnimationTree:active = false (tscn track 1: 2.6 s - 2.9 s): the pose freezes and no root motion is produced.</summary>
        public bool Frozen;
        /// <summary>Walk-cycle audio track keys (red_robot.tscn track 67) crossed this tick; argument = the Walk leaf's blend weight (use_blend).</summary>
        public event Action<float> WalkKey;
        public Loco Current { get; private set; } = Loco.Idle;
        public Vector3 RootMotion { get; private set; }
        public float RootYaw { get; private set; }
        public string DominantClip { get; private set; } = "Idle";

        static readonly string[] LocoClips = { "Idle", "TurnLeft", "TurnRight", "Walk" };
        static readonly string[] AimClips = { "Cannon-N", "Cannon-L", "Cannon-R", "Cannon-U", "Cannon-D" };
        static readonly string[] HitClips = { "Damage_take1", "Damage_take2", "Damage_heavy" };
        // BlendSpace2D points (tscn:10124-10138): 0 N (0,0), 1 L (-1,0), 2 R (1,0), 3 U (0,1), 4 D (0,-0.9). Delaunay of centre + diamond = 4 triangles.
        static readonly Vector2[] AimPts = { Vector2.Zero, new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -0.9f) };
        static readonly int[] Ring = { 1, 3, 2, 4 };

        readonly float[] _w = { 1, 0, 0, 0 }; readonly float[] _t = new float[4];
        readonly int[] _aimBones;
        readonly float[] _hitT = new float[3]; readonly bool[] _hitOn = new bool[3];
        readonly SkinnedClip[] _loco = new SkinnedClip[4], _aim = new SkinnedClip[5], _hit = new SkinnedClip[3];

        public RobotAnimator(SkinnedModel m)
        {
            Model = m; Pose = new AnimationPlayer(m, "MASTER") { ExtractRootRotation = true };
            for (int i = 0; i < 4; i++) _loco[i] = m.Clips[LocoClips[i]];
            for (int i = 0; i < 5; i++) _aim[i] = m.Clips[AimClips[i]];
            for (int i = 0; i < 3; i++) _hit[i] = m.Clips[HitClips[i]];
            _aimBones = new[] { m.BoneIndex("Cannon-TrackTurret"), m.BoneIndex("CannonAnimRecoil"), m.BoneIndex("Cannon_TrackElevation") };
            Update(0f);
        }

        /// <summary>parameters/hitN/request = 1 (n = 1..3).</summary>
        public void RequestHit(int n) { _hitOn[n - 1] = true; _hitT[n - 1] = 0f; }

        public void Update(float dt)
        {
            if (Frozen) { RootMotion = Vector3.Zero; RootYaw = 0f; return; }
            // Transition: a new request resets that clip (reset = true) and cross-fades over 0.5 s
            if (Request != Current) { Current = Request; _t[(int)Current] = 0f; }
            float step = dt / XFadeTime, sum = 0;
            for (int i = 0; i < 4; i++) { float target = i == (int)Current ? 1f : 0f; _w[i] = _w[i] < target ? Math.Min(_w[i] + step, target) : Math.Max(_w[i] - step, target); sum += _w[i]; }
            Pose.BeginFrame(); float best = -1;
            for (int i = 0; i < 4; i++)
            {
                if (_w[i] <= 1e-5f) continue;
                float t0 = _t[i]; _t[i] += dt; float w = _w[i] / sum;
                Pose.Add(_loco[i], _t[i], t0, w);
                if (i == (int)Loco.Walk && WalkKey != null)
                    foreach (float k in AudioTable.RobotWalkKeys) if (AudioMath.Crossed(t0, _t[i], k, AudioTable.RobotWalkCycle)) WalkKey(w);
                if (w > best) { best = w; DominantClip = LocoClips[i]; }
            }
            // aiming Blend2: bones of the cannon only, amount = blend_amount, input 1 = the 2D blend of 5 poses
            if (AimAmount > 1e-4f) Pose.AddOverlay(_aim, new float[5], Aim2D(AimPos), Math.Min(AimAmount, 1f), _aimBones);
            // OneShot chain: weight 1 until the last 0.5 s of the clip, then linear fadeout
            for (int i = 0; i < 3; i++)
            {
                if (!_hitOn[i]) continue;
                _hitT[i] += dt; float len = _hit[i].Duration;
                if (_hitT[i] >= len) { _hitOn[i] = false; continue; }
                float remain = len - _hitT[i], w = remain < HitFadeout ? remain / HitFadeout : 1f;
                Pose.AddOverlay(new[] { _hit[i] }, new[] { _hitT[i] }, new[] { 1f }, w, null);
            }
            Pose.Finish();
            RootMotion = Pose.RootMotion; RootYaw = Pose.RootYaw;
        }

        /// <summary>BlendSpace2D weights for <see cref="AimPts"/> (barycentric in the triangle holding p; outside the hull: closest hull edge, as Godot).</summary>
        public static float[] Aim2D(Vector2 p)
        {
            var w = new float[5];
            for (int i = 0; i < 4; i++)
            {
                int a = Ring[i], b = Ring[(i + 1) % 4];
                if (PlayerAnimator.Bary(p, AimPts[0], AimPts[a], AimPts[b], out float u, out float v, out float c) && u >= -1e-6f && v >= -1e-6f && c >= -1e-6f) { w[0] = c; w[a] = u; w[b] = v; return w; }
            }
            float bestD = float.MaxValue; int ba = 1, bb = 3; float bt = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector2 A = AimPts[Ring[i]], B = AimPts[Ring[(i + 1) % 4]], ab = B - A;
                float t = MathHelper.Clamp(Vector2.Dot(p - A, ab) / ab.LengthSquared(), 0f, 1f), d = (A + ab * t - p).LengthSquared();
                if (d < bestD) { bestD = d; ba = Ring[i]; bb = Ring[(i + 1) % 4]; bt = t; }
            }
            w[ba] = 1 - bt; w[bb] = bt; return w;
        }
    }
}
