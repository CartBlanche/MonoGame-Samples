using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Hand-coded port of the AnimationTree in player/player.tscn (sub-resource BlendTree "30"):
    ///   state Transition (xfade 0.2 s, reset = true): strafe &lt;- TimeScale 1.2 &lt;- BlendSpace2D strafe | walk &lt;- TimeScale 1.3 &lt;- BlendSpace2D walk | jump_up | jump_down
    ///   aim Add3 over neck.001 + upper_arm.R: AIM-Down (-1) / AIM-Up (+1), amount = player_input.get_aim_rotation() while strafing, else 0.
    /// Not ported: land OneShot (never requested by player.gd), eye_blend/eyes (cosmetic), running_gun/Idlecombat walk-space points (script only uses y = 0).
    /// Inputs are the parameters player.gd writes (animate(), :52-84). Output: <see cref="Pose"/> (skin matrices) and <see cref="Pose"/>.RootMotion.
    /// </summary>
    public sealed class PlayerAnimator
    {
        public enum State { Strafe, Walk, JumpUp, JumpDown }

        public const float XFadeTime = 0.2f;        // player.tscn:110-112 state Transition xfade_time
        public const float WalkTimeScale = 1.3f;    // walk_speed TimeScale (plan 03 sect.3)
        public const float StrafeTimeScale = 1.2f;  // strafe_speed TimeScale

        public readonly AnimationPlayer Pose;
        public SkinnedModel Model => _m;
        readonly SkinnedModel _m;
        readonly Dictionary<string, float> _time = new Dictionary<string, float>();

        // ---- parameters (set by the controller each tick before Update) ----
        /// <summary>Smoothed input motion exactly as player.gd `motion`: x = right, y = back-forward (forward = -1).</summary>
        public Vector2 MoveDir;
        public bool OnAir;
        public float VelocityY;
        /// <summary>Set on the take-off tick; forces jump_up for that frame (player.gd picks jump_up/down from velocity.y).</summary>
        public bool JumpPressed;
        public bool Aiming;
        /// <summary>Camera pitch in Godot sign (positive = looking down), radians. Only affects the pose while <see cref="Aiming"/>.</summary>
        public float AimPitch;

        /// <summary>Foot touch-down of the running/strafe clips (phases from <see cref="FootstepTable"/>), at most once per Update; the audio hook.</summary>
        public event Action Footstep;
        bool _stepFired;

        public State Current { get; private set; } = State.Walk;
        State _prev; bool _hasPrev; float _fade = XFadeTime;
        Vector2 _walkBlend, _strafeBlend; float _aimAmount;
        readonly int[] _aimBones;
        float _aimTime;

        public PlayerAnimator(SkinnedModel m)
        {
            _m = m; Pose = new AnimationPlayer(m);
            _aimBones = new[] { m.BoneIndex("neck.001"), m.BoneIndex("upper_arm.R") };
            Update(0f);
        }

        /// <summary>Name of the clip with the largest blend weight, for HUD/tests.</summary>
        public string DominantClip { get; private set; } = "";
        /// <summary>Last frame's root motion in model space (glb axes); copy of Pose.RootMotion.</summary>
        public Vector3 RootMotion => Pose.RootMotion;

        public static float AimRotation(float pitch) => pitch >= 0 ? -pitch / MathHelper.ToRadians(70f) : pitch / -MathHelper.ToRadians(89.9f); // player_input.gd get_aim_rotation (plan 03 sect.3)

        State Want() => OnAir ? (VelocityY > 0 || JumpPressed ? State.JumpUp : State.JumpDown) : Aiming ? State.Strafe : State.Walk;

        public void Update(float dt)
        {
            var want = Want(); JumpPressed = false; _stepFired = false;
            if (want != Current)
            {
                if (_hasPrev && want == _prev) { /* going back to the fading state: swap */ }
                _prev = Current; _hasPrev = true; Current = want; _fade = 0f;
                foreach (var (clip, _) in Leaves(want, Vector2.Zero, Vector2.Zero)) _time[clip] = 0f; // reset = true
            }
            _fade = Math.Min(_fade + dt, XFadeTime);
            float wCur = _hasPrev ? _fade / XFadeTime : 1f;

            // blend positions are only written while their own state is active (player.gd animate())
            if (Current == State.Walk) { _walkBlend = new Vector2(Math.Min(MoveDir.Length(), 1f), 0); _aimAmount = 0f; }
            if (Current == State.Strafe) { _strafeBlend = new Vector2(MoveDir.X, -MoveDir.Y); _aimAmount = AimRotation(AimPitch); }

            Pose.BeginFrame();
            float best = -1; DominantClip = "";
            if (_hasPrev && wCur < 1f) Mix(_prev, 1f - wCur, dt, ref best);
            Mix(Current, wCur, dt, ref best);
            if (wCur >= 1f) _hasPrev = false;
            // aim Add3: negative amount -> AIM-Down (input 0), positive -> AIM-Up (input 2)
            if (Math.Abs(_aimAmount) > 1e-4f)
            {
                _aimTime += dt;
                if (_m.Clips.TryGetValue(_aimAmount < 0 ? "AIM-Down" : "AIM-Up", out var ac)) Pose.AddAdditive(ac, _aimTime, _aimAmount, _aimBones);
            }
            Pose.Finish();
        }

        void Mix(State s, float stateWeight, float dt, ref float best)
        {
            float scale = s == State.Strafe ? StrafeTimeScale : s == State.Walk ? WalkTimeScale : 1f;
            foreach (var (name, lw) in Leaves(s, _walkBlend, _strafeBlend))
            {
                if (lw <= 1e-5f || !_m.Clips.TryGetValue(name, out var clip)) continue; // Godot only advances leaves with weight (BlendSpace sync = false)
                _time.TryGetValue(name, out float t0); float t1 = t0 + dt * scale; _time[name] = t1;
                float w = stateWeight * lw; Pose.Add(clip, t1, t0, w);
                if (Footstep != null && !_stepFired && w >= 0.3f && s != State.JumpUp && s != State.JumpDown)
                    foreach (float ph in FootstepTable.For(_m, name))
                        if (AudioMath.Crossed(t0, t1, ph * clip.Duration, clip.Duration)) { _stepFired = true; Footstep(); break; }
                if (w > best) { best = w; DominantClip = name; }
            }
        }

        /// <summary>Leaf clips of a state with their BlendSpace weights for the given blend positions.</summary>
        static IEnumerable<(string, float)> Leaves(State s, Vector2 walk, Vector2 strafe)
        {
            switch (s)
            {
                case State.JumpUp: yield return ("jump_2_upwards", 1f); break;
                case State.JumpDown: yield return ("jump_4_falling", 1f); break;
                case State.Walk: // walk BlendSpace2D, script only uses y = 0: 1-D Idle -> running_nogun along x in [0,1]
                    float x = MathHelper.Clamp(walk.X, 0f, 1f);
                    yield return ("Idle", 1f - x); yield return ("running_nogun", x); break;
                default:
                    var w = Strafe2D(strafe);
                    yield return ("Idlecombat", w[0]); yield return ("strafe_left", w[1]); yield return ("strafe_right", w[2]);
                    yield return ("strafe_back", w[3]); yield return ("strafe_front", w[4]); break;
            }
        }

        // strafe BlendSpace2D points: 0 = (0,0) Idlecombat, 1 = (1,0) left, 2 = (-1,0) right, 3 = (0,-1) back, 4 = (0,1) front
        static readonly Vector2[] StrafePts = { Vector2.Zero, new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, -1), new Vector2(0, 1) };
        // Delaunay of centre + diamond = 4 triangles (centre, a, b) in ring order left, front, right, back
        static readonly int[] Ring = { 1, 4, 2, 3 };

        /// <summary>BlendSpace2D weights (barycentric inside the triangle containing p; outside the diamond: projected on the closest hull edge, as Godot).</summary>
        public static float[] Strafe2D(Vector2 p)
        {
            var w = new float[5];
            for (int i = 0; i < 4; i++)
            {
                int a = Ring[i], b = Ring[(i + 1) % 4];
                if (Bary(p, StrafePts[0], StrafePts[a], StrafePts[b], out float u, out float v, out float c) && u >= -1e-6f && v >= -1e-6f && c >= -1e-6f)
                { w[0] = c; w[a] = u; w[b] = v; return w; }
            }
            float bestD = float.MaxValue; int ba = 1, bb = 4; float bt = 0;
            for (int i = 0; i < 4; i++) // hull edges
            {
                Vector2 A = StrafePts[Ring[i]], B = StrafePts[Ring[(i + 1) % 4]], ab = B - A;
                float t = MathHelper.Clamp(Vector2.Dot(p - A, ab) / ab.LengthSquared(), 0f, 1f);
                float d = (A + ab * t - p).LengthSquared();
                if (d < bestD) { bestD = d; ba = Ring[i]; bb = Ring[(i + 1) % 4]; bt = t; }
            }
            w[ba] = 1 - bt; w[bb] = bt; return w;
        }

        internal static bool Bary(Vector2 p, Vector2 c, Vector2 a, Vector2 b, out float wa, out float wb, out float wc)
        {
            Vector2 v0 = a - c, v1 = b - c, v2 = p - c;
            float den = v0.X * v1.Y - v1.X * v0.Y;
            if (Math.Abs(den) < 1e-9f) { wa = wb = wc = 0; return false; }
            wa = (v2.X * v1.Y - v1.X * v2.Y) / den; wb = (v0.X * v2.Y - v2.X * v0.Y) / den; wc = 1 - wa - wb; return true;
        }
    }
}
