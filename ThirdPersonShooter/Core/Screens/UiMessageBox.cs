using System;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    public enum MessageBoxResult { Left, Right }

    /// <summary>
    /// Reusable modal message box (pause "Leave?" and quit "Quit?"): a title and two slanted buttons on a dimmed full-screen backdrop, drawn over whatever the
    /// caller drew first. <paramref name="defaultButton"/> is the initially focused button; Esc / pad B always means the LEFT button (the safe choice),
    /// whichever is the default. Left/Right (A/D, d-pad, stick) move focus, Enter/Space/A confirm, mouse hover/click work through <see cref="UiFocus"/>.
    /// The result callback fires exactly once. Logic only (no GL) so it is testable headlessly; <see cref="Layout"/> centres it for any back-buffer size.
    /// </summary>
    public sealed class UiMessageBox : UiScreen
    {
        public readonly UiButton LeftButton, RightButton;
        public int Dispatched { get; private set; }
        public MessageBoxResult? Result { get; private set; }
        readonly UiPanel _panel; readonly UiLabel _title; readonly Action<MessageBoxResult> _result;
        const int PanelW = 860, PanelH = 260, Gap = 60;

        public UiMessageBox(string title, string left, string right, MessageBoxResult defaultButton, Action<MessageBoxResult> result)
        {
            _result = result;
            _panel = Focus.Add(new UiPanel(new Rectangle(0, 0, PanelW, PanelH)) { Fill = UiTheme.C(0f, 0f, 0f, 0.78f), Border = UiTheme.HoverBorder, BorderWidth = UiTheme.HoverBorderW });
            _title = Focus.Add(new UiLabel(title, Vector2.Zero, UiTheme.Font44) { AlignCentre = true });
            LeftButton = Focus.Add(new UiButton(left, 0, 0, clicked: () => Finish(MessageBoxResult.Left)));
            RightButton = Focus.Add(new UiButton(right, 0, 0, clicked: () => Finish(MessageBoxResult.Right)));
            Focus.BackPressed = () => Finish(MessageBoxResult.Left);
            Focus.SetFocus(defaultButton == MessageBoxResult.Left ? LeftButton : RightButton);
            LayoutVirtual(UiTheme.VirtualW, UiTheme.VirtualH);
        }

        public bool DefaultIsFocused(MessageBoxResult b) => Focus.Focused == (b == MessageBoxResult.Left ? LeftButton : RightButton);

        void Finish(MessageBoxResult r)
        {
            if (Result.HasValue) return;
            Result = r; Dispatched++; _result?.Invoke(r);
        }

        /// <summary>Centre the box on a back buffer of w x h pixels (the UI space is scaled by min(w/1920, h/1080), anchored top-left).</summary>
        public void Layout(int w, int h) { float s = UiRenderer.ScaleFor(w, h); LayoutVirtual(w / s, h / s); }

        void LayoutVirtual(float vw, float vh)
        {
            int x = (int)(vw / 2f) - PanelW / 2, y = (int)(vh / 2f) - PanelH / 2;
            _panel.Bounds = new Rectangle(x, y, PanelW, PanelH);
            _title.Bounds = new Rectangle(x + PanelW / 2, y + 45, 0, 0);
            int bx = x + (PanelW - (2 * UiTheme.ButtonW + Gap)) / 2, by = y + 150;
            LeftButton.Bounds = new Rectangle(bx, by, UiTheme.ButtonW, UiTheme.ButtonH);
            RightButton.Bounds = new Rectangle(bx + UiTheme.ButtonW + Gap, by, UiTheme.ButtonW, UiTheme.ButtonH);
        }

        public override void Draw(UiRenderer r)
        {
            r.Rect(-200, -200, 9000, 9000, UiTheme.Dim); // backdrop dim, covers any aspect ratio
            base.Draw(r);
        }
    }
}
