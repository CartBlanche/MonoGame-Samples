using Microsoft.Xna.Framework.Input.Touch;

namespace CardsFramework.Tests
{
    /// <summary>One pointer from the mouse and the primary touch.</summary>
    public class PointerTrackerTests
    {
        private static readonly IReadOnlyList<TouchSample> NoTouches = Array.Empty<TouchSample>();

        private static TouchSample Touch(int id, float x, float y, TouchLocationState state) => new TouchSample(id, new Vector2(x, y), state);

        private static IReadOnlyList<TouchSample> Touches(params TouchSample[] samples) => samples;

        // ── Mouse ─────────────────────────────────────────────────────────

        [Fact]
        public void TheMouse_GivesPressAndReleaseEdgesWithTheirPositions()
        {
            var p = new PointerTracker();
            p.Update(new Vector2(10, 10), false, NoTouches);
            Assert.False(p.Down || p.Pressed || p.Released);

            p.Update(new Vector2(20, 20), true, NoTouches);
            Assert.True(p.Down && p.Pressed && !p.Released);
            Assert.Equal(new Vector2(20, 20), p.PressPosition);
            Assert.Equal(PointerSource.Mouse, p.Source);

            p.Update(new Vector2(25, 22), true, NoTouches);
            Assert.True(p.Down && !p.Pressed && !p.Released);   // held: no new edge
            Assert.Equal(new Vector2(25, 22), p.Position);

            p.Update(new Vector2(30, 24), false, NoTouches);
            Assert.True(!p.Down && !p.Pressed && p.Released);
            Assert.Equal(new Vector2(30, 24), p.ReleasePosition);
            Assert.Equal(new Vector2(20, 20), p.PressPosition);  // remembered
        }

        [Fact]
        public void TheMouse_HoversEveryFrameWithoutBeingDown()
        {
            var p = new PointerTracker();
            p.Update(new Vector2(1, 1), false, NoTouches);
            p.Update(new Vector2(200, 100), false, NoTouches);
            Assert.Equal(new Vector2(200, 100), p.Position);
            Assert.False(p.Down);
        }

        [Fact]
        public void WithNoMouseAndNoTouch_NothingIsDown_AndThePositionIsKept()
        {
            var p = new PointerTracker();
            p.Update(new Vector2(50, 60), false, NoTouches);
            p.Update(null, false, NoTouches);                    // a phone: no mouse
            Assert.False(p.Down);
            Assert.Equal(new Vector2(50, 60), p.Position);
        }

        // ── Touch ─────────────────────────────────────────────────────────

        [Fact]
        public void ATouch_GoesDown_FollowsTheFinger_AndComesUp()
        {
            var p = new PointerTracker();

            p.Update(null, false, Touches(Touch(1, 100, 100, TouchLocationState.Pressed)));
            Assert.True(p.Down && p.Pressed);
            Assert.Equal(PointerSource.Touch, p.Source);
            Assert.True(p.TouchActive);

            p.Update(null, false, Touches(Touch(1, 130, 110, TouchLocationState.Moved)));
            Assert.True(p.Down && !p.Pressed);
            Assert.Equal(new Vector2(130, 110), p.Position);       // unlike the cursor, the pointer follows a moving finger

            p.Update(null, false, Touches(Touch(1, 150, 115, TouchLocationState.Released)));
            Assert.True(!p.Down && p.Released);
            Assert.Equal(new Vector2(150, 115), p.ReleasePosition);
            Assert.False(p.TouchActive);
        }

        [Fact]
        public void ASecondFinger_CannotTakeOver()
        {
            var p = new PointerTracker();
            p.Update(null, false, Touches(Touch(1, 100, 100, TouchLocationState.Pressed)));
            p.Update(null, false, Touches(
                Touch(1, 110, 100, TouchLocationState.Moved),
                Touch(2, 400, 300, TouchLocationState.Pressed)));
            Assert.Equal(new Vector2(110, 100), p.Position);        // still the first finger
            Assert.False(p.Pressed);

            p.Update(null, false, Touches(
                Touch(1, 115, 100, TouchLocationState.Moved),
                Touch(2, 410, 300, TouchLocationState.Moved)));
            Assert.Equal(new Vector2(115, 100), p.Position);
        }

        [Fact]
        public void WhenTheFirstFingerLifts_TheNextNewTouchBecomesThePrimary()
        {
            var p = new PointerTracker();
            p.Update(null, false, Touches(Touch(1, 100, 100, TouchLocationState.Pressed)));
            p.Update(null, false, Touches(Touch(1, 100, 100, TouchLocationState.Released), Touch(2, 400, 300, TouchLocationState.Moved)));
            Assert.True(p.Released);

            // Finger 2 was already down, so it has no Pressed entry and is not adopted; a brand-new touch is.
            p.Update(null, false, Touches(Touch(3, 50, 60, TouchLocationState.Pressed)));
            Assert.True(p.Pressed);
            Assert.Equal(new Vector2(50, 60), p.Position);
        }

