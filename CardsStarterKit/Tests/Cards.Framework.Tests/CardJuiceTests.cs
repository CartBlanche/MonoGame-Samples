namespace CardsFramework.Tests
{
    public class CardJuiceTests
    {
        private static void Run(CardJuice j, float seconds, float dt = 1f / 60f)
        {
            for (float t = 0; t < seconds; t += dt) j.Update(dt);
        }

        [Fact]
        public void Spring_ConvergesToTarget()
        {
            var s = new Spring(200f, 20f) { Target = 1f };
            for (int i = 0; i < 300; i++) s.Update(1f / 60f);
            Assert.True(s.IsAtRest());
            Assert.InRange(s.Value, 0.999f, 1.001f);
        }

        [Fact]
        public void Spring_UnderdampedOvershoots_ButStaysBounded()
        {
            var s = new Spring(170f, 7f) { Target = 1f };
            float max = 0f;
            for (int i = 0; i < 300; i++) { s.Update(1f / 60f); max = Math.Max(max, s.Value); }
            Assert.True(max > 1.05f);
            Assert.True(max < 2f);
        }

        [Fact]
        public void Spring_LargeFrameTimeDoesNotBlowUp()
        {
            var s = new Spring(400f, 10f) { Target = 1f };
            for (int i = 0; i < 20; i++) s.Update(0.5f);
            Assert.True(float.IsFinite(s.Value));
            Assert.InRange(s.Value, -2f, 3f);
        }

        [Fact]
        public void Grab_LiftsCardAndGrowsShadow()
        {
            var j = new CardJuice();
            Vector2 restShadow = j.GetShadowOffset();
            j.Grab();
            Run(j, 1f);
            Assert.InRange(j.Lift, 0.95f, 1.05f);
            Assert.True(j.PositionOffset.Y < 0f);
            Assert.True(j.GetShadowOffset().Y > restShadow.Y);
        }

        [Fact]
        public void Drag_LeansTheWayItIsDragged_ThenSwingsBack()
        {
            var j = new CardJuice();
            j.Grab();
            j.Drag(new Vector2(600f, 0f));
            Run(j, 0.5f);
            Assert.True(j.Rotation > 0.05f);

            j.StopDrag();
            float min = j.Rotation;
            for (int i = 0; i < 120; i++) { j.Update(1f / 60f); min = Math.Min(min, j.Rotation); }
            Assert.True(min < -0.005f, "should swing past upright");
            Assert.InRange(j.Rotation, -0.01f, 0.01f);
        }

        [Fact]
        public void Release_SquashesThenSettles()
        {
            var j = new CardJuice();
            j.Grab();
            Run(j, 0.5f);
            j.Release();
            Assert.True(j.Squash < 1f);
            Run(j, 1f);
            Assert.InRange(j.Squash, 0.999f, 1.001f);
            Assert.InRange(j.Lift, -0.001f, 0.001f);
        }

        [Fact]
        public void Hover_LiftsPartly_AndLeansTowardPointer()
        {
            var j = new CardJuice();
            j.SetHover(new Vector2(0.8f, -0.4f));
            Run(j, 1f);
            Assert.InRange(j.Lift, 0.2f, 0.5f);
            Assert.True(j.TiltX > 0.5f);
            j.SetHover(null);
            Run(j, 1f);
            Assert.True(j.Lift < 0.01f);
            Assert.False(j.IsActive);
        }
    }
}
