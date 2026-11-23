using System;
using Microsoft.Xna.Framework;
using RacingGame.Properties;

namespace RacingGame.Graphics
{
    /// <summary>
    /// Lighting for the menu's rolling demo (the replay car driving behind the menus). The race
    /// screen updates the lighting every frame (UpdateForGame); the demo has no race, so without
    /// this the tunnel never darkened and the headlights, tunnel lamps and glows stayed off there.
    /// The demo always shows the day look (menus keep Day), only the tunnel and the car lights
    /// follow the car. Respects the player's Lighting setting (Off = nothing).
    /// </summary>
    public sealed partial class TrackLighting
    {
        /// <summary>
        /// Call every frame before the demo track is drawn (UIRenderer.RenderMenuTrackBackground).
        /// Returns false when nothing is lit (Lighting Off), so the caller can skip the glows.
        /// </summary>
        public bool UpdateForDemo(float elapsedSeconds)
        {
            Quality = GameSettings.Default.Lighting;
            DemoClockVisible = false;
            if (Quality == LightingQuality.Off)
            {
                EndDemoLook();
                if (TunnelBlend > 0.0f || headFade > 0.0f)
                {
                    // Lighting was switched off while the demo was lit: back to the plain look
                    headFade = brakeFade = 0.0f;
                    nightLatch = tunnelLatch = false;
                    ResetTunnel();
                }
                return false;
            }

            if (envInitialised == false)
                InitFromEnvironment();
            if (envTestMode == false)
            {
                // Show-off: the demo runs through a whole day (DemoSecondsPerDay) with the full
                // time of day look: sun and flare, sky, stars and moon, lamps, headlights, tunnels.
                if (demoPhaseStarted == false)
                    StartDemoClock();
                demoPhase += Math.Min(Math.Max(elapsedSeconds, 0.0f), 0.1f) / DemoSecondsPerDay;
                demoPhase -= (float)Math.Floor(demoPhase);
#if DEVSHOTS
                // The video harness sets the time of day (DEVSHOT_CLOCK) so the copies agree
                if (RacingGame.Helpers.DevShots.Active && RacingGame.Helpers.DevShots.ClockHour() >= 0.0f)
                    demoPhase = PhaseOfHours(RacingGame.Helpers.DevShots.ClockHour());
#endif
                Active = true;
                clockPhase = demoPhase;
                // The sun and the moon sweep once around the sky during the demo's day (races keep
                // their fixed light direction): the light direction, the visible sun, the moon in
                // the sky shader and the shadows all follow, elevation comes from the presets.
                sweepAngle = demoPhase * MathHelper.TwoPi;
                UpdateForGame(elapsedSeconds);
                demoLookApplied = true;
                DemoClockVisible = true;
                BuildDemoClockText();
                return true;
            }

            inGame = true;
            UpdateTunnel(elapsedSeconds);
            // The demo is always the day look; time of day is only evaluated in a race. Reset the
            // preset before the tunnel multipliers so they cannot compound across frames.
            if (TunnelBlend > 0.0f || tunnelApplied)
                Current = Preset.Day;
            ApplyTunnelToPreset();
            return true;
        }

        // Demo clock ---------------------------------------------------------

        /// <summary>Real seconds for one whole day (noon to noon) in the menu demo.</summary>
        public const float DemoSecondsPerDay = 120.0f;

        // 0 = noon, 0.5 = midnight, back to noon at 1 (same phase as the race clock). The demo
        // starts at the time of the computer's clock (so it starts differently every time),
        // or at RACINGGAME_DEMOCLOCK=HH:MM for tests.
        float demoPhase;
        bool demoPhaseStarted;
        bool demoLookApplied;

        /// <summary>Phase (0 = 12:00) of a time of day in hours.</summary>
        static float PhaseOfHours(float hours)
        {
            float phase = (hours - 12.0f) / 24.0f;
            return phase - (float)Math.Floor(phase);
        }

        void StartDemoClock()
        {
            demoPhaseStarted = true;
            float hours = (float)DateTime.Now.TimeOfDay.TotalHours;
            string env = Env("RACINGGAME_DEMOCLOCK");
            if (string.IsNullOrEmpty(env) == false)
            {
                string[] hm = env.Split(':');
                int h, m = 0;
                if (hm.Length >= 1 && int.TryParse(hm[0], out h) &&
                    (hm.Length < 2 || int.TryParse(hm[1], out m)))
                    hours = h + m / 60.0f;
            }
            demoPhase = PhaseOfHours(hours);
        }

        /// <summary>True while the demo shows its clock (not with Lighting Off or the test env vars).</summary>
        public bool DemoClockVisible { get; private set; }
        /// <summary>The demo's time as "HH:MM".</summary>
        public string DemoClockText { get; private set; } = "12:00";
        /// <summary>Name of the part of the day, for a small caption under the clock.</summary>
        public string DemoClockCaption { get; private set; } = "";

        void BuildDemoClockText()
        {
            // phase 0 = 12:00, 0.5 = 24:00
            float hours = (12.0f + 24.0f * demoPhase) % 24.0f;
            int totalMinutes = (int)(hours * 60.0f) % (24 * 60);
            DemoClockText = (totalMinutes / 60).ToString("00") + ":" + (totalMinutes % 60).ToString("00");
            float t = TimeOfDay;                       // 0 day .. 1 night, evaluated by UpdateForGame
            bool evening = demoPhase < 0.5f;
            if (t < 0.2f) DemoClockCaption = "Day";
            else if (t < 0.5f) DemoClockCaption = evening ? "Golden hour" : "Morning";
            else if (t < 0.8f) DemoClockCaption = evening ? "Dusk" : "Dawn";
            else DemoClockCaption = "Night";
        }

        /// <summary>
        /// Back to the plain day look after the demo (car selection, Lighting switched off): the
        /// menus other than the demo background must not inherit a night or dusk look.
        /// </summary>
        public void EndDemoLook()
        {
            DemoClockVisible = false;
            if (demoLookApplied == false)
                return;
            demoLookApplied = false;
            sweepAngle = 0.0f;
            Active = false;
            headFade = brakeFade = 0.0f;
            nightLatch = tunnelLatch = false;
            Evaluate(0.0f);
            Current = Preset.Day;
            FlareAlpha = 1.0f;
            ApplyShadowColor();
            ResetTunnel();
            inGame = false;
        }
    }
}
