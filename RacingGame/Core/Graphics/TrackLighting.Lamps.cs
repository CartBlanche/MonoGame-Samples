using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RacingGame.GameLogic;
using RacingGame.Properties;

namespace RacingGame.Graphics
{
    /// <summary>
    /// WS4: tunnel blend, tunnel lamps and street lamps. The track registers lamp
    /// positions while it is built (Track.GenerateVerticesAndObjects), each frame
    /// <see cref="CollectLampLights"/> picks the nearest ones into the free light
    /// slots (headlights come first), with hysteresis and an eased intensity so
    /// nothing pops. Outside tunnels and at day nothing is added, so Day play
    /// is unchanged.
    /// </summary>
    public sealed partial class TrackLighting
    {
        sealed class Lamp
        {
            public Vector3 Pos;
            public bool Tunnel;
            /// <summary>Eased 0..1 (selected = rising).</summary>
            public float Ease;
            public bool Wanted, Active;
        }

        const float TunnelBlendInSeconds = 0.4f, TunnelBlendOutSeconds = 0.6f;
        const float LampEaseSeconds = 0.35f;
        const float StreetLampRange = 38.0f, TunnelLampRange = 34.0f;
        // Hysteresis: an incumbent lamp counts as 20 percent closer.
        const float IncumbentDistanceScale = 0.64f;
        static readonly Vector3 StreetLampColor = new Vector3(1.0f, 0.78f, 0.48f) * 1.4f;
        static readonly Vector3 TunnelLampColor = new Vector3(1.0f, 0.82f, 0.55f) * 1.7f;

        readonly List<Lamp> lamps = new List<Lamp>();
        readonly List<Vector3> lampPositions = new List<Vector3>();
        readonly List<Lamp> activeLamps = new List<Lamp>(8);
        readonly List<Lamp> wantedLamps = new List<Lamp>(8);
        readonly List<float> wantedScores = new List<float>(8);
        readonly List<Lamp> outputLamps = new List<Lamp>(8);
        readonly List<float> outputIntensity = new List<float>(8);
        float frameDt = 1.0f / 60.0f;
        bool tunnelApplied;

        /// <summary>World positions of all registered lamps (street and tunnel), read only (WS5 glow sprites).</summary>
        public IReadOnlyList<Vector3> LampPositions { get { return lampPositions; } }
        public bool LampIsTunnel(int index) { return lamps[index].Tunnel; }
        /// <summary>Current light intensity of a lamp, 0..1 (0 = off, also by day).</summary>
        public float LampIntensity(int index) { return LampLevel(lamps[index]); }

        const float LampGlowReach = 240.0f, LampGlowFade = 140.0f;
        const float StreetGlowSize = 1.7f, TunnelGlowSize = 0.5f;
        // Street lamp glows grow only slowly with distance (a big blob far away looks like it
        // floats in the air); the lamp head is about a unit wide.
        const float StreetGlowGrowth = 1.0f / 80.0f;

        /// <summary>
        /// Queue glow sprites (shared LightSprites) for the lamps near the camera: street lamps glow
        /// with the night, tunnel lamps with the tunnel blend, far ones fade out. Independent of the
        /// 2 real light slots, so a whole row of lamps shows down the road. Depth tested, so hills
        /// and buildings hide them.
        /// </summary>
        public void AddLampGlows()
        {
            if (Quality == LightingQuality.Off)
                return;
            if (lamps.Count == 0)
                return;
            float street = StreetOn, tunnel = TunnelBlend;
            if (street <= 0.01f && tunnel <= 0.01f)
                return;
            Vector3 cam = BaseGame.InverseViewMatrix.Translation;
            for (int i = 0; i < lamps.Count; i++)
            {
                Lamp l = lamps[i];
                float on = l.Tunnel ? tunnel : street;
                if (on <= 0.01f)
                    continue;
                float d = Vector3.Distance(l.Pos, cam);
                if (d > LampGlowReach)
                    continue;
                float fade = 1.0f - MathHelper.SmoothStep(0.0f, 1.0f,
                    MathHelper.Clamp((d - LampGlowFade) / (LampGlowReach - LampGlowFade), 0.0f, 1.0f));
                Vector3 c = l.Tunnel ? new Vector3(1.0f, 0.82f, 0.55f) : new Vector3(1.0f, 0.78f, 0.48f);
                Sprites.Add(l.Pos, c * (0.85f * on * fade), l.Tunnel ? TunnelGlowSize : StreetGlowSize,
                    l.Tunnel ? 1.0f / 15.0f : StreetGlowGrowth);
            }
        }

