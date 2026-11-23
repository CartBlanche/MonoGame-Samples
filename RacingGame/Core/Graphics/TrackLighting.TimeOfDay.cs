using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RacingGame.GameLogic;
using RacingGame.Helpers;
using RacingGame.Properties;
using RacingGame.GameScreens;
using RacingGame.Shaders;

namespace RacingGame.Graphics
{
    /// <summary>
    /// WS1: time of day (Path A, C# only). TimeOfDay 0 = noon (today's look),
    /// 0.35 golden hour, 0.65 dusk, 1 = night. Everything reads the evaluated
    /// Current preset and the fields below every frame.
    /// Shipped behaviour: Active is false, so nothing here changes the game.
    /// Test mode: env RACINGGAME_TIMEOFDAY (0..1 start), RACINGGAME_CLOCK (seconds
    /// per full day, loops noon, dusk, night, dawn, noon), keys only with
    /// RACINGGAME_DEBUGKEYS=1: ] cycles speed (0, 1x, 10x, 60x), [ pause or resume,
    /// - and = scrub. The clock advances on render time only, never physics.
    /// </summary>
    public sealed partial class TrackLighting
    {
        struct Keyframe
        {
            public float Time;
            public Vector3 Ambient, Diffuse, Specular, SkyTint;
            /// <summary>Sun height factor (1 = today's sun position).</summary>
            public float SunHeight;
            public float Flare, Night;
            public float GlowIntensity, GlowHighlight, GlowThreshold;
            public Vector3 ShadowColor;
            /// <summary>
            /// Integrated shadows: the tuning value. Multiplies the ambient (sky) light in shadow: 1 = the
            /// sun term is simply removed (measured shadow/lit luminance ratio about 0.4 to 0.5), above 1
            /// adds sky and bounce light (softer shadows), a blue bias gives cooler shadows.
            /// </summary>
            public Vector3 SkyFill;
            public Vector2 Fog;
            public Vector3 FogColor;
        }

        static Keyframe[] keyframes = new Keyframe[]
        {
            // Day: exactly today's values (all neutral).
            new Keyframe { Time = 0.0f,
                Ambient = Vector3.One, Diffuse = Vector3.One, Specular = Vector3.One,
                SkyTint = Vector3.One, SunHeight = 1.0f, Flare = 1.0f, Night = 0.0f,
                GlowIntensity = 0.7f, GlowHighlight = 0.4f, GlowThreshold = 0.975f,
                ShadowColor = new Vector3(0.25f, 0.26f, 0.27f),
                SkyFill = new Vector3(1.0f, 1.0f, 1.0f),
                Fog = Vector2.Zero, FogColor = Vector3.One },
            // Golden hour
            new Keyframe { Time = 0.35f,
                Ambient = new Vector3(0.95f, 0.82f, 0.74f),
                Diffuse = new Vector3(1.0f, 0.78f, 0.52f),
                Specular = new Vector3(1.0f, 0.8f, 0.6f),
                SkyTint = new Vector3(1.0f, 0.8f, 0.62f),
                SunHeight = 0.6f, Flare = 0.8f, Night = 0.1f,
                GlowIntensity = 0.7f, GlowHighlight = 0.45f, GlowThreshold = 0.96f,
                ShadowColor = new Vector3(0.28f, 0.27f, 0.32f),
                SkyFill = new Vector3(0.95f, 1.1f, 1.4f),
                Fog = new Vector2(0.0004f, 150.0f), FogColor = new Vector3(0.95f, 0.72f, 0.5f) },
            // Dusk
            new Keyframe { Time = 0.65f,
                Ambient = new Vector3(0.56f, 0.52f, 0.7f),
                Diffuse = new Vector3(0.6f, 0.4f, 0.46f),
                Specular = new Vector3(0.5f, 0.4f, 0.5f),
                SkyTint = new Vector3(0.5f, 0.42f, 0.62f),
                SunHeight = 0.35f, Flare = 0.0f, Night = 0.5f,
                GlowIntensity = 0.75f, GlowHighlight = 0.5f, GlowThreshold = 0.93f,
                ShadowColor = new Vector3(0.4f, 0.4f, 0.48f),
                SkyFill = new Vector3(1.5f, 1.6f, 1.9f),
                Fog = new Vector2(0.0008f, 120.0f), FogColor = new Vector3(0.4f, 0.32f, 0.5f) },
            // Night (moonlight)
            new Keyframe { Time = 1.0f,
                Ambient = new Vector3(0.24f, 0.28f, 0.44f),
                Diffuse = new Vector3(0.12f, 0.16f, 0.32f),
                Specular = new Vector3(0.2f, 0.25f, 0.45f),
                SkyTint = new Vector3(0.07f, 0.09f, 0.2f),
                SunHeight = 0.7f, Flare = 0.0f, Night = 1.0f,
                GlowIntensity = 0.8f, GlowHighlight = 0.6f, GlowThreshold = 0.88f,
                ShadowColor = new Vector3(0.85f, 0.86f, 0.9f),
                SkyFill = new Vector3(1.0f, 1.0f, 1.0f),
                Fog = new Vector2(0.0012f, 100.0f), FogColor = new Vector3(0.04f, 0.06f, 0.12f) },
        };

