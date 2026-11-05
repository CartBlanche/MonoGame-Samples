//-----------------------------------------------------------------------------
// PointerTracker.cs
//
// One pointer from two devices. The mouse and the primary touch become a single
// position with press and release edges, so game code asks one question ("where
// is the pointer, did it go down or up this frame?") and never branches on the
// platform. Pure logic: InputState feeds it the real devices each frame, and
// tests feed it scripted samples.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using System.Collections.Generic;

namespace CardsFramework
{
    /// <summary>One touch contact for the frame, already in the game's coordinate space.</summary>
    public readonly struct TouchSample
    {
        public int Id { get; }
        public Vector2 Position { get; }
        public TouchLocationState State { get; }

        public TouchSample(int id, Vector2 position, TouchLocationState state)
        {
            Id = id;
            Position = position;
            State = state;
        }
    }

    /// <summary>
    /// Combines the mouse and the touch panel into one pointer. The primary touch is the first finger that lands, followed
    /// by its id so a second finger cannot take over; while a touch is active it wins over the mouse. Unlike the cursor
    /// location, the pointer follows a finger as it moves, which a drag needs.
    /// </summary>
    public sealed class PointerTracker
    {
        private const int NoTouch = -1;

        private int primaryTouchId = NoTouch;
        private bool mouseWasDown;

        /// <summary>Where the pointer is now: the primary touch while one is active, otherwise the mouse (every frame, so hover works).</summary>
        public Vector2 Position { get; private set; }

        /// <summary>True while the primary touch or the left mouse button is held.</summary>
        public bool Down { get; private set; }

        /// <summary>True only on the frame the pointer goes down.</summary>
        public bool Pressed { get; private set; }

        /// <summary>True only on the frame the pointer comes up.</summary>
        public bool Released { get; private set; }

        /// <summary>Where the pointer was when it last went down.</summary>
        public Vector2 PressPosition { get; private set; }

        /// <summary>Where the pointer was when it last came up.</summary>
        public Vector2 ReleasePosition { get; private set; }

        /// <summary>Which device produced the pointer this frame (the last one used while nothing is active).</summary>
        public PointerSource Source { get; private set; }

        /// <summary>True while a touch is the primary pointer (a finger is down).</summary>
        public bool TouchActive => primaryTouchId != NoTouch;

        /// <summary>
        /// Feeds one frame. <paramref name="mousePosition"/> is null where there is no mouse (a phone).
        /// <paramref name="touches"/> are this frame's touch contacts, in order.
        /// </summary>
        public void Update(Vector2? mousePosition, bool mouseLeftDown, IReadOnlyList<TouchSample> touches)
        {
            Pressed = false;
            Released = false;

            bool touchHandled = UpdateTouch(touches);
            if (touchHandled)
            {
                mouseWasDown = mouseLeftDown; // keep the mouse edge in step so lifting the finger does not look like a click
                return;
            }

            if (mousePosition.HasValue)
            {
                Position = mousePosition.Value;
                Source = PointerSource.Mouse;

                bool pressed = mouseLeftDown && !mouseWasDown;
                bool released = !mouseLeftDown && mouseWasDown;
                Down = mouseLeftDown;
                if (pressed) { Pressed = true; PressPosition = Position; }
                if (released) { Released = true; ReleasePosition = Position; }
            }
            else
            {
                Down = false;
            }

            mouseWasDown = mouseLeftDown;
        }

        private bool UpdateTouch(IReadOnlyList<TouchSample> touches)
        {
            if (touches == null) touches = System.Array.Empty<TouchSample>();

            if (primaryTouchId == NoTouch)
            {
                // The first finger that lands becomes the primary touch.
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].State == TouchLocationState.Pressed)
                    {
                        primaryTouchId = touches[i].Id;
                        break;
                    }
                }
                if (primaryTouchId == NoTouch) return false;
            }

            bool seen = false;
            for (int i = 0; i < touches.Count; i++)
            {
                TouchSample touch = touches[i];
                if (touch.Id != primaryTouchId) continue;
                seen = true;
                Position = touch.Position;
                Source = PointerSource.Touch;

                switch (touch.State)
                {
                    case TouchLocationState.Pressed:
                        Down = true;
                        Pressed = true;
                        PressPosition = touch.Position;
                        break;
                    case TouchLocationState.Moved:
                        Down = true;
                        break;
                    case TouchLocationState.Released:
                    case TouchLocationState.Invalid:
                        Down = false;
                        Released = true;
                        ReleasePosition = touch.Position;
                        primaryTouchId = NoTouch;
                        break;
                }
                if (primaryTouchId == NoTouch) break; // released: ignore anything after it this frame
            }

            if (!seen)
            {
                // The finger vanished from the collection without a Released entry (cancelled): treat it as lifted.
                Down = false;
                Released = true;
                ReleasePosition = Position;
                primaryTouchId = NoTouch;
                Source = PointerSource.Touch;
            }

            return true;
        }
    }
}
