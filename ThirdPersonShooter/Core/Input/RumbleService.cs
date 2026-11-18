using System;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Gamepad rumble driven by camera-shake trauma events. DELIBERATE ENHANCEMENT: neither the Godot demo nor the first port has force feedback.
    /// With the RUMBLE setting off (or no pad connected) the service never calls the sink, so behaviour is the like-for-like original.
    /// Trauma amount -> pulse: 0.35 (player shot) = soft low-motor tick; 0.75 (hit()) = medium; 13.0 (robot laser) = strong, both motors, decaying.
    /// Pulses decay linearly; a new pulse replaces the running one only if its peak is at least the running pulse's remaining strength.
    /// The sink is a delegate (low-frequency motor, high-frequency motor) so the logic is headless-testable.
    /// </summary>
    public sealed class RumbleService
    {
        public const float FireLow = 0.25f, FireHigh = 0f, FireSecs = 0.12f;
        public const float HitLow = 0.6f, HitHigh = 0.5f, HitSecs = 0.2f;
        public const float LaserLow = 0.9f, LaserHigh = 0.9f, LaserSecs = 0.35f;
        public const float HitTraumaMin = 0.5f, LaserTraumaMin = 5f;

        readonly Func<bool> _padConnected, _enabled; readonly Action<float, float> _sink; readonly Action<string> _log;
        float _low, _high, _dur, _t; bool _has, _active;
        float _outLow, _outHigh; // last values sent to the sink
        public bool Sending => _outLow > 0f || _outHigh > 0f;

        public RumbleService(Func<bool> padConnected, Func<bool> enabled, Action<float, float> sink, Action<string> log = null)
        { _padConnected = padConnected; _enabled = enabled; _sink = sink; _log = log; }

        float Strength => _has ? Math.Max(_low, _high) * (1f - _t / _dur) : 0f;

        /// <summary>Wire to CameraShake.TraumaAdded.</summary>
        public void OnTrauma(float amount)
        {
            if (!_active || !_enabled() || !_padConnected()) return;
            float lo, hi, secs;
            if (amount >= LaserTraumaMin) { lo = LaserLow; hi = LaserHigh; secs = LaserSecs; }
            else if (amount >= HitTraumaMin) { lo = HitLow; hi = HitHigh; secs = HitSecs; }
            else { lo = FireLow; hi = FireHigh; secs = FireSecs; }
            if (Math.Max(lo, hi) < Strength) return; // stronger running pulse wins
            _low = lo; _high = hi; _dur = secs; _t = 0f; _has = true;
        }

        /// <summary>Call every frame. active = playing, not paused/menu/loading, window focused.</summary>
        public void Update(float dt, bool active)
        {
            _active = active;
            if (!active || !_enabled()) { Zero(); return; }
            if (!_padConnected()) { Zero(); return; }
            float lo = 0f, hi = 0f;
            if (_has)
            {
                _t += dt;
                if (_t >= _dur) _has = false;
                else { float k = 1f - _t / _dur; lo = _low * k; hi = _high * k; }
            }
            Send(lo, hi);
        }

        /// <summary>Drop any pulse and make sure the motors are off (pause, menu, focus loss, exit, disconnect). No sink call if nothing was sent.</summary>
        public void Zero() { _has = false; Send(0f, 0f); }

        void Send(float lo, float hi)
        {
            if (Math.Abs(lo - _outLow) < 0.01f && Math.Abs(hi - _outHigh) < 0.01f && !(lo == 0f && hi == 0f && Sending)) return;
            _outLow = lo; _outHigh = hi; _sink(lo, hi);
            _log?.Invoke($"[rumble] SetVibration low={lo:F2} high={hi:F2}");
        }
    }
}