        // Evaluated state (all neutral at Day) -------------------------------

        /// <summary>0 = noon, 1 = midnight.</summary>
        public float TimeOfDay = 0.0f;
        /// <summary>0 at day, 1 at night (smooth).</summary>
        public float NightFactor = 0.0f;
        /// <summary>
        /// How visible the night stars layer is (0 by day and at golden hour, 1 at night).
        /// </summary>
        public float StarBlend
        {
            get { return MathHelper.SmoothStep(0.0f, 1.0f, MathHelper.Clamp((NightFactor - 0.4f) / 0.6f, 0.0f, 1.0f)); }
        }
        /// <summary>Lens flare brightness (0 = hidden).</summary>
        public float FlareAlpha = 1.0f;
        public float GlowIntensity = 0.7f, GlowHighlight = 0.4f, GlowThreshold = 0.975f;
        public Vector3 ShadowColor = new Vector3(0.25f, 0.26f, 0.27f);
        /// <summary>Integrated shadows: ambient fill in shadow (see Keyframe.SkyFill).</summary>
        public Vector3 SkyFill = Vector3.One;
        Vector3 skyTint = Vector3.One;
        float sunHeight = 1.0f;
        /// <summary>
        /// Rotation of the sun, the light and the moon around the up axis, in radians. Only the
        /// menu demo sweeps them across the sky during its day (TrackLighting.Demo.cs); a race
        /// always has 0 (tracks have a fixed time of day and a fixed light direction).
        /// </summary>
        float sweepAngle;

        /// <summary>
        /// True while a race runs and time of day is requested (env var or key).
        /// False in shipped play: the game looks exactly as before.
        /// </summary>
        public bool Active;

        /// <summary>
        /// Lighting setting of the player (read once at race start, BeginRace). Off is the
        /// plain day game: no time of day, tunnel darkening, headlights, lamps or stars.
        /// Low: time of day, tunnels, two headlights and glows. High: also lamp lights and
        /// the tail point light. This is the single gate the producers read.
        /// </summary>
        public LightingQuality Quality = LightingQuality.High;

        /// <summary>Built-in tracks: Beginner day, Advanced dusk, Expert night.</summary>
        static readonly float[] BuiltInTimeOfDay = { 0.0f, 0.65f, 1.0f };

        /// <summary>
        /// Called when a race starts: reads the player's settings and picks the time of day
        /// (the track's own, or the override). Fixed for the whole race, visual only.
        /// The env vars RACINGGAME_TIMEOFDAY / RACINGGAME_CLOCK stay a test mode on top.
        /// </summary>
        public void BeginRace(float trackTimeOfDay)
        {
            if (envInitialised == false)
                InitFromEnvironment();
            bool envTest = envTestMode;
            Quality = GameSettings.Default.Lighting;
            float tod = trackTimeOfDay;
            switch (GameSettings.Default.TimeOfDayMode)
            {
                case TimeOfDayMode.Day: tod = 0.0f; break;
                case TimeOfDayMode.Dusk: tod = 0.65f; break;
                case TimeOfDayMode.Night: tod = 1.0f; break;
            }
            if (float.IsNaN(tod))
                tod = 0.0f;
            tod = MathHelper.Clamp(tod, 0.0f, 1.0f);

            // Start every race from the plain day look (also clears a previous race)
            inGame = true;
            EndGame();
            headFade = brakeFade = 0.0f;
            nightLatch = tunnelLatch = false;
            if (Quality == LightingQuality.Off)
            {
                Active = false;
                return;
            }
            if (envTest == false)
            {
                Active = tod > 0.0f;
                clockPhase = tod * 0.5f;
                speedIndex = 0;
            }
            else
                Active = true;
            Log.Write("BeginRace lighting=" + Quality + " timeOfDay=" + tod);
        }