        /// <summary>Forget all lamps (a track is being built).</summary>
        public void ClearLamps()
        {
            lamps.Clear();
            lampPositions.Clear();
            activeLamps.Clear();
            wantedLamps.Clear();
            wantedScores.Clear();
        }

        public void AddTunnelLamp(Vector3 pos) { AddLamp(pos, true); }

        /// <summary>
        /// Street lamp from its final object matrix (Landscape.AddObjectToRender,
        /// the 1.2 scale is already in it). The light sits under the end of the arm
        /// of Laterne.X: after the model's X-to-Z rotation the arm tip is at about
        /// x 2.5, z 8.7 (measured from the vertices of the model).
        /// </summary>
        public void AddStreetLamp(Matrix objectMatrix)
        {
            Vector3 head = new Vector3(2.4f, 0.0f, 8.0f);
            AddLamp(Vector3.Transform(head, objectMatrix), false);
        }

        void AddLamp(Vector3 pos, bool tunnel)
        {
            lamps.Add(new Lamp { Pos = pos, Tunnel = tunnel });
            lampPositions.Add(pos);
        }

        float StreetOn
        {
            get { return MathHelper.SmoothStep(0.0f, 1.0f,
                MathHelper.Clamp((NightFactor - 0.3f) / 0.4f, 0.0f, 1.0f)); }
        }

        float LampLevel(Lamp l)
        {
            float ease = l.Ease * l.Ease * (3.0f - 2.0f * l.Ease);
            return ease * (l.Tunnel ? TunnelBlend : StreetOn);
        }

        // Tunnel blend and preset ------------------------------------------

        /// <summary>
        /// Every frame from UpdateForGame (render time): eases TunnelBlend from the
        /// car's segment (segment ranges, so a lap wrap is just segment 0 again).
        /// </summary>
        void UpdateTunnel(float elapsedSeconds)
        {
            frameDt = Math.Min(Math.Max(elapsedSeconds, 0.0f), 0.1f);
            bool inTunnel = false;
            if (RacingGameManager.Landscape != null)
                inTunnel = RacingGameManager.Landscape.IsTunnel(
                    (int)RacingGameManager.Player.TrackProgress);
            if (inTunnel)
                TunnelBlend = Math.Min(1.0f, TunnelBlend + frameDt / TunnelBlendInSeconds);
            else
                TunnelBlend = Math.Max(0.0f, TunnelBlend - frameDt / TunnelBlendOutSeconds);
        }

        /// <summary>
        /// Apply the tunnel multipliers on Current (after the time of day evaluated
        /// it, or on Day when time of day is not active): sun diffuse and the sun
        /// shadow fade out, ambient drops to 20 percent. Exactly nothing at blend 0.
        /// </summary>
        void ApplyTunnelToPreset()
        {
            if (TunnelBlend > 0.0f)
            {
                if (Active == false)
                    Current = Preset.Day;
                float b = TunnelBlend;
                Current.AmbientTint *= 1.0f - 0.8f * b;
                Current.DiffuseTint *= 1.0f - b;
                tunnelApplied = true;
                ApplyShadowColor();
            }
            else if (tunnelApplied)
            {
                // Back to exactly the outside values.
                tunnelApplied = false;
                if (Active == false)
                    Current = Preset.Day;
                ApplyShadowColor();
            }
        }

