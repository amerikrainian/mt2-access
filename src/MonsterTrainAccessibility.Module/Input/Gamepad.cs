using System.Collections.Generic;
using HarmonyLib;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Input
{
    /// <summary>
    /// The gamepad as the mod's bindings see it: which controls are held this frame and which were held
    /// the frame before, across every connected pad. Fed from the game's own gamepad driver
    /// (<see cref="CoreInputDriverGamepad.OnLateUpdate"/>, once per frame), read from its devices
    /// directly, so it is the same whether or not the game is let to hear the pad. While focus mode owns
    /// the input the driver's update is replaced: the devices are still polled, for us, and the game's
    /// per-pad data is fed "nothing held", so it never sees a press and never holds a stuck one.
    /// </summary>
    [HarmonyPatch(typeof(CoreInputDriverGamepad), nameof(CoreInputDriverGamepad.OnLateUpdate))]
    internal static class Gamepad
    {
        // A stick or trigger counts as a press past this deflection: well clear of drift.
        private const float Threshold = 0.5f;

        private static HashSet<ControllerInput> _held = new HashSet<ControllerInput>();
        private static HashSet<ControllerInput> _was = new HashSet<ControllerInput>();

        public static bool IsHeld(ControllerInput input) => _held.Contains(input);
        public static bool WasHeld(ControllerInput input) => _was.Contains(input);

        /// <summary>Whether any control is held (a rebind waits for the pad to be let go).</summary>
        public static bool AnyHeld => _held.Count > 0;

        /// <summary>The controls held this frame that were not the frame before.</summary>
        public static IEnumerable<ControllerInput> Pressed()
        {
            foreach (var input in _held)
                if (!_was.Contains(input)) yield return input;
        }

        /// <summary>The controls let go this frame.</summary>
        public static IEnumerable<ControllerInput> Released()
        {
            foreach (var input in _was)
                if (!_held.Contains(input)) yield return input;
        }

        // Focus mode: poll the devices for us and feed the game nothing.
        private static bool Prefix(CoreInputDriverGamepad __instance)
        {
            if (!FocusMode.Active || __instance._manager == null) return true;
            __instance._manager.Update();
            int pads = Mathf.Min(__instance._manager.gamepads.Count, __instance._gamePadDataCacheList.Length);
            for (int i = 0; i < pads; i++)
            {
                var cache = __instance._gamePadDataCacheList[i];
                if (cache == null) continue;
                foreach (var button in cache.buttonList) button.UpdateButtonParams(false);
                foreach (var axis in cache.axisList)
                {
                    axis.position = Vector2.zero;
                    axis.UpdateButtonParams(false, Vector3.zero, 0);
                }
            }
            return false;
        }

        // Every frame, after the devices updated (by the game or by the prefix above).
        private static void Postfix(CoreInputDriverGamepad __instance)
        {
            var previous = _was;
            _was = _held;
            previous.Clear();
            _held = previous;
            if (__instance._manager == null) return;
            var pads = __instance._manager.gamepads;
            for (int i = 0; i < pads.Count; i++)
            {
                var pad = pads[i];
                if (pad == null) continue;
                Button(pad, GamepadButton.Action1, ControllerInput.A);
                Button(pad, GamepadButton.Action2, ControllerInput.B);
                Button(pad, GamepadButton.Action3, ControllerInput.X);
                Button(pad, GamepadButton.Action4, ControllerInput.Y);
                Button(pad, GamepadButton.LeftBumper, ControllerInput.LeftShoulder);
                Button(pad, GamepadButton.RightBumper, ControllerInput.RightShoulder);
                Button(pad, GamepadButton.LeftStickButton, ControllerInput.LeftStickClick);
                Button(pad, GamepadButton.RightStickButton, ControllerInput.RightStickClick);
                Button(pad, GamepadButton.Start, ControllerInput.Start);
                Button(pad, GamepadButton.Back, ControllerInput.Back);
                Button(pad, GamepadButton.DpadUp, ControllerInput.DpadUp);
                Button(pad, GamepadButton.DpadDown, ControllerInput.DpadDown);
                Button(pad, GamepadButton.DpadLeft, ControllerInput.DpadLeft);
                Button(pad, GamepadButton.DpadRight, ControllerInput.DpadRight);
                Axis(pad, GamepadAxis.LeftStickY, ControllerInput.LeftStickUp, ControllerInput.LeftStickDown);
                Axis(pad, GamepadAxis.LeftStickX, ControllerInput.LeftStickRight, ControllerInput.LeftStickLeft);
                Axis(pad, GamepadAxis.RightStickY, ControllerInput.RightStickUp, ControllerInput.RightStickDown);
                Axis(pad, GamepadAxis.RightStickX, ControllerInput.RightStickRight, ControllerInput.RightStickLeft);
                Axis(pad, GamepadAxis.LeftTrigger, ControllerInput.LeftTrigger, null);
                Axis(pad, GamepadAxis.RightTrigger, ControllerInput.RightTrigger, null);
            }
        }

        private static void Button(GamepadDevice pad, GamepadButton button, ControllerInput input)
        {
            if (pad.GetButton(button)) _held.Add(input);
        }

        private static void Axis(GamepadDevice pad, GamepadAxis axis, ControllerInput positive, ControllerInput? negative)
        {
            float value = pad.GetAxis(axis);
            if (value >= Threshold) _held.Add(positive);
            else if (negative != null && value <= -Threshold) _held.Add(negative.Value);
        }

        /// <summary>Module teardown: nothing held, so a successor starts from a clean slate.</summary>
        public static void Reset()
        {
            _held.Clear();
            _was.Clear();
        }
    }
}
