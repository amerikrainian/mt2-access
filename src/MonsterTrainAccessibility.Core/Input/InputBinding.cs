using System;
using System.Collections.Generic;

namespace MonsterTrainAccessibility.Core.Input
{
    /// <summary>
    /// Base for a single key/button combo. Phase queries are polled each frame by InputManager; the
    /// engine-backed keyboard binding lives in the module, controller bindings can join as a sibling.
    /// </summary>
    public abstract class InputBinding
    {
        /// <summary>Human-readable combo, e.g. "Ctrl+Shift+A".</summary>
        public abstract string DisplayName { get; }

        public abstract bool JustPressed();
        public abstract bool Held();
        public abstract bool Released();

        /// <summary>True when this combo is one the type-ahead search owns while a search is live (a
        /// bare letter, space, up/down, escape), so a live search swallows the action instead.</summary>
        public virtual bool ConflictsWithTypeahead => false;

        // ---- persistence ----

        /// <summary>Stable kind tag used to pick a deserializer, e.g. "keyboard".</summary>
        public abstract string Type { get; }

        /// <summary>Serialize this binding's data; round-trips through <see cref="Deserialize"/>.</summary>
        public abstract string Serialize();

        // Stable identity key for chord shadowing (Type + serialized data). A binding is immutable (a
        // rebind makes a NEW binding), so this is constant and cached: RebuildLive runs every frame.
        private string _chord;
        public string Chord => _chord ?? (_chord = Type + "\n" + Serialize());

        private static readonly Dictionary<string, Func<string, InputBinding>> _deserializers =
            new Dictionary<string, Func<string, InputBinding>>();

        /// <summary>Register a binding kind's deserializer (the module registers "keyboard").</summary>
        public static void RegisterType(string type, Func<string, InputBinding> deserialize)
            => _deserializers[type] = deserialize;

        /// <summary>Rebuild a binding from a (type, data) pair, or null if the type is unknown/invalid.</summary>
        public static InputBinding Deserialize(string type, string data)
            => type != null && _deserializers.TryGetValue(type, out var f) ? f(data) : null;
    }
}
