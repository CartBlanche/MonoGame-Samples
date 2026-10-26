using System;
using System.Collections.Generic;

namespace Shooter.Animation
{
    /// <summary>
    /// Runtime evaluator for an exported Unity Animator controller (parameters, layers, states, transitions).
    /// It drives an AnimationPlayer: every layer maps to an AnimationLayer, state changes become cross-fades.
    ///
    /// Supported: bool/int/float/trigger parameters, any-state transitions, exit time (looping aware), fixed and
    /// normalized transition duration, transition offset, conditions (if/ifNot/greater/less/equals/notEqual),
    /// exit transitions, state speed (+ speed parameter), cycle offset, layer weight/mask/additive blending,
    /// states without motion (layer fades out).
    /// Not supported: blend trees (the shipped controllers only have empty ones in unreachable states), transition
    /// interruption (all shipped transitions use interruptionSource = none), synced layers, mirror, IK.
    /// </summary>
    public sealed class AnimatorController
    {
        private sealed class LayerRuntime
        {
            public AnimatorLayerData Data;
            public AnimationLayer PlayerLayer;
            public Dictionary<string, AnimatorStateData> States = new Dictionary<string, AnimatorStateData>();
            public AnimatorStateData Current;
            public float PreviousNormalizedTime;
            public float NullStateTime;
            public bool Started;
        }

        private readonly AnimatorControllerData _data;
        private readonly AnimationPlayer _player;
        private readonly Func<string, AnimationClip> _clipLoader;
        private readonly Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();
        private readonly Dictionary<string, AnimatorParameterData> _paramDefs = new Dictionary<string, AnimatorParameterData>();
        private readonly Dictionary<string, double> _values = new Dictionary<string, double>();
        private readonly List<LayerRuntime> _layers = new List<LayerRuntime>();

        /// <param name="clipLoader">Loads a clip by content asset name, e.g. content.Load&lt;AnimationClip&gt;.</param>
        public AnimatorController(AnimatorControllerData data, AnimationPlayer player, Func<string, AnimationClip> clipLoader)
        {
            _data = data;
            _player = player;
            _clipLoader = clipLoader;

            foreach (AnimatorParameterData p in data.Parameters)
            {
                _paramDefs[p.Name] = p;
                _values[p.Name] = p.Type == "bool" ? (p.DefaultBool ? 1 : 0)
                    : p.Type == "int" ? p.DefaultInt
                    : p.Type == "float" ? p.DefaultFloat : 0;
            }

            for (int i = 0; i < data.Layers.Count; i++)
            {
                AnimatorLayerData ld = data.Layers[i];
                LayerRuntime lr = new LayerRuntime { Data = ld };
                foreach (AnimatorStateData s in ld.States)
                {
                    lr.States[s.Id] = s;
                }

                if (i == 0)
                {
                    lr.PlayerLayer = player.BaseLayer;
                }
                else
                {
                    lr.PlayerLayer = player.AddLayer(
                        (float)ld.DefaultWeight,
                        ld.BlendMode == "additive",
                        ld.AvatarMask != null ? ld.AvatarMask.IncludedTransforms : null);
                }

                _layers.Add(lr);
            }

            foreach (AnimatorClipData c in data.Clips)
            {
                GetClip(c.Asset);
            }
        }

        public AnimationPlayer Player
        {
            get { return _player; }
        }

        public int LayerCount
        {
            get { return _layers.Count; }
        }

        public void SetBool(string name, bool value)
        {
            _values[name] = value ? 1 : 0;
        }

        public bool GetBool(string name)
        {
            return _values[name] != 0;
        }

        public void SetFloat(string name, float value)
        {
            _values[name] = value;
        }

        public float GetFloat(string name)
        {
            return (float)_values[name];
        }

        public void SetInteger(string name, int value)
        {
            _values[name] = value;
        }

        public void SetTrigger(string name)
        {
            _values[name] = 1;
        }

        public void ResetTrigger(string name)
        {
            _values[name] = 0;
        }

        /// <summary>Id of the layer's current (or incoming) state.</summary>
        public string GetCurrentStateId(int layer)
        {
            LayerRuntime lr = _layers[layer];
            return lr.Current != null ? lr.Current.Id : null;
        }

        public bool IsInTransition(int layer)
        {
            return _layers[layer].PlayerLayer.IsInTransition;
        }

        public void SetLayerWeight(int layer, float weight)
        {
            _layers[layer].PlayerLayer.Weight = weight;
        }

        /// <summary>Evaluates transitions, then advances the player by dt.</summary>
        public void Update(float dt)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                UpdateLayer(_layers[i], dt);
            }

