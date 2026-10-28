using ShinyShoe;

namespace MonsterTrainAccessibility.Module.Interop
{
    /// <summary>
    /// Input handed to the game the way its own UI hands it on: <see cref="UISignals.GameUITriggered"/>,
    /// the signal a mouse click on a widget raises. The game's <c>ScreenManager</c> resolves the control
    /// and offers it to every active screen in order (dialogs and navigation layers first), so the
    /// game's own handler runs, sound and all, exactly as for a press. The mapping is marked fake, as the
    /// game marks its own synthetic clicks. Activation goes through here rather than through a button's
    /// <c>onClick</c>: the game's buttons trigger from screen input (<c>TryTrigger</c>), and most carry
    /// no click listeners at all.
    /// </summary>
    internal static class GameInput
    {
        /// <summary>Submit on a widget: the press a keyboard or pad user makes on the selected control.</summary>
        public static void Submit(IGameUIComponent widget)
        {
            if (widget == null) return;
            UISignals.GameUITriggered.Dispatch(Fake(global::InputManager.Controls.Submit), widget);
        }

        /// <summary>The pointer coming onto a widget: what hover shows (a card's tooltips, a filter's name)
        /// the game shows, as for the mouse.</summary>
        public static void Hover(IGameUIComponent widget)
        {
            if (widget == null) return;
            UISignals.GameUITriggered.Dispatch(Fake(global::InputManager.Controls.PointerEnter), widget);
        }

        /// <summary>A control with no widget behind it (Escape, a screen's own hotkey).</summary>
        public static void Send(global::InputManager.Controls control)
            => UISignals.GameUITriggered.Dispatch(Fake(control), null);

        private static CoreInputControlMapping Fake(global::InputManager.Controls control)
            => new CoreInputControlMapping(control, InputType.None, fake: true);
    }
}