        [Fact]
        public void APressAndReleaseInOneFrame_GivesBothEdges()
        {
            var p = new PointerTracker();
            p.Update(null, false, Touches(
                Touch(1, 70, 80, TouchLocationState.Pressed),
                Touch(1, 70, 80, TouchLocationState.Released)));
            Assert.True(p.Pressed && p.Released);
            Assert.False(p.Down);
            Assert.Equal(new Vector2(70, 80), p.PressPosition);
            Assert.Equal(new Vector2(70, 80), p.ReleasePosition);
            Assert.False(p.TouchActive);
        }

        [Fact]
        public void AFingerThatVanishesWithoutAReleasedEntry_CountsAsLifted()
        {
            var p = new PointerTracker();
            p.Update(null, false, Touches(Touch(1, 100, 100, TouchLocationState.Pressed)));
            p.Update(null, false, NoTouches);                        // cancelled: the contact is simply gone
            Assert.True(p.Released);
            Assert.False(p.Down);
            Assert.Equal(new Vector2(100, 100), p.ReleasePosition);
            Assert.False(p.TouchActive);
        }

        // ── Both devices ──────────────────────────────────────────────────

        [Fact]
        public void ATouch_WinsOverTheMouse_AndLiftingTheFingerIsNotAMouseClick()
        {
            var p = new PointerTracker();
            p.Update(new Vector2(300, 300), true, NoTouches);        // the mouse button is held...
            p.Update(new Vector2(300, 300), true, NoTouches);
            p.Update(new Vector2(300, 300), true, Touches(Touch(1, 10, 10, TouchLocationState.Pressed)));
            Assert.Equal(new Vector2(10, 10), p.Position);           // ...but the finger is the pointer
            Assert.Equal(PointerSource.Touch, p.Source);

            p.Update(new Vector2(300, 300), true, Touches(Touch(1, 10, 10, TouchLocationState.Released)));
            Assert.True(p.Released);

            // Next frame: only the mouse. It is still held, but it was already down: no fresh press.
            p.Update(new Vector2(300, 300), true, NoTouches);
            Assert.False(p.Pressed);
            Assert.Equal(PointerSource.Mouse, p.Source);
            Assert.True(p.Down);
        }

        [Fact]
        public void TheMouse_WorksNormallyAfterATouch()
        {
            var p = new PointerTracker();
            p.Update(new Vector2(5, 5), false, Touches(Touch(1, 10, 10, TouchLocationState.Pressed)));
            p.Update(new Vector2(5, 5), false, Touches(Touch(1, 10, 10, TouchLocationState.Released)));
            p.Update(new Vector2(40, 40), false, NoTouches);
            Assert.Equal(new Vector2(40, 40), p.Position);
            p.Update(new Vector2(40, 40), true, NoTouches);
            Assert.True(p.Pressed);
        }

        // ── With the recognizer ───────────────────────────────────────────

        [Fact]
        public void ATouchDrag_BecomesDragStartMoveEnd()
        {
            var tracker = new PointerTracker();
            var recognizer = new PointerGestureRecognizer();
            var kinds = new List<PointerGestureKind>();
            int ms = 0;

            void Frame(params TouchSample[] touches)
            {
                tracker.Update(null, false, touches);
                recognizer.Update(TimeSpan.FromMilliseconds(ms += 16), tracker.Position, tracker.Down, tracker.Pressed, tracker.Released, tracker.Source);
                kinds.AddRange(recognizer.Events.Select(e => e.Kind));
            }

            Frame(Touch(1, 100, 100, TouchLocationState.Pressed));
            Frame(Touch(1, 105, 100, TouchLocationState.Moved));    // inside the touch threshold
            Frame(Touch(1, 130, 100, TouchLocationState.Moved));    // past it
            Frame(Touch(1, 160, 100, TouchLocationState.Moved));
            Frame(Touch(1, 160, 100, TouchLocationState.Released));

            Assert.Equal(new[] { PointerGestureKind.DragStart, PointerGestureKind.DragMove, PointerGestureKind.DragEnd }, kinds.ToArray());
        }

        [Fact]
        public void ATouchTap_BecomesAClick_AndTwoTapsADoubleClick()
        {
            var tracker = new PointerTracker();
            var recognizer = new PointerGestureRecognizer();
            var kinds = new List<PointerGestureKind>();
            int ms = 0;

            void Frame(params TouchSample[] touches)
            {
                tracker.Update(null, false, touches);
                recognizer.Update(TimeSpan.FromMilliseconds(ms += 50), tracker.Position, tracker.Down, tracker.Pressed, tracker.Released, tracker.Source);
                kinds.AddRange(recognizer.Events.Select(e => e.Kind));
            }

            Frame(Touch(1, 100, 100, TouchLocationState.Pressed));
            Frame(Touch(1, 101, 100, TouchLocationState.Released));
            Frame();
            Frame(Touch(2, 102, 101, TouchLocationState.Pressed));
            Frame(Touch(2, 102, 101, TouchLocationState.Released));

            Assert.Equal(new[] { PointerGestureKind.Click, PointerGestureKind.Click, PointerGestureKind.DoubleClick }, kinds.ToArray());
        }
    }
}
