using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Input;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Input
{
    /// <summary>
    /// A key bound in the game's own key mapping to one of its controls (Access HUD), for a mod action
    /// that stands in for that control: whatever keys the player has bound to it in Bind Keys, read from
    /// the game's mapping on every poll, never kept, so a rebinding takes effect at once and the key help
    /// names the keys bound now. Pressed bare, as the game's own keys pass to it (<see cref="GameKeys"/>).
    /// </summary>
    public sealed class GameControlBinding : InputBinding
    {
        public global::InputManager.Controls Control { get; }

        public GameControlBinding(global::InputManager.Controls control)
        {
            Control = control;
        }

        // The keys the player has bound to the control, as the game maps them now.
        private List<KeyCode> Keys()
        {
            var keys = new List<KeyCode>();
            var mapping = global::InputManager.Inst != null ? global::InputManager.Inst.GetUserKeyMappingHelper() : null;
            if (mapping == null) return keys;
            foreach (var key in mapping.GetKeysMappedToControl(Control))
                if (key != null && key.keyCode != KeyCode.None) keys.Add(key.keyCode);
            return keys;
        }

        private static bool ModifierHeld =>
            UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl)
            || UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift)
            || UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);

        private bool Any(System.Func<KeyCode, bool> test)
        {
            if (ModifierHeld) return false;
            foreach (var key in Keys())
                if (test(key)) return true;
            return false;
        }

        public override bool JustPressed() => Any(UnityEngine.Input.GetKeyDown);
        public override bool Held() => Any(UnityEngine.Input.GetKey);
        public override bool Released() => Any(UnityEngine.Input.GetKeyUp);

        // A live type-ahead search owns bare letters, space, up/down and escape, as for any bare key.
        public override bool ConflictsWithTypeahead
        {
            get
            {
                foreach (var key in Keys())
                    if (new KeyboardBinding(key).ConflictsWithTypeahead) return true;
                return false;
            }
        }

        // The keys bound now, as the key help names any key: "Tab".
        public override string DisplayName
        {
            get
            {
                var names = new List<string>();
                foreach (var key in Keys()) names.Add(new KeyboardBinding(key).DisplayName);
                return string.Join(", ", names);
            }
        }

        public override string Type => "game";

        public override string Serialize() => Control.ToString();

        public static GameControlBinding Deserialize(string data)
            => System.Enum.TryParse(data, out global::InputManager.Controls control) ? new GameControlBinding(control) : null;
    }
}
