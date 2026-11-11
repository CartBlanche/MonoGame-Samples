//-----------------------------------------------------------------------------
// CardJuice.cs
//
// Per-card "feel" state: hover lean, pick-up lift, drag lean with swing-back,
// landing squash. It depends on no renderer or component. A game reads the
// outputs (rotation, scale, offset, shadow) and applies them however it draws.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;

namespace CardsFramework
{
    /// <summary>
    /// Physical-feeling card state driven by springs. Call <see cref="Update"/> every frame,
    /// then read <see cref="Rotation"/>, <see cref="Scale"/>, <see cref="PositionOffset"/>
    /// and <see cref="GetShadowOffset"/>.
    /// </summary>
    public class CardJuice
    {
        // Tuning (pixels / radians). Public so a game can adjust them per theme.
        public float LiftPixels = 6f;            // how far a fully lifted card rises
        public float LiftScale = 0.08f;          // extra scale at full lift
        public float HoverLiftAmount = 0.35f;    // lift contributed by hover alone
        public float MaxRoll = 0.21f;            // about 12 degrees
        public float RollPerPixelPerSecond = 0.0006f;
        public float MaxTilt = 0.18f;            // radians-equivalent used for squash/parallax
        public float ParallaxPixels = 3f;
        public float BaseShadowDistance = 2f;
        public float LiftedShadowDistance = 14f;

        private Spring lift = new Spring(260f, 22f);           // slightly underdamped: soft overshoot
        private Spring tiltX = new Spring(220f, 18f);
        private Spring tiltY = new Spring(220f, 18f);
        private Spring roll = new Spring(170f, 7f);            // underdamped: swings back when released
        private Spring squash = new Spring(420f, 14f, 1f);     // overshoots past 1 on landing

        private bool hovered;
        private bool grabbed;

        /// <summary>Total lift 0..1 (hover and grab combined). Springs, so it may briefly leave 0..1.</summary>
        public float Lift => lift.Value;
        public float TiltX => tiltX.Value;
        public float TiltY => tiltY.Value;
        public float Squash => squash.Value;
        public bool IsGrabbed => grabbed;
        public bool IsHovered => hovered;

        /// <summary>Rotation in radians from drag lean.</summary>
        public float Rotation => roll.Value;

        /// <summary>Scale multiplier: lift grows it, landing squashes it, tilt narrows the width.</summary>
        public Vector2 Scale
        {
            get
            {
                float s = 1f + LiftScale * lift.Value;
                float tilt = (float)Math.Cos(MathHelper.Clamp(Math.Abs(tiltX.Value) * MaxTilt, 0f, 1f));
                return new Vector2(s * tilt / Math.Max(0.2f, squash.Value), s * squash.Value);
            }
        }

        /// <summary>Like <see cref="Scale"/> but without the tilt narrowing, for callers that draw the tilt in 3D themselves.</summary>
        public Vector2 ScaleNoTilt
        {
            get
            {
                float s = 1f + LiftScale * lift.Value;
                return new Vector2(s / Math.Max(0.2f, squash.Value), s * squash.Value);
            }
        }

        /// <summary>Offset to add to the card position: lifted cards rise, tilt shifts slightly.</summary>
        public Vector2 PositionOffset => new Vector2(0f, -LiftPixels * lift.Value);

        /// <summary>Offset for inner layers (suit, rank) so a tilted card reads as having depth.</summary>
        public Vector2 GetParallaxOffset(float layerDepth) =>
            new Vector2(tiltX.Value, tiltY.Value) * (ParallaxPixels * layerDepth);

        /// <summary>
        /// Shadow offset in pixels. Grows with lift, and leans opposite the card's roll,
        /// like a light from the upper left.
        /// </summary>
        public Vector2 GetShadowOffset()
        {
            float d = MathHelper.Lerp(BaseShadowDistance, LiftedShadowDistance, MathHelper.Clamp(lift.Value, 0f, 1.5f));
            return new Vector2(d * 0.6f - roll.Value * 18f, d);
        }

        /// <summary>Shadow opacity multiplier: softer and fainter as the card lifts.</summary>
        public float ShadowAlpha => MathHelper.Clamp(0.35f - 0.12f * lift.Value, 0.15f, 0.4f);

        /// <summary>True while anything is still moving, so callers can skip work.</summary>
        public bool IsActive =>
            hovered || grabbed || !lift.IsAtRest() || !roll.IsAtRest() ||
            !tiltX.IsAtRest() || !tiltY.IsAtRest() || !squash.IsAtRest();

        /// <summary>
        /// Pointer is over the card. <paramref name="offsetFromCentre"/> is the pointer position relative to
        /// the card centre, normalised so the card edges are +-1; pass null when not hovered.
        /// </summary>
        public void SetHover(Vector2? offsetFromCentre)
        {
            hovered = offsetFromCentre.HasValue;
            if (hovered)
            {
                tiltX.Target = MathHelper.Clamp(offsetFromCentre.Value.X, -1f, 1f);
                tiltY.Target = MathHelper.Clamp(offsetFromCentre.Value.Y, -1f, 1f);
            }
            else
            {
                tiltX.Target = 0f;
                tiltY.Target = 0f;
            }
            RefreshLiftTarget();
        }

        /// <summary>The card was picked up.</summary>
        public void Grab()
        {
            grabbed = true;
            RefreshLiftTarget();
        }

        /// <summary>
        /// Feed pointer velocity (pixels/second) while dragging. The card leans the way it is dragged;
        /// when the pointer stops, the roll spring swings it back past upright and settles.
        /// </summary>
        public void Drag(Vector2 pointerVelocity)
        {
            roll.Target = MathHelper.Clamp(pointerVelocity.X * RollPerPixelPerSecond, -MaxRoll, MaxRoll);
        }

        /// <summary>The pointer has stopped or the card was released: the roll target returns to upright.</summary>
        public void StopDrag() => roll.Target = 0f;

        /// <summary>The card was put down. <paramref name="strength"/> 0..1 scales the landing squash.</summary>
        public void Release(float strength = 1f)
        {
            grabbed = false;
            roll.Target = 0f;
            RefreshLiftTarget();
            strength = MathHelper.Clamp(strength, 0f, 1f);
            squash.Value = 1f - 0.06f * strength;   // compress, then spring back past 1 and settle
            squash.Velocity = 0f;
        }

        /// <summary>A neighbour landed nearby: nudge this card (used for ripples). Positive pushes it down first.</summary>
        public void Nudge(float strength)
        {
            squash.Impulse(-strength * 0.8f);
            lift.Impulse(-strength * 2f);
        }

        public void Update(float dt)
        {
            lift.Update(dt);
            tiltX.Update(dt);
            tiltY.Update(dt);
            roll.Update(dt);
            squash.Update(dt);
        }

        /// <summary>Resets everything to rest (e.g. when a card is recycled).</summary>
        public void Reset()
        {
            hovered = grabbed = false;
            lift.Snap(0f);
            tiltX.Snap(0f);
            tiltY.Snap(0f);
            roll.Snap(0f);
            squash.Snap(1f);
        }

        private void RefreshLiftTarget()
        {
            lift.Target = grabbed ? 1f : (hovered ? HoverLiftAmount : 0f);
        }
    }
}
