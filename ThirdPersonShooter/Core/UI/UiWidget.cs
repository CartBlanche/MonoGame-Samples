using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>Visual state a widget is drawn in; computed by <see cref="UiFocus"/>.</summary>
    public struct UiState { public bool Focused, Hover, Pressed; }

    /// <summary>Base of all UI kit widgets. Bounds are in the virtual 1920x1080 UI space. No GL objects live here, so navigation is testable headlessly.</summary>
    public abstract class UiWidget
    {
        public Rectangle Bounds;
        public bool Enabled = true, Visible = true;
        /// <summary>Can take keyboard/gamepad focus (labels, panels, images: no).</summary>
        public virtual bool Focusable => true;
        /// <summary>Left/Right change the widget's value (radio row, slider) instead of moving focus.</summary>
        public virtual bool HandlesHorizontal => false;
        public bool CanFocus => Focusable && Enabled && Visible;
        public Point Center => new Point(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);

        public virtual void Activate() { }                  // Enter / A / click
        public virtual void Adjust(int dir) { }              // Left (-1) / Right (+1) when HandlesHorizontal
        public virtual void MouseDown(Point p) { }           // press inside the widget
        public virtual void MouseDrag(Point p) { }           // held while the press started on this widget
        public abstract void Draw(UiRenderer r, UiState s);
    }

    /// <summary>Static text (Godot Label: white, PT Sans Bold).</summary>
    public sealed class UiLabel : UiWidget
    {
        public string Text; public SpriteFont Font; public Color Color = UiTheme.Text; public bool AlignRight, AlignCentre; // AlignCentre: Bounds.X is the centre
        public UiLabel(string text, Vector2 pos, SpriteFont font = null) { Text = text; Font = font ?? UiTheme.Font30; Bounds = new Rectangle((int)pos.X, (int)pos.Y, 0, 0); }
        public override bool Focusable => false;
        public override void Draw(UiRenderer r, UiState s) { if (Visible) r.Text(Font, Text, new Vector2(AlignRight ? Bounds.X - Font.MeasureString(Text).X : AlignCentre ? Bounds.X - Font.MeasureString(Text).X * 0.5f : Bounds.X, Bounds.Y), Color); }
    }

    /// <summary>A slanted panel (backdrop for groups / modal dialogs such as the future pause overlay).</summary>
    public sealed class UiPanel : UiWidget
    {
        public Color Fill = UiTheme.PressedFill; public Color Border = UiTheme.NormalBorder; public int BorderWidth = UiTheme.NormalBorderW;
        public UiPanel(Rectangle bounds) { Bounds = bounds; }
        public override bool Focusable => false;
        public override void Draw(UiRenderer r, UiState s) { if (Visible) r.Slanted(Bounds, Fill, BorderWidth, Border); }
    }

    /// <summary>A texture drawn at its position (title logo).</summary>
    public sealed class UiImage : UiWidget
    {
        public Texture2D Texture;
        public UiImage(Texture2D t, Point pos) { Texture = t; Bounds = new Rectangle(pos.X, pos.Y, t?.Width ?? 0, t?.Height ?? 0); }
        public override bool Focusable => false;
        public override void Draw(UiRenderer r, UiState s) { if (Visible && Texture != null) r.Image(Texture, new Vector2(Bounds.X, Bounds.Y), Color.White); }
    }
}