        /// <summary>Leaving the race: no tunnel state survives.</summary>
        void ResetTunnel()
        {
            TunnelBlend = 0.0f;
            if (tunnelApplied)
            {
                tunnelApplied = false;
                Current = Preset.Day;
                ApplyShadowColor();
            }
        }

        // Light producer -----------------------------------------------------

        partial void CollectLampLights(List<Light> list)
        {
            // Only High lets lamps light the road (the glow sprites stay on Low)
            if (Quality != LightingQuality.High)
                return;
            if (lamps.Count == 0)
                return;
            float streetOn = StreetOn, tunnelOn = TunnelBlend;
            if (activeLamps.Count == 0 && streetOn <= 0.001f && tunnelOn <= 0.001f)
                return;

            int free = Math.Max(0, MaxLights - list.Count);
            Vector3 car = RacingGameManager.Player.CarPosition;

            // 1. Pick the nearest `free` enabled lamps (incumbents count as closer).
            wantedLamps.Clear();
            wantedScores.Clear();
            for (int i = 0; i < lamps.Count && free > 0; i++)
            {
                Lamp l = lamps[i];
                if ((l.Tunnel ? tunnelOn : streetOn) <= 0.001f)
                    continue;
                float reach = (l.Tunnel ? TunnelLampRange : StreetLampRange) + 40.0f;
                float d2 = Vector3.DistanceSquared(l.Pos, car);
                if (d2 > reach * reach)
                    continue;
                if (l.Wanted)
                    d2 *= IncumbentDistanceScale;
                int at = wantedLamps.Count;
                while (at > 0 && wantedScores[at - 1] > d2)
                    at--;
                if (at >= free)
                    continue;
                wantedLamps.Insert(at, l);
                wantedScores.Insert(at, d2);
                if (wantedLamps.Count > free)
                {
                    wantedLamps.RemoveAt(free);
                    wantedScores.RemoveAt(free);
                }
            }

            // 2. Update wanted flags, the active set and the eased intensities.
            for (int i = 0; i < activeLamps.Count; i++)
                activeLamps[i].Wanted = false;
            for (int i = 0; i < wantedLamps.Count; i++)
            {
                Lamp l = wantedLamps[i];
                l.Wanted = true;
                if (l.Active == false)
                {
                    l.Active = true;
                    activeLamps.Add(l);
                }
            }
            float step = frameDt / LampEaseSeconds;
            for (int i = activeLamps.Count - 1; i >= 0; i--)
            {
                Lamp l = activeLamps[i];
                if (l.Wanted)
                    l.Ease = Math.Min(1.0f, l.Ease + step);
                else
                {
                    l.Ease = Math.Max(0.0f, l.Ease - step);
                    if (l.Ease <= 0.0f)
                    {
                        l.Active = false;
                        activeLamps.RemoveAt(i);
                    }
                }
            }

            // 3. Output the `free` strongest. A crossfade between two lamps keeps
            // the total intensity continuous even with only two slots (the lamp
            // that is shown changes where both are equally bright).
            outputLamps.Clear();
            outputIntensity.Clear();
            for (int i = 0; i < activeLamps.Count; i++)
            {
                float v = LampLevel(activeLamps[i]);
                if (v <= 0.002f)
                    continue;
                int at = outputLamps.Count;
                while (at > 0 && outputIntensity[at - 1] < v)
                    at--;
                outputLamps.Insert(at, activeLamps[i]);
                outputIntensity.Insert(at, v);
            }
            for (int i = 0; i < outputLamps.Count && i < free; i++)
            {
                Lamp l = outputLamps[i];
                list.Add(new Light
                {
                    Position = l.Pos,
                    Color = (l.Tunnel ? TunnelLampColor : StreetLampColor) * outputIntensity[i],
                    Range = l.Tunnel ? TunnelLampRange : StreetLampRange,
                    Spot = false,
                });
            }
        }
    }
}
