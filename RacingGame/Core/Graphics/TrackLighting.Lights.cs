using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace RacingGame.Graphics
{
    /// <summary>
    /// Light list (WS2): the per-pixel point and spot lights uploaded to the shaders
    /// (LPos0..3, LCol0..3, LDir0..3 in Content/Shaders/TrackLighting.fxh) plus a
    /// debug test light. Headlights, lamps and tunnel lights (WS3, WS4) fill the
    /// same list through <see cref="SetLights"/>.
    /// </summary>
    public sealed partial class TrackLighting
    {
        /// <summary>One point or spot light (world space, Z is up).</summary>
        public struct Light
        {
            public Vector3 Position;
            /// <summary>Spot direction (ignored for points).</summary>
            public Vector3 Direction;
            /// <summary>Colour times intensity.</summary>
            public Vector3 Color;
            public float Range;
            public bool Spot;
            /// <summary>Cone half angles in radians (spots only).</summary>
            public float InnerAngle, OuterAngle;
        }

        // Packed shader values, set by SetLights.
        internal readonly Vector4[] LPos = new Vector4[MaxLights];
        internal readonly Vector4[] LCol = new Vector4[MaxLights];
        internal readonly Vector4[] LDir = new Vector4[MaxLights];

        int lastLightUpdateFrame = -1;

        // Light producers (contract between the workstreams, see Lighting-Plan.md):
        // each frame the list is rebuilt as headlights first (at most 2), then lamp and
        // tunnel lights, truncated to MaxLights. WS3 implements CollectHeadlights in
        // TrackLighting.Headlights.cs, WS4 implements CollectLampLights in
        // TrackLighting.Lamps.cs. Unimplemented partial methods compile away.
        partial void CollectHeadlights(List<Light> list);
        partial void CollectLampLights(List<Light> list);
        readonly List<Light> collectedLights = new List<Light>(MaxLights * 2);
        readonly Light[] collectedArray = new Light[MaxLights];


        /// <summary>
        /// Replace the active light list (up to MaxLights are used) and switch to
        /// the per-pixel path (LightingMode 1) when it is not empty.
        /// </summary>
        public void SetLights(params Light[] lights)
        {
            SetLights(lights, lights == null ? 0 : lights.Length);
        }

        /// <summary>Same as <see cref="SetLights(Light[])"/> but uses only the first <paramref name="count"/> entries (no allocation).</summary>
        public void SetLights(Light[] lights, int count)
        {
            int n = lights == null ? 0 : Math.Min(Math.Min(count, lights.Length), MaxLights);
            for (int i = 0; i < n; i++)
            {
                Light l = lights[i];
                LPos[i] = new Vector4(l.Position, l.Range);
                if (l.Spot)
                {
                    Vector3 d = Vector3.Normalize(l.Direction);
                    LCol[i] = new Vector4(l.Color, (float)Math.Cos(l.OuterAngle));
                    LDir[i] = new Vector4(d, (float)Math.Cos(l.InnerAngle));
                }
                else
                {
                    LCol[i] = new Vector4(l.Color, -3.0f);
                    LDir[i] = new Vector4(0, 0, -1, -2.0f);
                }
            }
            LightCount = n;
            LightingMode = n > 0 ? 1.0f : 0.0f;
        }

        /// <summary>
        /// Called once per frame from the first effect that uploads lighting
        /// parameters: rebuilds the light list from the producers.
        /// </summary>
        internal void UpdateLightsOncePerFrame()
        {
            if (lastLightUpdateFrame == BaseGame.TotalFrames)
                return;
            lastLightUpdateFrame = BaseGame.TotalFrames;
            // Rebuild the list from the producers (no allocation after start-up)
            collectedLights.Clear();
            CollectHeadlights(collectedLights);
            CollectLampLights(collectedLights);
            int collected = Math.Min(collectedLights.Count, MaxLights);
            for (int i = 0; i < collected; i++)
                collectedArray[i] = collectedLights[i];
            SetLights(collectedArray, collected);
        }
    }
}
