using System;
using System.Collections.Generic;

namespace ThirdPersonShooter
{
    /// <summary>One loading step: <see cref="Tick"/> does a slice of work on the calling (main) thread and returns the fraction of this step done (1 = finished).</summary>
    public sealed class LoadStep
    {
        public readonly string Name; public readonly float Weight; public readonly Func<float> Tick;
        public LoadStep(string name, float weight, Func<float> tick) { Name = name; Weight = weight; Tick = tick; }
        /// <summary>A step that does all its work in one tick.</summary>
        public static LoadStep Once(string name, float weight, Action work) => new LoadStep(name, weight, () => { work(); return 1f; });
        /// <summary>A step that does one slice of work per tick, driven by an iterator yielding the fraction done (the last tick reports 1).</summary>
        public static LoadStep Incremental(string name, float weight, IEnumerable<float> slices)
        {
            var e = slices.GetEnumerator();
            return new LoadStep(name, weight, () => e.MoveNext() ? e.Current : 1f);
        }
    }

    /// <summary>The asset groups of the game, split into what the menu needs at boot and what Singleplayer loads behind the loading screen. Weights are relative costs in ms measured on a warm disk cache (see the `[load]` log lines).</summary>
    public static class LoadTable
    {
        /// <summary>Every asset group the pre-loading-screen LoadContent (plus StartPlay's collider build) loaded. Boot + Play must cover exactly this set.</summary>
        public static readonly string[] FullSet =
            { "fonts-ui", "logo", "player", "floor", "post", "audio-menu", "scene", "renderer", "colliders", "ground", "robot", "debris", "blob", "fx", "hud", "audio-level" };

        public static readonly (string Name, float Weight)[] Boot =
            { ("fonts-ui", 2f), ("logo", 1f), ("player", 6f), ("floor", 1f), ("post", 2f), ("audio-menu", 1f) };

        public static readonly (string Name, float Weight)[] Play =
            { ("scene", 175f), ("renderer", 5f), ("colliders", 58f), ("ground", 14f), ("robot", 27f), ("debris", 4f), ("blob", 3f), ("fx", 3f), ("hud", 1f), ("audio-level", 27f) };
    }

    /// <summary>Runs <see cref="LoadStep"/>s one tick at a time and reports a weighted, monotonic progress in [0,1].</summary>
    public sealed class LoadRunner
    {
        readonly List<LoadStep> _steps; int _i; float _doneW; readonly float _totalW; float _progress;
        public LoadRunner(IEnumerable<LoadStep> steps) { _steps = new List<LoadStep>(steps); foreach (var s in _steps) _totalW += Math.Max(0f, s.Weight); if (_steps.Count == 0) _progress = 1f; }
        public bool Done => _i >= _steps.Count;
        public float Progress => _progress;
        public string CurrentName => Done ? null : _steps[_i].Name;
        public int StepIndex => _i; public int StepCount => _steps.Count;

        /// <summary>Do one slice of work. Returns the name of a step that just finished, else null.</summary>
        public string Tick()
        {
            if (Done) return null;
            var s = _steps[_i]; float frac = Math.Clamp(s.Tick(), 0f, 1f); string finished = null;
            float w = Math.Max(0f, s.Weight);
            if (frac >= 1f) { _doneW += w; _i++; finished = s.Name; frac = 0f; w = 0f; }
            _progress = Done ? 1f : Math.Max(_progress, Math.Clamp(_totalW > 0f ? (_doneW + w * frac) / _totalW : (float)_i / _steps.Count, 0f, 1f));
            return finished;
        }
    }
}
