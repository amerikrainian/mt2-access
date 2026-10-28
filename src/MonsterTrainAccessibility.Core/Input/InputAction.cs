using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrainAccessibility.Core.Input
{
    /// <summary>
    /// A named mod command with one or more bindings. Exposes per-frame phase state
    /// (JustPressed/Held/Released); the dispatcher routes phases into the active navigator, and fires
    /// <see cref="Performed"/> for a JustPressed that the navigator did not consume (the "global hotkey"
    /// fallback).
    /// </summary>
    public class InputAction
    {
        public string Key { get; }
        public string Label { get; }

        /// <summary>The display label resolved through the strings table ("bind.&lt;key&gt;"), falling
        /// back to the registration label. Use for anything spoken/displayed.</summary>
        public string DisplayLabel => Strings.Strings.BindLabel(Key, Label);

        /// <summary>The input layer this action lives in (decides when it is polled and how identical
        /// chords across categories resolve; see <see cref="InputCategory"/>).</summary>
        public InputCategory Category { get; set; } = InputCategory.Global;

        private readonly List<InputBinding> _bindings = new List<InputBinding>();
        public IReadOnlyList<InputBinding> Bindings => _bindings;

        /// <summary>Fired on JustPressed when not consumed by the navigator.</summary>
        public event Action Performed;

        /// <summary>Fired whenever the binding set changes (add/clear).</summary>
        public event Action BindingsChanged;

        public string BindingsDisplay =>
            _bindings.Count == 0 ? "(none)" : string.Join(", ", _bindings.Select(b => b.DisplayName));

        public InputAction(string key, string label)
        {
            Key = key;
            Label = label;
        }

        public InputAction AddBinding(InputBinding binding)
        {
            _bindings.Add(binding);
            BindingsChanged?.Invoke();
            return this;
        }

        /// <summary>Drop all bindings (a rebind replaces them, or a saved config reloads them).</summary>
        public void ClearBindings()
        {
            _bindings.Clear();
            BindingsChanged?.Invoke();
        }

        /// <summary>Remove one binding (the rebind capture stealing a within-category conflict).</summary>
        public void RemoveBinding(InputBinding binding)
        {
            if (_bindings.Remove(binding)) BindingsChanged?.Invoke();
        }

        public bool JustPressed { get { for (int i = 0; i < _bindings.Count; i++) if (_bindings[i].JustPressed()) return true; return false; } }
        public bool Held { get { for (int i = 0; i < _bindings.Count; i++) if (_bindings[i].Held()) return true; return false; } }
        public bool Released { get { for (int i = 0; i < _bindings.Count; i++) if (_bindings[i].Released()) return true; return false; } }

        /// <summary>Whether this action auto-repeats while held (nav directions + Tab). Set via Repeating().</summary>
        public bool Repeats { get; private set; }
        internal float NextRepeatTime;

        public InputAction Repeating() { Repeats = true; return this; }

        /// <summary>Display-only subgroup within the category's settings tree; dispatch ignores it.</summary>
        public string Group { get; private set; }

        public InputAction Grouped(string group) { Group = group; return this; }

        public void InvokePerformed() => Performed?.Invoke();

        /// <summary>Whether a handler listens: an action without one is a key a screen or the
        /// navigator answers, or nothing.</summary>
        public bool HasHandler => Performed != null;

        /// <summary>Whether the handler would do anything right now, for the key help: a glance is
        /// silent where its fact is not on screen, and is not listed there. Null = always.</summary>
        public Func<bool> Available { get; private set; }

        public InputAction When(Func<bool> available) { Available = available; return this; }

        public bool IsAvailable
        {
            get
            {
                if (Available == null) return true;
                try { return Available(); }
                catch (Exception e) { CoreLog.Warning("input: availability of " + Key + " failed: " + e.Message); return false; }
            }
        }
    }
}