        /// <summary>Time of day of the race the player is about to start.</summary>
        public static float RaceTimeOfDay()
        {
            if (TrackSelection.CustomTrack != null)
                return TrackSelection.CustomTrack.Data != null ?
                    TrackSelection.CustomTrack.Data.TimeOfDay : 0.0f;
            int n = TrackSelection.SelectedTrackNumber;
            return n >= 0 && n < BuiltInTimeOfDay.Length ? BuiltInTimeOfDay[n] : 0.0f;
        }

        // Clock (test mode)
        static string Env(string n) { return Environment.GetEnvironmentVariable(n); }
        static readonly bool debugKeys = Env("RACINGGAME_DEBUGKEYS") == "1";
        float clockPhase;               // 0..1, one full day (noon, night, noon)
        float clockSecondsPerDay = 120.0f;
        static readonly float[] speedSteps = { 0.0f, 1.0f, 10.0f, 60.0f };
        int speedIndex;
        bool clockPaused;
        bool envInitialised;
        bool envTestMode;
        bool inGame;

        /// <summary>Sun position (lens flare origin) for the current time.</summary>
        public Vector3 SunPos
        {
            get
            {
                Vector3 p = LensFlare.DefaultSunPos;
                p.Z *= sunHeight;
                if (sweepAngle != 0.0f)
                    p = Vector3.Transform(p, Matrix.CreateRotationZ(sweepAngle));
                if (IntegratedShadows)
                {
                    // One sun: the visible sun sits along the lighting/shadow direction, at the
                    // distance of the classic sun (the lit look is unchanged).
                    Vector3 l = LightPos;
                    p = Vector3.Normalize(l) * LensFlare.DefaultSunPos.Length();
                }
                return p;
            }
        }

        /// <summary>Light position (direction to the sun) for the current time.</summary>
        public Vector3 LightPos
        {
            get
            {
                Vector3 p = LensFlare.DefaultLightPos;
                p.Z *= sunHeight;
                if (sweepAngle != 0.0f)
                    p = Vector3.Transform(p, Matrix.CreateRotationZ(sweepAngle));
                return p;
            }
        }

        /// <summary>Colour handed to LensFlare.Render (white at day).</summary>
        public Color FlareColor
        {
            get
            {
                float alpha = FlareAlpha * (1.0f - TunnelBlend);
                if (alpha >= 1.0f)
                    return Color.White;
                byte b = (byte)MathHelper.Clamp(alpha * 255.0f, 0, 255);
                return new Color(b, b, b, (byte)255);
            }
        }

        void InitFromEnvironment()
        {
            envInitialised = true;
            float start;
            bool haveStart = float.TryParse(Env("RACINGGAME_TIMEOFDAY"),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out start);
            float secs;
            bool haveClock = float.TryParse(Env("RACINGGAME_CLOCK"),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out secs) && secs > 0;
            if (haveClock)
            {
                clockSecondsPerDay = secs;
                speedIndex = 1;
            }
            if (haveStart)
                clockPhase = MathHelper.Clamp(start, 0, 1) * 0.5f;
            Active = haveStart || haveClock;
            envTestMode = Active;
        }

        /// <summary>
        /// Call once per frame from GameScreen.Render (render time only).
        /// Updates the clock, evaluates the preset and sets the sun.
        /// </summary>
        public void UpdateForGame(float elapsedSeconds)
        {
            if (envInitialised == false)
                InitFromEnvironment();
            inGame = true;

            // Off: today's game exactly, nothing is evaluated
            if (Quality == LightingQuality.Off)
                return;

            if (debugKeys)
                HandleDebugKeys();

            UpdateTunnel(elapsedSeconds);
            if (Active == false)
            {
                ApplyTunnelToPreset();
                return;
            }

            if (clockPaused == false && speedSteps[speedIndex] > 0)
            {
                clockPhase += elapsedSeconds * speedSteps[speedIndex] /
                    clockSecondsPerDay;
                clockPhase -= (float)Math.Floor(clockPhase);
            }

            // Loop 0..1..0 (noon, dusk, night, dawn, noon)
            TimeOfDay = clockPhase < 0.5f ? clockPhase * 2.0f : 2.0f - clockPhase * 2.0f;
            Evaluate(TimeOfDay);

            // Sun, also used by the shadow map. The flare origin follows.
            BaseGame.LightDirection = LightPos;
            LensFlare.Origin3D = SunPos;
            ApplyShadowColor();
            ApplyTunnelToPreset();
        }

