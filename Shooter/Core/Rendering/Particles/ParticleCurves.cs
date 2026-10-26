using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace Shooter.Rendering.Particles
{
    /// <summary>JSON helpers for the RAW Unity serialisation (numbers may be strings such as "1" or "Infinity").</summary>
    internal static class UJson
    {
        public static float F(JsonElement e, float fallback = 0f)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Number:
                    return (float)e.GetDouble();
                case JsonValueKind.String:
                    string s = e.GetString();
                    if (s == "Infinity") return float.PositiveInfinity;
                    if (s == "-Infinity") return float.NegativeInfinity;
                    if (s == "NaN") return fallback;
                    return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
                case JsonValueKind.True: return 1f;
                case JsonValueKind.False: return 0f;
            }
            return fallback;
        }

        public static float F(JsonElement parent, string name, float fallback = 0f)
        {
            return parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out JsonElement e) ? F(e, fallback) : fallback;
        }

        public static int I(JsonElement parent, string name, int fallback = 0)
        {
            return (int)Math.Round(F(parent, name, fallback));
        }

        public static bool B(JsonElement parent, string name, bool fallback = false)
        {
            return parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out JsonElement e) ? F(e) != 0f : fallback;
        }

        public static bool Has(JsonElement parent, string name, out JsonElement child)
        {
            child = default;
            return parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out child) && child.ValueKind != JsonValueKind.Null;
        }

        public static Vector3 V3(JsonElement parent, string name, Vector3 fallback)
        {
            if (!Has(parent, name, out JsonElement e))
                return fallback;
            return new Vector3(F(e, "x"), F(e, "y"), F(e, "z"));
        }
    }

    /// <summary>Unity AnimationCurve (cubic Hermite between keys, clamped outside).</summary>
    public sealed class AnimCurve
    {
        private float[] _time = new float[0];
        private float[] _value = new float[0];
        private float[] _inSlope = new float[0];
        private float[] _outSlope = new float[0];

        public int Count => _time.Length;

        public static AnimCurve Constant(float v)
        {
            var c = new AnimCurve();
            c._time = new[] { 0f };
            c._value = new[] { v };
            c._inSlope = new[] { 0f };
            c._outSlope = new[] { 0f };
            return c;
        }

        internal static AnimCurve FromJson(JsonElement e)
        {
            var c = new AnimCurve();
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty("m_Curve", out JsonElement keys) || keys.ValueKind != JsonValueKind.Array)
                return Constant(0f);
            int n = keys.GetArrayLength();
            c._time = new float[n];
            c._value = new float[n];
            c._inSlope = new float[n];
            c._outSlope = new float[n];
            int i = 0;
            foreach (JsonElement k in keys.EnumerateArray())
            {
                c._time[i] = UJson.F(k, "time");
                c._value[i] = UJson.F(k, "value");
                c._inSlope[i] = UJson.F(k, "inSlope");
                c._outSlope[i] = UJson.F(k, "outSlope");
                i++;
            }
            return c;
        }

        public float Evaluate(float t)
        {
            int n = _time.Length;
            if (n == 0) return 0f;
            if (n == 1 || t <= _time[0]) return _value[0];
            if (t >= _time[n - 1]) return _value[n - 1];
            int i = 0;
            while (i < n - 2 && t >= _time[i + 1]) i++;
            float t0 = _time[i], t1 = _time[i + 1];
            float dt = t1 - t0;
            if (dt <= 1e-8f) return _value[i + 1];
            float s = (t - t0) / dt;
            float m0 = _outSlope[i];
            float m1 = _inSlope[i + 1];
            if (float.IsInfinity(m0) || float.IsInfinity(m1) || float.IsNaN(m0) || float.IsNaN(m1))
                return _value[i]; // stepped tangent
            float s2 = s * s, s3 = s2 * s;
            return (2 * s3 - 3 * s2 + 1) * _value[i] + (s3 - 2 * s2 + s) * dt * m0 + (-2 * s3 + 3 * s2) * _value[i + 1] + (s3 - s2) * dt * m1;
        }
    }

    public enum CurveMode { Constant = 0, Curve = 1, TwoCurves = 2, TwoConstants = 3 }

    /// <summary>Unity MinMaxCurve (serialised: minMaxState, scalar, minScalar, maxCurve, minCurve).</summary>
    public sealed class MinMaxCurve
    {
        public CurveMode Mode = CurveMode.Constant;
        public float Scalar;
        public float MinScalar;
        public AnimCurve Max = AnimCurve.Constant(1f);
        public AnimCurve Min = AnimCurve.Constant(1f);

        public static MinMaxCurve Constant(float v)
        {
            return new MinMaxCurve { Mode = CurveMode.Constant, Scalar = v, MinScalar = v };
        }

        internal static MinMaxCurve FromJson(JsonElement parent, string name, float defaultValue)
        {
            if (!UJson.Has(parent, name, out JsonElement e) || e.ValueKind != JsonValueKind.Object)
                return Constant(defaultValue);
            return FromJson(e);
        }

        internal static MinMaxCurve FromJson(JsonElement e)
        {
            var c = new MinMaxCurve();
            c.Mode = (CurveMode)UJson.I(e, "minMaxState");
            c.Scalar = UJson.F(e, "scalar");
            c.MinScalar = UJson.F(e, "minScalar");
            if (UJson.Has(e, "maxCurve", out JsonElement mx)) c.Max = AnimCurve.FromJson(mx);
            if (UJson.Has(e, "minCurve", out JsonElement mn)) c.Min = AnimCurve.FromJson(mn);
            return c;
        }

        /// <param name="t">normalised time (system time or particle life) used by curve modes</param>
        /// <param name="rand">per-particle random value 0..1 used by the two-value modes</param>
        public float Evaluate(float t, float rand)
        {
            switch (Mode)
            {
                case CurveMode.Constant: return Scalar;
                case CurveMode.Curve: return Max.Evaluate(t) * Scalar;
                case CurveMode.TwoCurves: return MathHelper.Lerp(Min.Evaluate(t), Max.Evaluate(t), rand) * Scalar;
                default: return MathHelper.Lerp(MinScalar, Scalar, rand);
            }
        }

        public bool IsConstantZero => Mode == CurveMode.Constant && Scalar == 0f;
    }

    /// <summary>Unity Gradient (colour keys + alpha keys, blend or fixed).</summary>
    public sealed class Gradient
    {
        private Vector3[] _colors = { Vector3.One };
        private float[] _colorTimes = { 0f };
        private float[] _alphas = { 1f };
        private float[] _alphaTimes = { 0f };
        private bool _fixed;

        public static Gradient White = new Gradient();

        internal static Gradient FromJson(JsonElement g)
        {
            var r = new Gradient();
            if (g.ValueKind != JsonValueKind.Object) return r;
            int nc = Math.Max(1, UJson.I(g, "m_NumColorKeys", 1));
            int na = Math.Max(1, UJson.I(g, "m_NumAlphaKeys", 1));
            r._colors = new Vector3[nc];
            r._colorTimes = new float[nc];
            r._alphas = new float[na];
            r._alphaTimes = new float[na];
            for (int i = 0; i < nc; i++)
            {
                JsonElement k = g.GetProperty("key" + i);
                r._colors[i] = new Vector3(UJson.F(k, "r"), UJson.F(k, "g"), UJson.F(k, "b"));
                r._colorTimes[i] = UJson.F(g, "ctime" + i) / 65535f;
            }
            for (int i = 0; i < na; i++)
            {
                JsonElement k = g.GetProperty("key" + i);
                r._alphas[i] = UJson.F(k, "a");
                r._alphaTimes[i] = UJson.F(g, "atime" + i) / 65535f;
            }
            r._fixed = UJson.I(g, "m_Mode") == 1;
            return r;
        }

        private static int Segment(float[] times, float t, out float s)
        {
            int n = times.Length;
            s = 0f;
            if (n == 1 || t <= times[0]) return 0;
            if (t >= times[n - 1]) return n - 1;
            int i = 0;
            while (i < n - 2 && t >= times[i + 1]) i++;
            float d = times[i + 1] - times[i];
            s = d > 1e-8f ? (t - times[i]) / d : 1f;
            return i;
        }

        private static int FirstAtOrAfter(float[] times, float t)
        {
            for (int i = 0; i < times.Length; i++)
                if (times[i] >= t) return i;
            return times.Length - 1;
        }

        /// <summary>RGB in the gradient's own (gamma) space plus alpha.</summary>
        public Vector4 Evaluate(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            Vector3 c;
            float a;
            if (_fixed)
            {
                c = _colors[FirstAtOrAfter(_colorTimes, t)];
                a = _alphas[FirstAtOrAfter(_alphaTimes, t)];
            }
            else
            {
                int ci = Segment(_colorTimes, t, out float cs);
                c = ci < _colors.Length - 1 ? Vector3.Lerp(_colors[ci], _colors[ci + 1], cs) : _colors[ci];
                int ai = Segment(_alphaTimes, t, out float asx);
                a = ai < _alphas.Length - 1 ? MathHelper.Lerp(_alphas[ai], _alphas[ai + 1], asx) : _alphas[ai];
            }
            return new Vector4(c, a);
        }
    }

    public enum GradientMode { Color = 0, Gradient = 1, TwoColors = 2, TwoGradients = 3, RandomColor = 4 }

    /// <summary>Unity MinMaxGradient.</summary>
    public sealed class MinMaxGradient
    {
        public GradientMode Mode = GradientMode.Color;
        public Vector4 MinColor = Vector4.One;
        public Vector4 MaxColor = Vector4.One;
        public Gradient Min = Gradient.White;
        public Gradient Max = Gradient.White;

        public static MinMaxGradient White()
        {
            return new MinMaxGradient();
        }

        internal static MinMaxGradient FromJson(JsonElement parent, string name)
        {
            if (!UJson.Has(parent, name, out JsonElement e) || e.ValueKind != JsonValueKind.Object)
                return White();
            var g = new MinMaxGradient();
            g.Mode = (GradientMode)UJson.I(e, "minMaxState");
            if (UJson.Has(e, "minColor", out JsonElement mn)) g.MinColor = new Vector4(UJson.F(mn, "r"), UJson.F(mn, "g"), UJson.F(mn, "b"), UJson.F(mn, "a", 1f));
            if (UJson.Has(e, "maxColor", out JsonElement mx)) g.MaxColor = new Vector4(UJson.F(mx, "r"), UJson.F(mx, "g"), UJson.F(mx, "b"), UJson.F(mx, "a", 1f));
            if (UJson.Has(e, "minGradient", out JsonElement ming)) g.Min = Gradient.FromJson(ming);
            if (UJson.Has(e, "maxGradient", out JsonElement maxg)) g.Max = Gradient.FromJson(maxg);
            return g;
        }

        public Vector4 Evaluate(float t, float rand)
        {
            switch (Mode)
            {
                case GradientMode.Color: return MaxColor;
                case GradientMode.Gradient: return Max.Evaluate(t);
                case GradientMode.TwoColors: return Vector4.Lerp(MinColor, MaxColor, rand);
                case GradientMode.TwoGradients: return Vector4.Lerp(Min.Evaluate(t), Max.Evaluate(t), rand);
                default: return Max.Evaluate(rand);
            }
        }
    }
}
