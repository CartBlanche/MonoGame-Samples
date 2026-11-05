//-----------------------------------------------------------------------------
// PointerGestures.cs
//
// Turns a stream of pointer samples (a position and whether the button or finger
// is down, one sample per frame) into the gestures a card game needs: click,
// double-click, drag, long-press. Pure logic with no device access, driven by
// the game clock, so it can be unit-tested with scripted sequences.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace CardsFramework
{
    /// <summary>Which device is producing the pointer.</summary>
    public enum PointerSource
    {
        /// <summary>No pointer has been seen yet.</summary>
        None,
        Mouse,
        Touch,
    }

    /// <summary>The kinds of gesture the recognizer reports.</summary>
    public enum PointerGestureKind
    {
        /// <summary>Pressed and released without moving far or holding long: a click or tap.</summary>
        Click,

        /// <summary>A second click close enough in time and place to the first. Reported after the second <see cref="Click"/>.</summary>
        DoubleClick,

        /// <summary>The pointer moved past the drag threshold while down.</summary>
        DragStart,

        /// <summary>The pointer moved while dragging.</summary>
        DragMove,

        /// <summary>The pointer was released while dragging (the drop).</summary>
        DragEnd,

        /// <summary>A drag was abandoned without a release (see <see cref="PointerGestureRecognizer.Cancel"/>).</summary>
        DragCancel,

        /// <summary>The pointer stayed down and nearly still for the long-press time. No click follows its release.</summary>
        LongPress,
    }

    /// <summary>One gesture event. Positions are in the same space as the samples fed in (the game's base resolution).</summary>
    public readonly struct PointerGesture
    {
        public PointerGestureKind Kind { get; }

        /// <summary>Where the pointer is for this event.</summary>
        public Vector2 Position { get; }

        /// <summary>Where the press began.</summary>
        public Vector2 StartPosition { get; }

        /// <summary>
        /// Movement since the previous drag event (for <see cref="PointerGestureKind.DragMove"/>); from the start to the
        /// position where the threshold was crossed (for <see cref="PointerGestureKind.DragStart"/>); zero otherwise.
        /// </summary>
        public Vector2 Delta { get; }

        /// <summary>How long the pointer had been down.</summary>
        public TimeSpan HeldFor { get; }

        public PointerSource Source { get; }

        /// <summary>1 for a click, 2 for a double-click, 0 for everything else.</summary>
        public int ClickCount { get; }

        public PointerGesture(PointerGestureKind kind, Vector2 position, Vector2 startPosition, Vector2 delta,
                              TimeSpan heldFor, PointerSource source, int clickCount = 0)
        {
            Kind = kind;
            Position = position;
            StartPosition = startPosition;
            Delta = delta;
            HeldFor = heldFor;
            Source = source;
            ClickCount = clickCount;
        }
    }

    /// <summary>
    /// The thresholds, in the game's base resolution units. A finger is less precise than a mouse, so each has its own.
    /// </summary>
    public sealed class PointerGestureOptions
    {
        /// <summary>How far the mouse must move while down before it counts as a drag.</summary>
        public float MouseDragThreshold { get; set; } = 6f;

        /// <summary>How far a finger must move while down before it counts as a drag.</summary>
        public float TouchDragThreshold { get; set; } = 12f;

        /// <summary>How close a second click must be to the first (mouse).</summary>
        public float MouseDoubleClickRadius { get; set; } = 8f;

        /// <summary>How close a second tap must be to the first (touch).</summary>
        public float TouchDoubleClickRadius { get; set; } = 24f;

        /// <summary>The longest gap between the two clicks of a double-click.</summary>
        public TimeSpan DoubleClickTime { get; set; } = TimeSpan.FromMilliseconds(350);

        /// <summary>How long the pointer must stay down and still for a long-press.</summary>
        public TimeSpan LongPressTime { get; set; } = TimeSpan.FromMilliseconds(500);

        public float DragThresholdFor(PointerSource source) => source == PointerSource.Touch ? TouchDragThreshold : MouseDragThreshold;

        public float DoubleClickRadiusFor(PointerSource source) => source == PointerSource.Touch ? TouchDoubleClickRadius : MouseDoubleClickRadius;
    }

    /// <summary>
    /// Recognizes click, double-click, drag and long-press from one pointer. Call <see cref="Update"/> once per frame, then
    /// read <see cref="Events"/> (everything that happened this frame, in order).
    /// <para/>
    /// Rules: a press that moves past the drag threshold becomes a drag (never a click); a press held still for the
    /// long-press time becomes a long-press (never a click); anything else is a click on release. A double-click needs two
    /// clicks within <see cref="PointerGestureOptions.DoubleClickTime"/> and the double-click radius; a drag or long-press
    /// in between breaks the sequence. A press and release inside one frame (a quick tap) is a click.
    /// </summary>
    public sealed class PointerGestureRecognizer
    {
        private readonly List<PointerGesture> events = new List<PointerGesture>();

        private bool isDown;
        private Vector2 startPosition;
        private Vector2 lastDragPosition;
        private TimeSpan pressTime;
        private bool dragging;
        private bool longPressed;
        private PointerSource source;

        private bool hasLastClick;
        private TimeSpan lastClickTime;
        private Vector2 lastClickPosition;

        public PointerGestureRecognizer(PointerGestureOptions options = null)
        {
            Options = options ?? new PointerGestureOptions();
        }

        public PointerGestureOptions Options { get; }

        /// <summary>The gestures recognized in the most recent <see cref="Update"/>, in order.</summary>
        public IReadOnlyList<PointerGesture> Events => events;

        /// <summary>True while the pointer is down and has not yet become a drag or long-press.</summary>
        public bool IsPressed => isDown && !dragging && !longPressed;

        /// <summary>True from the drag start until the drop.</summary>
        public bool IsDragging => dragging;

        /// <summary>Where the current or most recent press began.</summary>
        public Vector2 PressPosition => startPosition;

        /// <summary>
        /// Feeds one frame. <paramref name="down"/> is whether the pointer is down now; <paramref name="pressed"/> and
        /// <paramref name="released"/> are the edges (both true means a press and release within the frame).
        /// </summary>
        public void Update(TimeSpan now, Vector2 position, bool down, bool pressed, bool released, PointerSource pointerSource)
        {
            events.Clear();

            if (pressed)
            {
                isDown = true;
                dragging = false;
                longPressed = false;
                source = pointerSource;
                startPosition = position;
                lastDragPosition = position;
                pressTime = now;
            }
            else if (down && isDown)
            {
                if (!dragging && !longPressed)
                {
                    float threshold = Options.DragThresholdFor(source);
                    if (Vector2.Distance(startPosition, position) > threshold)
                    {
                        dragging = true;
                        hasLastClick = false;
                        events.Add(Make(PointerGestureKind.DragStart, position, position - startPosition, now));
                        lastDragPosition = position;
                    }
                    else if (now - pressTime >= Options.LongPressTime)
                    {
                        longPressed = true;
                        hasLastClick = false;
                        events.Add(Make(PointerGestureKind.LongPress, position, Vector2.Zero, now));
                    }
                }
                else if (dragging)
                {
                    Vector2 delta = position - lastDragPosition;
                    if (delta != Vector2.Zero)
                        events.Add(Make(PointerGestureKind.DragMove, position, delta, now));
                    lastDragPosition = position;
                }
            }

            if (released && isDown)
                Release(now, position);
        }

        /// <summary>Convenience for callers that only know whether the pointer is down: edges come from the previous call.</summary>
        public void Update(TimeSpan now, Vector2 position, bool down, PointerSource pointerSource)
        {
            bool pressed = down && !isDown;
            bool released = !down && isDown;
            Update(now, position, down, pressed, released, pointerSource);
        }

        /// <summary>
        /// Abandons whatever the pointer is doing (the window lost focus, a screen opened over the drag). A drag
        /// reports <see cref="PointerGestureKind.DragCancel"/>; a plain press is simply forgotten, with no click.
        /// </summary>
        public void Cancel(TimeSpan now, Vector2 position)
        {
            events.Clear();
            if (isDown && dragging)
                events.Add(Make(PointerGestureKind.DragCancel, position, Vector2.Zero, now));
            isDown = false;
            dragging = false;
            longPressed = false;
            hasLastClick = false;
        }

        private void Release(TimeSpan now, Vector2 position)
        {
            if (!dragging && !longPressed && Vector2.Distance(startPosition, position) > Options.DragThresholdFor(source))
            {
                // A quick swipe: down, far away and up again between two samples. It is a drag, not a click.
                dragging = true;
                events.Add(Make(PointerGestureKind.DragStart, position, position - startPosition, now));
                lastDragPosition = position;
            }

            if (dragging)
            {
                events.Add(Make(PointerGestureKind.DragEnd, position, Vector2.Zero, now));
                hasLastClick = false;
            }
            else if (!longPressed)
            {
                events.Add(Make(PointerGestureKind.Click, position, Vector2.Zero, now, 1));

                bool isDouble = hasLastClick
                    && now - lastClickTime <= Options.DoubleClickTime
                    && Vector2.Distance(lastClickPosition, position) <= Options.DoubleClickRadiusFor(source);
                if (isDouble)
                {
                    events.Add(Make(PointerGestureKind.DoubleClick, position, Vector2.Zero, now, 2));
                    hasLastClick = false; // a third click starts a new sequence
                }
                else
                {
                    hasLastClick = true;
                    lastClickTime = now;
                    lastClickPosition = position;
                }
            }
            else
            {
                hasLastClick = false;
            }

            isDown = false;
            dragging = false;
            longPressed = false;
        }

        private PointerGesture Make(PointerGestureKind kind, Vector2 position, Vector2 delta, TimeSpan now, int clickCount = 0) =>
            new PointerGesture(kind, position, startPosition, delta, now - pressTime, source, clickCount);
    }
}