        /// <summary>Back to today's look (leaving the race, menus use Day).</summary>
        public void EndGame()
        {
            sweepAngle = 0.0f;
            if (inGame == false)
                return;
            inGame = false;
            ResetTunnel();
            if (Active == false)
                return;
            Evaluate(0.0f);
            Current = Preset.Day;
            FlareAlpha = 1.0f;
            ApplyShadowColor();
            LensFlare.Origin3D = IntegratedShadows ? SunPos : LensFlare.DefaultSunPos;
        }

        void HandleDebugKeys()
        {
            if (Input.KeyboardKeyJustPressed(Keys.OemCloseBrackets))
            {
                Active = true;
                speedIndex = (speedIndex + 1) % speedSteps.Length;
                Log.Write("TimeOfDay clock speed x" + speedSteps[speedIndex]);
            }
            if (Input.KeyboardKeyJustPressed(Keys.OemOpenBrackets))
            {
                Active = true;
                clockPaused = !clockPaused;
                Log.Write("TimeOfDay clock paused=" + clockPaused);
            }
            if (Input.KeyboardKeyJustPressed(Keys.OemMinus))
            {
                Active = true;
                clockPhase = Math.Max(0, clockPhase - 0.025f);
            }
            if (Input.KeyboardKeyJustPressed(Keys.OemPlus))
            {
                Active = true;
                clockPhase = Math.Min(0.5f, clockPhase + 0.025f);
            }
        }

        void Evaluate(float t)
        {
            t = MathHelper.Clamp(t, 0, 1);
            int i = 0;
            while (i < keyframes.Length - 2 && t > keyframes[i + 1].Time)
                i++;
            Keyframe a = keyframes[i], b = keyframes[i + 1];
            float u = MathHelper.Clamp((t - a.Time) / (b.Time - a.Time), 0, 1);
            u = u * u * (3.0f - 2.0f * u);

            Current.AmbientTint = Vector3.Lerp(a.Ambient, b.Ambient, u);
            Current.DiffuseTint = Vector3.Lerp(a.Diffuse, b.Diffuse, u);
            Current.SpecularTint = Vector3.Lerp(a.Specular, b.Specular, u);
            Current.AmbSky = Current.AmbGround = Vector3.One;
            Current.Fog = Vector2.Lerp(a.Fog, b.Fog, u);
            Current.FogColor = Vector3.Lerp(a.FogColor, b.FogColor, u);
            skyTint = Vector3.Lerp(a.SkyTint, b.SkyTint, u);
            sunHeight = MathHelper.Lerp(a.SunHeight, b.SunHeight, u);
            FlareAlpha = MathHelper.Lerp(a.Flare, b.Flare, u);
            NightFactor = MathHelper.Lerp(a.Night, b.Night, u);
            GlowIntensity = MathHelper.Lerp(a.GlowIntensity, b.GlowIntensity, u);
            GlowHighlight = MathHelper.Lerp(a.GlowHighlight, b.GlowHighlight, u);
            GlowThreshold = MathHelper.Lerp(a.GlowThreshold, b.GlowThreshold, u);
            ShadowColor = Vector3.Lerp(a.ShadowColor, b.ShadowColor, u);
            SkyFill = Vector3.Lerp(a.SkyFill, b.SkyFill, u);
        }

        void ApplyShadowColor()
        {
            var fx = ShaderEffect.shadowMapping != null ?
                ShaderEffect.shadowMapping.Effect : null;
            var p = fx != null ? fx.Parameters["ShadowColor"] : null;
            if (p != null)
                p.SetValue(new Vector4(Vector3.Lerp(ShadowColor, One, TunnelBlend), 1.0f));
        }

        /// <summary>Sky tint multiplier for the cube map (1 at day).</summary>
        Color TintSky(Color daySky)
        {
            if (skyTint == One)
                return daySky;
            return Scale(daySky, skyTint);
        }
    }
}
