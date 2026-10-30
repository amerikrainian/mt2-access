using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Input;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Input
{
    /// <summary>
    /// A key bound in the game's own key mapping to one of its controls (Access HUD), for a mod action
    /// that stands in for that control: whatever keys the player has bound to it in Bind Keys, read from
    /// the game's mapping on every poll, never kept, so a rebinding takes effect at once and the key help
    /// names the keys bound now. Pressed bare, as the game's own keys pass to it (<see cref="GameKeys"/>),
    /// or with Shift for a mod action beside the game's (Shift and a card's key reads the card), which
    /// the game, hearing its keys bare only, does not hear.
    /// </summary>
    public sealed class GameControlBinding : InputBinding
    {
        public global::InputManager.Controls Control { get; }
        public bool Shift { get; }

        public GameControlBinding(global::InputManager.Controls control, bool shift = false)
        {
            Control = control;
            Shift = shift;
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

        private static bool CtrlOrAltHeld =>
            UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl)
            || UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);
        private static bool ShiftHeld => UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);

        // Modifiers matched exactly, as for any key of the mod's.
        private bool Any(System.Func<KeyCode, bool> test)
        {
            if (CtrlOrAltHeld || ShiftHeld != Shift) return false;
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
                    if (new KeyboardBinding(key, shift: Shift).ConflictsWithTypeahead) return true;
                return false;
            }
        }

        // The keys bound now, as the key help names any key: "Tab".
        public override string DisplayName
        {
            get
            {
                var names = new List<string>();
                foreach (var key in Keys()) names.Add(new KeyboardBinding(key, shift: Shift).DisplayName);
                return string.Join(", ", names);
            }
        }

        // Each key bound now, for a rebind's conflicts: the mod's keys never take one of the game's.
        public override IEnumerable<string> Combos
        {
            get
            {
                foreach (var key in Keys()) yield return new KeyboardBinding(key, shift: Shift).DisplayName;
            }
        }

        // The game's to change, in its Bind Keys.
        public override bool Fixed => true;

        public override string Type => "game";

        // "AccessHud", or "Card01|shift".
        public override string Serialize() => Shift ? Control + "|shift" : Control.ToString();

        public static GameControlBinding Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;
            var parts = data.Split('|');
            return System.Enum.TryParse(parts[0], out global::InputManager.Controls control)
                ? new GameControlBinding(control, parts.Length > 1 && parts[1] == "shift") : null;
        }
    }
}
