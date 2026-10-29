using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Input
{
    using InputManager = Core.Input.InputManager;
    using ScreenManager = Core.Screens.ScreenManager;

    /// <summary>A mod screen that lets some of the game's own controls through while it is in front
    /// (the battle: undo, the piles, end turn, the card number keys).</summary>
    internal interface IGameKeys
    {
        IEnumerable<global::InputManager.Controls> GameControls { get; }
    }

    /// <summary>
    /// The keys the game hears while focus mode owns the keyboard: those the player has bound (in the
    /// game's own key mapping) to the controls the screen in front lets through, held without Ctrl,
    /// Shift or Alt, and not bound bare to one of the mod's actions (the mod's key wins). So undo's
    /// hold, the piles and end turn are the game's own, on the player's own keys.
    /// </summary>
    internal static class GameKeys
    {
        private static readonly HashSet<KeyCode> s_keys = new HashSet<KeyCode>();
        private static int s_frame = -1;

        /// <summary>The keys passing to the game this frame; null when none do.</summary>
        public static HashSet<KeyCode> Passing()
        {
            if (Time.frameCount != s_frame)
            {
                s_frame = Time.frameCount;
                s_keys.Clear();
                try { Collect(); }
                catch (System.Exception e) { CoreLog.Warning("game keys: " + e.Message); s_keys.Clear(); }
            }
            return s_keys.Count > 0 ? s_keys : null;
        }

        private static void Collect()
        {
            if (!(ScreenManager.Current is IGameKeys screen) || UI.TextEdit.OwnsKeyboard) return;
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl)
                || UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift)
                || UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt)) return;
            var mapping = global::InputManager.Inst != null ? global::InputManager.Inst.GetUserKeyMappingHelper() : null;
            if (mapping == null) return;
            foreach (var control in screen.GameControls)
                foreach (var key in mapping.GetKeysMappedToControl(control))
                    if (key != null) s_keys.Add(key.keyCode);
            foreach (var action in InputManager.Actions)
                foreach (var binding in action.Bindings)
                    if (binding is KeyboardBinding k && !k.Ctrl && !k.Shift && !k.Alt) s_keys.Remove(k.Key);
        }
    }
}
