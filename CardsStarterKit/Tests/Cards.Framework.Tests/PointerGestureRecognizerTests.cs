namespace CardsFramework.Tests
{
    /// <summary>Click, double-click, drag and long-press, from scripted pointer sequences on a fake clock.</summary>
    public class PointerGestureRecognizerTests
    {
        private static TimeSpan T(int ms) => TimeSpan.FromMilliseconds(ms);

        /// <summary>A scripted pointer: each call is one frame.</summary>
        private sealed class Script
        {
            public readonly PointerGestureRecognizer R;
            private readonly PointerSource source;
            public Script(PointerSource source = PointerSource.Mouse, PointerGestureOptions options = null)
            {
                R = new PointerGestureRecognizer(options);
                this.source = source;
            }

            public IReadOnlyList<PointerGesture> Down(int ms, float x, float y) { R.Update(T(ms), new Vector2(x, y), true, source); return R.Events; }
            public IReadOnlyList<PointerGesture> Hold(int ms, float x, float y) { R.Update(T(ms), new Vector2(x, y), true, source); return R.Events; }
            public IReadOnlyList<PointerGesture> Up(int ms, float x, float y) { R.Update(T(ms), new Vector2(x, y), false, source); return R.Events; }
            public IReadOnlyList<PointerGesture> Idle(int ms, float x, float y) { R.Update(T(ms), new Vector2(x, y), false, source); return R.Events; }

            /// <summary>Press, wait, release in place: one click.</summary>
            public List<PointerGestureKind> Click(int startMs, float x, float y, int heldMs = 60)
            {
                var kinds = new List<PointerGestureKind>();
                kinds.AddRange(Down(startMs, x, y).Select(e => e.Kind));
                kinds.AddRange(Up(startMs + heldMs, x, y).Select(e => e.Kind));
                return kinds;
            }
        }

        private static List<PointerGestureKind> Kinds(IEnumerable<PointerGesture> events) => events.Select(e => e.Kind).ToList();

        // ── Click ─────────────────────────────────────────────────────────

        [Fact]
        public void PressAndRelease_InPlace_IsAClick()
        {
            var s = new Script();
            Assert.Empty(s.Down(0, 100, 100));
            var up = s.Up(80, 100, 100);
            var click = Assert.Single(up);
            Assert.Equal(PointerGestureKind.Click, click.Kind);
            Assert.Equal(1, click.ClickCount);
            Assert.Equal(new Vector2(100, 100), click.Position);
            Assert.Equal(T(80), click.HeldFor);
        }

        [Fact]
        public void ASmallWobble_IsStillAClick()
        {
            var s = new Script();
            s.Down(0, 100, 100);
            Assert.Empty(s.Hold(30, 103, 102));         // well under the 6 unit mouse threshold
            Assert.Equal(PointerGestureKind.Click, Assert.Single(s.Up(60, 104, 102)).Kind);
        }

        [Fact]
        public void APressAndReleaseInOneFrame_IsAClick()
        {
            var r = new PointerGestureRecognizer();
            r.Update(T(0), new Vector2(50, 50), down: false, pressed: true, released: true, PointerSource.Touch);
            Assert.Equal(PointerGestureKind.Click, Assert.Single(r.Events).Kind);
        }

        [Fact]
        public void EventsAreClearedEveryFrame()
        {
            var s = new Script();
            s.Down(0, 10, 10);
            Assert.Single(s.Up(50, 10, 10));
            Assert.Empty(s.Idle(66, 10, 10));
            Assert.Empty(s.Idle(83, 10, 10));
        }

        [Fact]
        public void ReleasingWithNothingPressed_DoesNothing()
        {
            var s = new Script();
            Assert.Empty(s.Idle(0, 5, 5));
            Assert.Empty(s.Up(16, 5, 5));
        }

        // ── Drag ──────────────────────────────────────────────────────────

        [Fact]
        public void MovingPastTheThreshold_IsADrag_NeverAClick()
        {
            var s = new Script();
            s.Down(0, 100, 100);

            var start = Assert.Single(s.Hold(16, 120, 100));     // 20 units: past the threshold
            Assert.Equal(PointerGestureKind.DragStart, start.Kind);
            Assert.Equal(new Vector2(100, 100), start.StartPosition);
            Assert.Equal(new Vector2(120, 100), start.Position);
            Assert.Equal(new Vector2(20, 0), start.Delta);
            Assert.True(s.R.IsDragging);

            var move = Assert.Single(s.Hold(32, 130, 105));
            Assert.Equal(PointerGestureKind.DragMove, move.Kind);
            Assert.Equal(new Vector2(10, 5), move.Delta);           // since the last drag event, not since the start
            Assert.Equal(new Vector2(100, 100), move.StartPosition);

            var end = Assert.Single(s.Up(48, 140, 105));
            Assert.Equal(PointerGestureKind.DragEnd, end.Kind);
            Assert.Equal(new Vector2(140, 105), end.Position);
            Assert.False(s.R.IsDragging);
        }

        [Fact]
        public void AStationaryFrameDuringADrag_ReportsNoMove()
        {
            var s = new Script();
            s.Down(0, 0, 0);
            s.Hold(16, 30, 0);
            Assert.Empty(s.Hold(32, 30, 0));
        }

        [Fact]
        public void ADragThatReturnsToTheStart_IsStillADrag()
        {
            var s = new Script();
            s.Down(0, 100, 100);
            s.Hold(16, 130, 100);
            s.Hold(32, 100, 100);
            var kinds = Kinds(s.Up(48, 100, 100));
            Assert.Equal(new[] { PointerGestureKind.DragEnd }, kinds);   // once dragging, always dragging until the drop
        }

        [Fact]
        public void TheThreshold_IsBiggerForAFinger()
        {
            // 8 units: a mouse drag (threshold 6), but a finger's wobble (threshold 12).
            var mouse = new Script(PointerSource.Mouse);
            mouse.Down(0, 0, 0);
            Assert.Equal(PointerGestureKind.DragStart, Assert.Single(mouse.Hold(16, 8, 0)).Kind);

            var touch = new Script(PointerSource.Touch);
            touch.Down(0, 0, 0);
            Assert.Empty(touch.Hold(16, 8, 0));
            Assert.Equal(PointerGestureKind.Click, Assert.Single(touch.Up(32, 8, 0)).Kind);
        }

        [Fact]
        public void AQuickSwipe_BetweenTwoSamples_IsADragNotAClick()
        {
            var s = new Script(PointerSource.Touch);
            s.Down(0, 100, 100);
            var kinds = Kinds(s.Up(16, 300, 100));       // down, far away, up: one frame
            Assert.Equal(new[] { PointerGestureKind.DragStart, PointerGestureKind.DragEnd }, kinds);
        }

        [Fact]
        public void TheThresholdsCanBeTuned()
        {
            var s = new Script(PointerSource.Mouse, new PointerGestureOptions { MouseDragThreshold = 30f });
            s.Down(0, 0, 0);
            Assert.Empty(s.Hold(16, 25, 0));
            Assert.Single(s.Hold(32, 35, 0));
        }

        // ── Long press ────────────────────────────────────────────────────

        [Fact]
        public void HoldingStill_IsALongPress_Once_AndNoClickFollows()
        {
            var s = new Script();
            s.Down(0, 100, 100);
            Assert.Empty(s.Hold(400, 100, 100));
            var press = Assert.Single(s.Hold(520, 100, 100));
            Assert.Equal(PointerGestureKind.LongPress, press.Kind);
            Assert.Equal(T(520), press.HeldFor);
            Assert.Empty(s.Hold(700, 100, 100));          // only once
            Assert.Empty(s.Up(800, 100, 100));            // releasing after a long press is not a click
        }

        [Fact]
        public void MovingBeforeTheLongPressTime_MakesItADragInstead()
        {
            var s = new Script();
            s.Down(0, 0, 0);
            Assert.Equal(PointerGestureKind.DragStart, Assert.Single(s.Hold(300, 40, 0)).Kind);
            Assert.Empty(s.Hold(900, 40, 0));             // no long press while dragging
        }

        [Fact]
        public void AWobbleDuringALongPress_DoesNotCancelIt()
        {
            var s = new Script(PointerSource.Touch);
            s.Down(0, 100, 100);
            s.Hold(200, 105, 103);
            Assert.Equal(PointerGestureKind.LongPress, Assert.Single(s.Hold(550, 106, 104)).Kind);
        }

        // ── Double click ──────────────────────────────────────────────────

        [Fact]
        public void TwoQuickClicksInTheSameSpot_AreADoubleClick()
        {
            var s = new Script();
            Assert.Equal(new[] { PointerGestureKind.Click }, s.Click(0, 100, 100).ToArray());
            var second = s.Click(200, 101, 100);
            Assert.Equal(new[] { PointerGestureKind.Click, PointerGestureKind.DoubleClick }, second.ToArray());
        }

        [Fact]
        public void TheDoubleClickEvent_CarriesTheSecondClickDetails()
        {
            var s = new Script();
            s.Click(0, 100, 100);
            s.Down(150, 102, 101);
            var up = s.Up(210, 102, 101);
            var dbl = up.Single(e => e.Kind == PointerGestureKind.DoubleClick);
            Assert.Equal(2, dbl.ClickCount);
            Assert.Equal(new Vector2(102, 101), dbl.Position);
            Assert.Equal(T(60), dbl.HeldFor);
        }

        [Fact]
        public void ATripleClick_IsOneDoubleClickThenANewSequence()
        {
            var s = new Script();
            s.Click(0, 100, 100);
            Assert.Contains(PointerGestureKind.DoubleClick, s.Click(150, 100, 100));
            Assert.DoesNotContain(PointerGestureKind.DoubleClick, s.Click(300, 100, 100));   // the third starts afresh
            Assert.Contains(PointerGestureKind.DoubleClick, s.Click(450, 100, 100));         // and the fourth completes that pair
        }

        [Fact]
        public void ASlowSecondClick_IsNotADoubleClick()
        {
            var s = new Script();
            s.Click(0, 100, 100);
            Assert.DoesNotContain(PointerGestureKind.DoubleClick, s.Click(700, 100, 100));
        }

        [Fact]
        public void ASecondClickFarAway_IsNotADoubleClick()
        {
            var s = new Script();
            s.Click(0, 100, 100);
            Assert.DoesNotContain(PointerGestureKind.DoubleClick, s.Click(150, 140, 100));
        }

        [Fact]
        public void ATapNeedsLessPrecisionThanAClick()
        {
            // 15 units apart: too far for a mouse double-click (radius 8) but fine for a double tap (radius 24).
            var mouse = new Script(PointerSource.Mouse);
            mouse.Click(0, 100, 100);
            Assert.DoesNotContain(PointerGestureKind.DoubleClick, mouse.Click(150, 115, 100));

            var touch = new Script(PointerSource.Touch);
            touch.Click(0, 100, 100);
            Assert.Contains(PointerGestureKind.DoubleClick, touch.Click(150, 115, 100));
        }

        [Fact]
        public void ADragBetweenTwoClicks_BreaksTheDoubleClick()
        {
            var s = new Script();
            s.Click(0, 100, 100);
            s.Down(80, 100, 100); s.Hold(96, 150, 100); s.Up(112, 150, 100);          // a drag
            Assert.DoesNotContain(PointerGestureKind.DoubleClick, s.Click(200, 100, 100));
        }

        [Fact]
        public void ALongPressBetweenTwoClicks_BreaksTheDoubleClick()
        {
            var s = new Script();
            s.Click(0, 100, 100);
            s.Down(100, 100, 100); s.Hold(700, 100, 100); s.Up(720, 100, 100);        // a long press
            Assert.DoesNotContain(PointerGestureKind.DoubleClick, s.Click(740, 100, 100));
        }

        [Fact]
        public void TheDoubleClickWindow_CanBeTuned()
        {
            var s = new Script(PointerSource.Mouse, new PointerGestureOptions { DoubleClickTime = T(800) });
            s.Click(0, 100, 100);
            Assert.Contains(PointerGestureKind.DoubleClick, s.Click(700, 100, 100));
        }

        // ── Cancel ────────────────────────────────────────────────────────

        [Fact]
        public void CancellingADrag_ReportsItOnce_AndTheLaterReleaseIsSilent()
        {
            var s = new Script();
            s.Down(0, 0, 0);
            s.Hold(16, 40, 0);
            s.R.Cancel(T(32), new Vector2(40, 0));
            Assert.Equal(PointerGestureKind.DragCancel, Assert.Single(s.R.Events).Kind);
            Assert.False(s.R.IsDragging);
            Assert.Empty(s.Up(48, 40, 0));       // no drop and no click after a cancel
        }

        [Fact]
        public void CancellingAPlainPress_ProducesNoClick()
        {
            var s = new Script();
            s.Down(0, 10, 10);
            s.R.Cancel(T(20), new Vector2(10, 10));
            Assert.Empty(s.R.Events);
            Assert.Empty(s.Up(40, 10, 10));
        }

        [Fact]
        public void CancellingBreaksADoubleClickInProgress()
        {
            var s = new Script();
            s.Click(0, 100, 100);
            s.R.Cancel(T(100), new Vector2(100, 100));
            Assert.DoesNotContain(PointerGestureKind.DoubleClick, s.Click(150, 100, 100));
        }

        // ── State ─────────────────────────────────────────────────────────

        [Fact]
        public void TheStateProperties_FollowThePress()
        {
            var s = new Script();
            Assert.False(s.R.IsPressed);
            s.Down(0, 20, 30);
            Assert.True(s.R.IsPressed);
            Assert.Equal(new Vector2(20, 30), s.R.PressPosition);
            s.Hold(16, 60, 30);
            Assert.False(s.R.IsPressed);             // now a drag
            Assert.True(s.R.IsDragging);
            s.Up(32, 60, 30);
            Assert.False(s.R.IsPressed || s.R.IsDragging);
        }

        [Fact]
        public void EveryEventCarriesTheSourceAndTheStartPosition()
        {
            var s = new Script(PointerSource.Touch);
            s.Down(0, 11, 22);
            var e = Assert.Single(s.Hold(16, 80, 22));
            Assert.Equal(PointerSource.Touch, e.Source);
            Assert.Equal(new Vector2(11, 22), e.StartPosition);
        }

        [Fact]
        public void ADifferentSourceOnTheNextPress_UsesItsOwnThresholds()
        {
            var r = new PointerGestureRecognizer();
            r.Update(T(0), Vector2.Zero, true, PointerSource.Mouse);
            r.Update(T(10), Vector2.Zero, false, PointerSource.Mouse);
            r.Update(T(1000), Vector2.Zero, true, PointerSource.Touch);
            r.Update(T(1016), new Vector2(8, 0), true, PointerSource.Touch);
            Assert.Empty(r.Events);                  // 8 units is not a drag for a finger
        }
    }
}
