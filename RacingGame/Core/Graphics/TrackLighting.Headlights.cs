using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RacingGame.GameLogic;
using RacingGame.Helpers;
using RacingGame.Properties;

namespace RacingGame.Graphics
{
    /// <summary>
    /// WS3: car headlights and tail lights. Auto on/off with hysteresis (night or tunnel),
    /// 0.3 s fade, two spot lights on the local car, glow sprites for local and remote cars.
    /// In shipped Day play with no tunnel the fade stays 0: no lights, no sprites, legacy path.
    /// </summary>
    public sealed partial class TrackLighting
    {
        // Tuning (tuned from frames; the env-var tuning hooks were removed, see Lighting-Rig-testlights.patch)
        static readonly float HeadInner = MathHelper.ToRadians(12);
        static readonly float HeadOuter = MathHelper.ToRadians(30);
        const float HeadRange = 80.0f;
        const float HeadIntensity = 5.0f;
        static readonly Vector3 HeadColor = new Vector3(1.0f, 0.92f, 0.75f);
        static readonly Vector3 TailColor = new Vector3(1.0f, 0.06f, 0.03f);
        static readonly bool headDebugKeys = Env("RACINGGAME_DEBUGKEYS") == "1";

        const float FadeSeconds = 0.3f;

        /// <summary>Glow billboards shared by headlights, tail lights and (WS4) lamps.</summary>
        public readonly LightSprites Sprites = new LightSprites();

        bool nightLatch, tunnelLatch;
        /// <summary>0 = auto, 1 = forced on, 2 = forced off (debug key H).</summary>
        int manualMode;
        float headFade;      // 0..1
        float brakeFade;     // 0..1
        int lastHeadFrame = -1;

        /// <summary>Current headlight intensity 0..1 (0 = off, nothing is rendered).</summary>
        public float HeadlightFade { get { return headFade; } }

        void UpdateHeadlightState()
        {
            if (lastHeadFrame == BaseGame.TotalFrames)
                return;
            lastHeadFrame = BaseGame.TotalFrames;
            float dt = Math.Min(BaseGame.ElapsedTimeThisFrameInMilliseconds / 1000.0f, 0.1f);

            if (headDebugKeys && inGame && Input.KeyboardKeyJustPressed(Keys.H))
            {
                manualMode = (manualMode + 1) % 3;
                Log.Write("Headlights manual mode " + manualMode + " (0 auto, 1 on, 2 off)");
            }

            // Hysteresis: night on > 0.35, off < 0.25; tunnel on > 0.3, off < 0.2
            if (nightLatch) { if (NightFactor < 0.25f) nightLatch = false; }
            else if (NightFactor > 0.35f) nightLatch = true;
            if (tunnelLatch) { if (TunnelBlend < 0.2f) tunnelLatch = false; }
            else if (TunnelBlend > 0.3f) tunnelLatch = true;

            bool want = inGame && Quality != LightingQuality.Off && (manualMode == 1 || (manualMode == 0 && (nightLatch || tunnelLatch)));
            float target = want ? 1.0f : 0.0f;
            float step = dt / FadeSeconds;
            headFade = target > headFade ? Math.Min(target, headFade + step) : Math.Max(target, headFade - step);

            // Brake lights follow the same keys CarPhysics uses to brake (read only)
            bool braking = inGame && RacingGameManager.Player.IsBraking;
            float bt = braking ? 1.0f : 0.0f;
            float bs = dt / 0.08f;
            brakeFade = bt > brakeFade ? Math.Min(bt, brakeFade + bs) : Math.Max(bt, brakeFade - bs);
        }

        partial void CollectHeadlights(List<Light> list)
        {
            UpdateHeadlightState();
            if (headFade <= 0.0f)
                return;

            Matrix m = Model.CarLightMatrix(RacingGameManager.Player.CarRenderMatrix);
            Vector3 dir = Vector3.TransformNormal(Model.HeadlightDirLocal, m);
            Vector3 color = HeadColor * (HeadIntensity * headFade);
            list.Add(HeadSpot(Vector3.Transform(Model.HeadlightLeftLocal, m), dir, color));
            list.Add(HeadSpot(Vector3.Transform(Model.HeadlightRightLocal, m), dir, color));
        }

        static Light HeadSpot(Vector3 pos, Vector3 dir, Vector3 color)
        {
            return new Light
            {
                Position = pos,
                Direction = dir,
                Color = color,
                Range = HeadRange,
                Spot = true,
                InnerAngle = HeadInner,
                OuterAngle = HeadOuter,
            };
        }

        /// <summary>
        /// Queue the glow sprites of one car (local or remote). Nothing is queued while
        /// the lights are off.
        /// </summary>
        public void AddCarSprites(Matrix carRenderMatrix, bool local)
        { AddCarSprites(carRenderMatrix, local, local ? brakeFade : 0.0f); }

        /// <summary>Remote cars: braking comes from the received flag, eased here per car.</summary>
        public void AddCarSprites(Matrix carRenderMatrix, bool local, float brake)
        {
            if (headFade <= 0.0f)
                return;
            Matrix m = Model.CarLightMatrix(carRenderMatrix);
            Vector3 hc = HeadColor * (0.9f * headFade);
            Sprites.Add(Vector3.Transform(Model.HeadlightLeftLocal, m), hc, 1.4f);
            Sprites.Add(Vector3.Transform(Model.HeadlightRightLocal, m), hc, 1.4f);
            float b = brake;
            Vector3 tc = TailColor * ((0.6f + 0.4f * b) * headFade);
            float size = 0.7f + 0.4f * b;
            Sprites.Add(Vector3.Transform(Model.TailLightLeftLocal, m), tc, size);
            Sprites.Add(Vector3.Transform(Model.TailLightRightLocal, m), tc, size);
        }
    }
}
