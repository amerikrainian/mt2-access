using System.Collections.Generic;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using ShinyShoe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// When active, our navigation owns the keyboard and the gamepad. Three things make that true:
    /// <list type="bullet">
    /// <item>The uGUI EventSystem stops sending navigation events and its selection is cleared, so
    /// arrows and Enter cannot also move or submit the game's own selection.</item>
    /// <item>The game's keyboard driver (<see cref="CoreInputDriverKeyboard"/>, which every game screen
    /// reads keys through) is fed "nothing held" in place of its own poll, so no game action hears a
    /// key. Our own keys are polled through Unity's legacy Input directly, which this does not touch.</item>
    /// <item>The game's gamepad driver is fed the same (<see cref="Input.Gamepad"/>), while the pads
    /// are still polled for our controller bindings.</item>
    /// </list>
    /// Feeding "released" rather than skipping the poll matters: a key held at the moment focus mode
    /// engages (the toggle chord itself) is seen let go, never stuck down. Fully reversible: turning
    /// focus mode off lets the drivers poll again and restores the EventSystem's navigation. A fresh
    /// EventSystem is noticed by a per-frame instance compare and suppressed on sight. The mouse is left
    /// alone.
    /// </summary>
    [HarmonyPatch(typeof(CoreInputDriverKeyboard), nameof(CoreInputDriverKeyboard.OnLateUpdate))]
    public static class FocusMode
    {
        private static bool _active;
        private static EventSystem _applied; // the EventSystem instance the suppression was applied to

        public static bool Active => _active;

        public static void Toggle() => Set(!_active);

        public static void Set(bool on)
        {
            if (on == _active) return;
            _active = on;
            if (on) Apply(EventSystem.current);
            else Release();
        }

        /// <summary>Per-frame: apply the EventSystem suppression to a fresh EventSystem (Unity offers no
        /// event for one).</summary>
        public static void Tick()
        {
            if (!_active) return;
            var es = EventSystem.current;
            if (es != null && !ReferenceEquals(es, _applied)) Apply(es);
        }

        /// <summary>Module teardown. With <paramref name="restore"/> the game gets its navigation events
        /// back (a shutdown); without it only our state is dropped, because a newer generation has
        /// already taken over (a reload). The driver patches go with this generation's Harmony id.</summary>
        public static void Shutdown(bool restore)
        {
            if (restore) Set(false);
            else _active = false;
        }

        // The game's keyboard poll, replaced while we own the keys: every key it tracks is let go.
        private static bool Prefix(CoreInputDriverKeyboard __instance)
        {
            if (!_active) return true;
            foreach (KeyValuePair<KeyCode, CoreInputDataButton> key in __instance._keyData)
                key.Value.UpdateButtonParams(false);
            return false;
        }

        private static void Apply(EventSystem es)
        {
            _applied = es;
            if (es == null)
            {
                CoreLog.Info("FocusMode: no EventSystem yet; will engage when one appears.");
                return;
            }
            es.sendNavigationEvents = false;
            es.SetSelectedGameObject(null);
        }

        private static void Release()
        {
            var es = _applied ?? EventSystem.current;
            _applied = null;
            if (es != null) es.sendNavigationEvents = true;
        }
    }
}
