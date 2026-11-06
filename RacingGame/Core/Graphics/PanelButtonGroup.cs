using Microsoft.Xna.Framework;
using RacingGame.GameLogic;

namespace RacingGame.Graphics
{
    /// <summary>
    /// A set of panel buttons with one selected, as on the Start Race screen, the dialogs and the
    /// multiplayer browser: the keyboard and gamepad move the selection (see <see cref="Move"/> and
    /// <see cref="Select"/>), the mouse moves it only once the mouse has moved (so it does not fight
    /// the keyboard), and a click or tap answers right away. The screen decides where the buttons
    /// are and what Enter does with <see cref="Selection"/>.
    /// </summary>
    public class PanelButtonGroup
    {
        bool ignoreMouse = true;

        /// <summary>
        /// The selected button
        /// </summary>
        public int Selection;

        public PanelButtonGroup(int selection = 0)
        {
            Selection = selection;
        }

        /// <summary>
        /// Keyboard or gamepad: move the selection by delta (wraps around)
        /// </summary>
        public void Move(int delta, int count)
        {
            Selection = ((Selection + delta) % count + count) % count;
            ignoreMouse = true;
        }

        /// <summary>
        /// Keyboard or gamepad: select this button
        /// </summary>
        public void Select(int index)
        {
            Selection = index;
            ignoreMouse = true;
        }

        /// <summary>
        /// Draw the buttons. Returns the index of the button that was clicked (or tapped) this
        /// frame, else -1.
        /// </summary>
        /// <param name="rects">Where each button is</param>
        /// <param name="labels">The text of each button</param>
        /// <param name="textScale">Text size</param>
        /// <param name="labelColors">Optional label colours (null entries: the default for the state)</param>
        /// <param name="enabled">False: draw without a selection and ignore the mouse (e.g. while busy)</param>
        public int Render(Rectangle[] rects, string[] labels, float textScale,
            Color?[] labelColors = null, bool enabled = true)
        {
            int hover = -1;
            if (enabled)
                for (int i = 0; i < rects.Length; i++)
                    if (Input.MouseInBox(rects[i]))
                        hover = i;

            if (Input.HasMouseMoved || Input.MouseLeftButtonJustPressed)
                ignoreMouse = false;
            if (!ignoreMouse && hover >= 0)
                Selection = hover;

            for (int i = 0; i < rects.Length; i++)
                BaseGame.UI.RenderPanelButton(rects[i], labels[i], enabled && i == Selection, textScale,
                    labelColors == null ? null : labelColors[i]);

            if (hover >= 0 && Input.MouseLeftButtonJustPressed)
            {
                Selection = hover;
                return hover;
            }
            return -1;
        }
    }
}
