using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>A menu-style screen: a <see cref="UiFocus"/> of widgets. Main menu, settings (and later the pause overlay) derive from it.</summary>
    public abstract class UiScreen
    {
        public UiFocus Focus { get; protected set; } = new UiFocus();
        protected Vector2 Mouse;
        public virtual void Update(UiInput input, float dt) { Mouse = input.Mouse; Focus.Update(input); }
        public virtual void Draw(UiRenderer r) => Focus.Draw(r, new Point((int)Mouse.X, (int)Mouse.Y));
    }
}
