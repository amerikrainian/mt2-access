using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrainAccessibility.Core.UI;

namespace MonsterTrainAccessibility.Core.Screens
{
    /// <summary>
    /// Resolves the active screen stack each frame by polling every registered screen's
    /// <see cref="Screen.IsActive"/> (poll-and-diff, robust to the game rebuilding its views) and
    /// dispatches lifecycle events. The stack is ordered bottom to top by Layer; Current is the deepest
    /// screen of the top entry (the focused screen). Ticked from the module.
    /// </summary>
    public static class ScreenManager
    {
        private static readonly List<Screen> _registered = new List<Screen>();
        private static List<Screen> _stack = new List<Screen>();
        private static Screen _focused; // the deepest screen the navigator is currently attached to

        public static Screen Current => _stack.Count > 0 ? _stack[_stack.Count - 1].DeepestActiveScreen() : null;
        public static IReadOnlyList<Screen> Stack => _stack;
        public static IReadOnlyList<Screen> Registered => _registered;

        /// <summary>Active screens in focus-priority order: the focused screen first, then outward/down
        /// to the base context. This is the order the input claim-chain walks.</summary>
        public static IEnumerable<Screen> FocusedFirst()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var chain = new List<Screen>();
                for (var s = _stack[i]; s != null; s = s.ActiveChild) chain.Add(s); // outer -> deepest
                for (int j = chain.Count - 1; j >= 0; j--) yield return chain[j];    // deepest -> outer
            }
        }

        public static void Register(Screen screen) => _registered.Add(screen);

        /// <summary>Pop everything and forget every registration (module teardown / tests).</summary>
        public static void Shutdown()
        {
            for (int i = _stack.Count - 1; i >= 0; i--) PopTree(_stack[i]);
            _stack = new List<Screen>();
            _focused = null;
            _registered.Clear();
            Navigation.Attach(null);
        }

        public static void Tick()
        {
            ApplyDiff(Resolve()); // poll the outer (game-driven) screens -> push/pop on the persistent stack
            SyncFocus();          // focus the deepest screen (outer changes; before OnUpdate)
            Current?.OnUpdate();  // may push/remove child sub-screens
            SyncFocus();          // re-sync if OnUpdate (or this frame's input) changed the child tree
            // Standardized first-focus: once the focused screen has built its content, make sure
            // something is focused. No-op when focus already exists or the screen is intentionally
            // unfocused.
            Navigation.EnsureFocus();
        }

        /// <summary>Active screens, ordered bottom (low layer) to top (high layer).</summary>
        private static List<Screen> Resolve()
        {
            var active = new List<Screen>();
            for (int i = 0; i < _registered.Count; i++)
                if (SafeIsActive(_registered[i])) active.Add(_registered[i]);
            return active.OrderBy(s => s.Layer).ToList();
        }

        private static bool SafeIsActive(Screen s)
        {
            try { return s.IsActive(); }
            catch (Exception e)
            {
                CoreLog.Error("Screen.IsActive threw for '" + s.Key + "': " + e.Message);
                return false;
            }
        }

        // Diff the polled active set against the persistent stack: pop outer screens that went inactive
        // (each with its whole child subtree) and push newly-active ones. Focus is handled separately by
        // SyncFocus so child-tree changes and outer changes go through one path.
        private static void ApplyDiff(List<Screen> desired)
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
                if (!desired.Contains(_stack[i])) PopTree(_stack[i]);
            for (int i = 0; i < desired.Count; i++)
                if (!_stack.Contains(desired[i])) { var s = desired[i]; Safe(() => s.OnPush(), s, "OnPush"); }
            _stack = desired;
        }

        // An outer screen leaving the stack disposes its child subtree (deepest-first), then OnPops itself.
        private static void PopTree(Screen s)
        {
            if (s.ActiveChild != null) s.RemoveChild(s.ActiveChild);
            Safe(() => s.OnPop(), s, "OnPop");
            // Closing a screen clears its nav state (reopening starts fresh), unless it opts out.
            if (!s.KeepStateOnPop) Navigation.ScreenClosed(s);
        }

        // Re-attach the navigator whenever the deepest (focused) screen changes, from an outer push/pop
        // OR a child-tree push/remove. Idempotent (no-op when the focused screen is unchanged).
        private static void SyncFocus()
        {
            var cur = Current;
            if (ReferenceEquals(cur, _focused)) return;
            _focused?.OnUnfocus();
            _focused = cur;
            Safe(() => cur?.OnFocus(), cur, "OnFocus"); // speaks the screen name
            // Bind the navigator; the initial-focus landing is announced by EnsureFocus once the screen's
            // content exists.
            Navigation.Attach(cur);
        }

        private static void Safe(Action a, Screen s, string hook)
        {
            try { a(); }
            catch (Exception e) { CoreLog.Error("Screen." + hook + " threw for '" + (s?.Key ?? "?") + "': " + e); }
        }
    }
}