            _player.Update(dt);
        }

        private AnimationClip GetClip(string asset)
        {
            AnimationClip c;
            if (!_clips.TryGetValue(asset, out c))
            {
                c = _clipLoader(asset);
                _clips[asset] = c;
            }

            return c;
        }

        private AnimationClip ClipOf(AnimatorStateData s)
        {
            if (s == null || s.Motion == null || s.Motion.Type != "clip")
            {
                return null;
            }

            return GetClip(s.Motion.Asset);
        }

        private float SpeedOf(AnimatorStateData s)
        {
            float speed = (float)s.Speed;
            if (!string.IsNullOrEmpty(s.SpeedParameter) && _values.ContainsKey(s.SpeedParameter))
            {
                speed *= (float)_values[s.SpeedParameter];
            }

            return speed;
        }

        private void Enter(LayerRuntime lr, AnimatorStateData state, AnimatorTransitionData via)
        {
            AnimationClip clip = ClipOf(state);
            float offset = (float)state.CycleOffset;
            float speed = SpeedOf(state);
            if (via == null)
            {
                if (clip != null)
                {
                    lr.PlayerLayer.Play(clip, offset, speed);
                }
                else
                {
                    lr.PlayerLayer.Stop();
                }
            }
            else
            {
                float start = (float)via.Offset + offset;
                lr.PlayerLayer.CrossFade(clip, (float)via.Duration, start, speed, via.HasFixedDuration);
            }

            lr.Current = state;
            lr.PreviousNormalizedTime = 0f;
            lr.NullStateTime = 0f;
            if (clip != null && lr.PlayerLayer.Current != null)
            {
                lr.PreviousNormalizedTime = lr.PlayerLayer.Current.NormalizedTime;
            }
        }

        private float NormalizedTimeOf(LayerRuntime lr)
        {
            if (lr.PlayerLayer.Current != null)
            {
                return lr.PlayerLayer.Current.NormalizedTime;
            }

            return lr.NullStateTime;
        }

        private void UpdateLayer(LayerRuntime lr, float dt)
        {
            if (!lr.Started)
            {
                lr.Started = true;
                if (lr.Data.DefaultState != null)
                {
                    Enter(lr, lr.States[lr.Data.DefaultState], null);
                }

                return;
            }

            if (lr.Current == null)
            {
                return;
            }

            // Null-motion states: pretend a 1 second length so exit-time transitions still work.
            if (ClipOf(lr.Current) == null)
            {
                lr.NullStateTime += dt;
            }

            if (lr.PlayerLayer.IsInTransition)
            {
                lr.PreviousNormalizedTime = NormalizedTimeOf(lr);
                return;
            }

            float nt = NormalizedTimeOf(lr);
            bool loop = lr.PlayerLayer.Current != null && lr.PlayerLayer.Current.Loop;
            AnimatorTransitionData fired = FindTransition(lr, lr.Data.AnyStateTransitions, nt, loop, true)
                ?? FindTransition(lr, lr.Current.Transitions, nt, loop, false);
            if (fired != null)
            {
                string dest = fired.IsExit || fired.Destination == null ? lr.Data.DefaultState : fired.Destination;
                ConsumeTriggers(fired);
                if (dest != null)
                {
                    Enter(lr, lr.States[dest], fired);
                    return;
                }
            }

            lr.PreviousNormalizedTime = nt;
        }

        private AnimatorTransitionData FindTransition(
            LayerRuntime lr, List<AnimatorTransitionData> list, float nt, bool loop, bool anyState)
        {
            for (int i = 0; i < list.Count; i++)
            {
                AnimatorTransitionData t = list[i];
                if (t.Mute)
                {
                    continue;
                }

                if (anyState && !t.CanTransitionToSelf && t.Destination == lr.Current.Id)
                {
                    continue;
                }

                if (!t.HasExitTime && t.Conditions.Count == 0)
                {
                    continue;
                }

                if (t.HasExitTime && !ExitTimeReached(t, nt, lr.PreviousNormalizedTime, loop))
                {
                    continue;
                }

                if (!ConditionsMet(t))
                {
                    continue;
                }

                return t;
            }

            return null;
        }

        private static bool ExitTimeReached(AnimatorTransitionData t, float nt, float prevNt, bool loop)
        {
            float exit = (float)t.ExitTime;
            if (!loop || exit >= 1f)
            {
                return nt >= exit;
            }

            // Looping state: exit time is reached once per loop.
            float frac = nt - (float)Math.Floor(nt);
            return frac >= exit;
        }

        private bool ConditionsMet(AnimatorTransitionData t)
        {
            for (int i = 0; i < t.Conditions.Count; i++)
            {
                AnimatorConditionData c = t.Conditions[i];
                double v;
                if (!_values.TryGetValue(c.Parameter, out v))
                {
                    return false;
                }

                bool ok;
                switch (c.Mode)
                {
                    case "if":
                        ok = v != 0;
                        break;
                    case "ifNot":
                        ok = v == 0;
                        break;
                    case "greater":
                        ok = v > c.Threshold;
                        break;
                    case "less":
                        ok = v < c.Threshold;
                        break;
                    case "equals":
                        ok = Math.Abs(v - c.Threshold) < 1e-6;
                        break;
                    case "notEqual":
                        ok = Math.Abs(v - c.Threshold) >= 1e-6;
                        break;
                    default:
                        ok = false;
                        break;
                }

                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }

        private void ConsumeTriggers(AnimatorTransitionData t)
        {
            for (int i = 0; i < t.Conditions.Count; i++)
            {
                AnimatorParameterData def;
                if (_paramDefs.TryGetValue(t.Conditions[i].Parameter, out def) && def.Type == "trigger")
                {
                    _values[def.Name] = 0;
                }
            }
        }
    }
}
