using System;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Screens;

namespace MonsterTrainAccessibility.Core.UI
{
    /// <summary>
    /// The navigation contract <see cref="Navigation"/> drives: bind to a screen, consume input, keep
    /// focus established, and announce focus changes. The active implementation is
    /// <see cref="GraphNavigator"/>, where announcements are PULL-based: focus is diffed per frame and a
    /// change speaks exactly once no matter what caused it, so implementations and screens never make
    /// per-callsite announce decisions.
    /// </summary>
    public abstract class Navigator
    {
        protected Screen Screen { get; set; }

        /// <summary>The screen this navigator is bound to.</summary>
        public Screen BoundScreen => Screen;

        /// <summary>True when the navigator owns the keys (something is focused): false in the unfocused
        /// state of a screen that starts unfocused.</summary>
        public abstract bool HasFocus { get; }

        /// <summary>Bind to a screen. Re-attaching the SAME screen means "content changed" (focus and
        /// announce memory survive); a new screen resets both.</summary>
        public abstract void Attach(Screen screen);

        /// <summary>Drop focus back to the screen's unfocused state, the same place Tab-off-the-end
        /// lands. On a <see cref="Screen.StartUnfocused"/> screen the keyboard returns to the game and
        /// stays there; on other screens focus re-establishes next frame.</summary>
        public abstract void Blur();

        /// <summary>The per-frame pull, called after the focused screen updates: (re)establish focus when
        /// the screen has focusable content, and announce any focus change exactly once.</summary>
        public abstract void EnsureFocus();

        public abstract bool OnInputJustPressed(InputAction action);

        /// <summary>Whether <see cref="OnInputJustPressed"/> would do something with the action right
        /// now, without doing it: the key help lists a UI key only where it answers.</summary>
        public virtual bool WouldHandle(InputAction action) => false;

        /// <summary>Take the next landing as spoken: the key help closes to run an action and reads
        /// the focus itself, after the action, instead of the landing a closing overlay brings.</summary>
        public virtual void QuietNextLanding() { }
        public virtual bool OnInputHeld(InputAction action) => false;
        public virtual bool OnInputReleased(InputAction action) => false;

        /// <summary>Per-frame hook for typed-character input (type-ahead search).</summary>
        public virtual void TickTypeahead() { }

        /// <summary>Announce the current focus in full (the container hierarchy down to the element),
        /// e.g. when focus mode engages.</summary>
        /// <summary>Read the focus in full; <paramref name="interrupt"/> for a focus the player moved
        /// (the screen answered a key), queued otherwise.</summary>
        public abstract void AnnounceCurrent(bool interrupt = false);

        /// <summary>A screen closed (stack pop without <see cref="Screen.KeepStateOnPop"/>, or a child
        /// page removed): drop its per-screen state so reopening starts fresh.</summary>
        public virtual void ScreenClosed(Screen screen) { }

        /// <summary>Move focus to a graph node by id, applied when the node exists in a render, with one
        /// retry frame for content that appears mid-build.</summary>
        public virtual void FocusNode(Graph.ControlId id, bool announce = true) { }

        /// <summary>Move focus to the FIRST node of a Tab-stop.</summary>
        public virtual void FocusStop(object stopKey) { }

        /// <summary>Move focus to a node of the current render right now and announce it as a move
        /// (interrupting, the contexts crossed read as an arrow's are): a screen's own key that lands
        /// somewhere. False when the node is not in the render or is the focused one already, so the
        /// caller stays silent as at an edge.</summary>
        public virtual bool MoveTo(Graph.ControlId id) => false;

        /// <summary>Whether the current render has a focusable node in that Tab-stop: a screen offers
        /// a jump key (Alt+B for the board) only where its stop is on show.</summary>
        public virtual bool HasStop(object stopKey) => false;

        /// <summary>Land on a Tab-stop right now, as Tab would (its remembered position, else its
        /// selected member, else its first node), and announce the landing as a move, wherever focus
        /// was, in that stop included: a jump key's answer. False when the stop is not in the render.</summary>
        public virtual bool JumpToStop(object stopKey) => false;

        /// <summary>The Tab-stop the focused node belongs to, or null.</summary>
        public virtual object FocusedStopKey => null;

        // interrupt: true for focus MOVES (so held key-repeat reads the item you land on instead of
        // backing up a queue); false for screen-entry / landing readouts.
        protected static void Speak(string text, bool interrupt = false)
        {
            if (!string.IsNullOrEmpty(text)) Speech.Say(text, interrupt);
        }
    }

    /// <summary>
    /// Holds the active Navigator and is the entry point input dispatches into. ScreenManager re-attaches
    /// it on screen change. <see cref="FocusActive"/> is the module's focus-mode switch: with it off the
    /// navigator neither speaks landings nor runs type-ahead (the game keeps its own keys).
    /// </summary>
    public static class Navigation
    {
        public static Navigator Active = new GraphNavigator();

        /// <summary>Whether our navigation owns the keyboard right now (the module's focus mode).</summary>
        public static Func<bool> FocusActive = () => true;

        /// <summary>Plays the default hover cue when focus lands on a node; null = silent.</summary>
        public static Action HoverSound;

        public static void Attach(Screen screen) => Active?.Attach(screen);

        /// <summary>True when something is focused (the navigator owns the keys).</summary>
        public static bool HasFocus => Active != null && Active.HasFocus;

        public static bool DispatchJustPressed(InputAction action) =>
            Active != null && Active.OnInputJustPressed(action);

        /// <summary>Feed typed characters to the active navigator's type-ahead search (per frame).</summary>
        public static void TickTypeahead() => Active?.TickTypeahead();

        public static void AnnounceCurrent(bool interrupt = false) => Active?.AnnounceCurrent(interrupt);

        public static bool WouldHandle(InputAction action) => Active != null && Active.WouldHandle(action);

        public static bool HasStop(object stopKey) => Active != null && Active.HasStop(stopKey);

        public static bool JumpToStop(object stopKey) => Active != null && Active.JumpToStop(stopKey);

        public static void QuietNextLanding() => Active?.QuietNextLanding();

        /// <summary>The screen the navigator is bound to (the focused one), or null.</summary>
        public static Screen BoundScreen => Active?.BoundScreen;

        /// <summary>Re-establish initial focus if the focused screen has focusable content but nothing is
        /// focused yet. Ticked each frame.</summary>
        public static void EnsureFocus() => Active?.EnsureFocus();

        public static void Blur() => Active?.Blur();

        public static void ScreenClosed(Screen screen) => Active?.ScreenClosed(screen);

        public static void FocusNode(Graph.ControlId id, bool announce = true) => Active?.FocusNode(id, announce);

        public static void FocusStop(object stopKey) => Active?.FocusStop(stopKey);

        public static bool MoveTo(Graph.ControlId id) => Active != null && Active.MoveTo(id);

        public static object FocusedStopKey => Active?.FocusedStopKey;

        /// <summary>The focused node of the graph navigator, or null (no focus, or another navigator).</summary>
        public static Graph.GraphNode FocusedNode => (Active as GraphNavigator)?.FocusedNode;
    }
}
