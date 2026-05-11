using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using ShinyShoe;
using UnityEngine;
using ModScreenManager = MonsterTrainAccessibility.UI.Screens.ScreenManager;

namespace MonsterTrainAccessibility.Input
{
    internal static class InputManager
    {
        private static readonly List<InputAction> _actions = new List<InputAction>();
        private static readonly Dictionary<string, InputAction> _actionsByKey = new Dictionary<string, InputAction>(System.StringComparer.Ordinal);
        private static readonly HashSet<string> _activeActions = new HashSet<string>(System.StringComparer.Ordinal);
        private static readonly HashSet<string> _activeControllerActions = new HashSet<string>(System.StringComparer.Ordinal);
        private static readonly HashSet<string> _blockedNativeActions = new HashSet<string>(System.StringComparer.Ordinal);
        private static readonly HashSet<ControllerInput> _heldControllerInputs = new HashSet<ControllerInput>();
        private static readonly HashSet<ControllerInput> _previousHeldControllerInputs = new HashSet<ControllerInput>();
        private static readonly HashSet<ControllerInput> _justPressedControllerInputs = new HashSet<ControllerInput>();
        private static readonly HashSet<ControllerInput> _blockedNativeControllerInputs = new HashSet<ControllerInput>();
        private static readonly HashSet<ControllerInput> _suppressedControllerInputsUntilRelease = new HashSet<ControllerInput>();
        private static readonly HashSet<ControllerInput> _listenInitialHeldControllerInputs = new HashSet<ControllerInput>();
        private static readonly Dictionary<KeyCode, int> _unmodifiedChordCandidateFrames = new Dictionary<KeyCode, int>();
        private static readonly HashSet<KeyCode> _unmodifiedChordSuppressedUntilRelease = new HashSet<KeyCode>();
        private static readonly HashSet<KeyCode> _suppressedKeysUntilRelease = new HashSet<KeyCode>();
        private static readonly HashSet<KeyCode> _listenInitialHeldKeys = new HashSet<KeyCode>();
        private static System.Action<InputBinding> _listenCallback;
        private static ControllerInput? _listenControllerModifier;
        private static int _lastControllerSpeechStopFrame = -1;

        private static bool _initialized;

        public static IReadOnlyList<InputAction> Actions => _actions;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _actions.Clear();
            _actionsByKey.Clear();
            _activeActions.Clear();
            _activeControllerActions.Clear();
            _blockedNativeActions.Clear();
            _heldControllerInputs.Clear();
            _previousHeldControllerInputs.Clear();
            _justPressedControllerInputs.Clear();
            _blockedNativeControllerInputs.Clear();
            _suppressedControllerInputsUntilRelease.Clear();
            _listenInitialHeldControllerInputs.Clear();
            _unmodifiedChordCandidateFrames.Clear();
            _unmodifiedChordSuppressedUntilRelease.Clear();
            _suppressedKeysUntilRelease.Clear();
            _listenInitialHeldKeys.Clear();
            _listenCallback = null;
            _listenControllerModifier = null;
            _lastControllerSpeechStopFrame = -1;
            RegisterGameNavigationActions();
            RegisterModActions();
        }

        public static void Shutdown()
        {
            _initialized = false;
            _actions.Clear();
            _actionsByKey.Clear();
            _activeActions.Clear();
            _activeControllerActions.Clear();
            _blockedNativeActions.Clear();
            _heldControllerInputs.Clear();
            _previousHeldControllerInputs.Clear();
            _justPressedControllerInputs.Clear();
            _blockedNativeControllerInputs.Clear();
            _suppressedControllerInputsUntilRelease.Clear();
            _listenInitialHeldControllerInputs.Clear();
            _unmodifiedChordCandidateFrames.Clear();
            _unmodifiedChordSuppressedUntilRelease.Clear();
            _suppressedKeysUntilRelease.Clear();
            _listenInitialHeldKeys.Clear();
            _listenCallback = null;
            _listenControllerModifier = null;
            _lastControllerSpeechStopFrame = -1;
        }

        public static bool IsListening => _listenCallback != null;

        public static void StartListening(System.Action<InputBinding> callback)
        {
            _listenCallback = callback;
            _listenControllerModifier = null;
            SnapshotListenInitialHeldKeys();
            _listenInitialHeldControllerInputs.Clear();
            foreach (ControllerInput input in _heldControllerInputs)
            {
                _listenInitialHeldControllerInputs.Add(input);
            }
        }

        public static void StopListening()
        {
            _listenCallback = null;
            _listenControllerModifier = null;
            _listenInitialHeldKeys.Clear();
            _listenInitialHeldControllerInputs.Clear();
        }

        public static bool Poll()
        {
            if (!_initialized)
            {
                return false;
            }

            if (_listenCallback != null)
            {
                CaptureKeyboardBinding();
                return true;
            }

            bool blocksGameInput = false;
            for (int i = 0; i < _actions.Count; i++)
            {
                blocksGameInput |= PollAction(_actions[i]);
            }

            return blocksGameInput;
        }

        public static InputAction GetAction(string key)
        {
            InputAction action;
            return key != null && _actionsByKey.TryGetValue(key, out action) ? action : null;
        }

        public static bool ShouldBlockNativeAction(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            return _blockedNativeActions.Contains(action.Key) ||
                ModScreenManager.BlocksGameInputForAction(action);
        }

        public static bool ShouldBlockNativeMapping(CoreInputControlMapping mapping)
        {
            if (mapping == null || mapping.deviceID != InputDeviceType.Gamepad)
            {
                return false;
            }

            if (_listenCallback != null)
            {
                return true;
            }

            ControllerInput? input = ToControllerInput(mapping);
            return input != null && _blockedNativeControllerInputs.Contains(input.Value);
        }

        public static void StopSpeechForControllerPress()
        {
            if (_lastControllerSpeechStopFrame == Time.frameCount)
            {
                return;
            }

            _lastControllerSpeechStopFrame = Time.frameCount;
            SpeechManager.Stop();
        }

        public static void PollController(CoreInputDriverGamepad driver)
        {
            if (!_initialized || driver == null)
            {
                return;
            }

            _blockedNativeControllerInputs.Clear();
            UpdateHeldControllerInputs(driver);
            StopSpeechForControllerPresses();

            if (_listenCallback != null)
            {
                CaptureControllerBinding();
                return;
            }

            for (int i = 0; i < _actions.Count; i++)
            {
                PollControllerAction(_actions[i]);
            }
        }

        private static void RegisterGameNavigationActions()
        {
            AddAction("ui_up", new Message("input.navigate_up"));
            AddAction("ui_down", new Message("input.navigate_down"));
            AddAction("ui_left", new Message("input.navigate_left"));
            AddAction("ui_right", new Message("input.navigate_right"));
            AddAction("ui_select", new Message("input.select"));
            AddAction("ui_accept", new Message("input.accept"));
            AddAction("ui_cancel", new Message("input.cancel"))
                .AddBinding(KeyCode.Escape)
                .BlocksGameInput = true;
        }

        private static void RegisterModActions()
        {
            AddAction("help", new Message("input.help"))
                .AddBinding(KeyCode.F1)
                .AddBinding(ControllerInput.Back, ControllerInput.LeftTrigger)
                .BlocksGameInput = true;

            AddAction("buffer_prev_item", new Message("input.buffer_prev_item"))
                .AddBinding(KeyCode.UpArrow, ctrl: true)
                .AddBinding(ControllerInput.RightStickUp)
                .BlocksGameInput = true;

            AddAction("buffer_next_item", new Message("input.buffer_next_item"))
                .AddBinding(KeyCode.DownArrow, ctrl: true)
                .AddBinding(ControllerInput.RightStickDown)
                .BlocksGameInput = true;

            AddAction("buffer_prev", new Message("input.buffer_prev"))
                .AddBinding(KeyCode.LeftArrow, ctrl: true)
                .AddBinding(ControllerInput.RightStickLeft)
                .BlocksGameInput = true;

            AddAction("buffer_next", new Message("input.buffer_next"))
                .AddBinding(KeyCode.RightArrow, ctrl: true)
                .AddBinding(ControllerInput.RightStickRight)
                .BlocksGameInput = true;

            AddAction("mod_settings", new Message("input.mod_settings"))
                .AddBinding(KeyCode.M, ctrl: true)
                .AddBinding(ControllerInput.Start, ControllerInput.LeftTrigger)
                .BlocksGameInput = true;

            AddAction("debug_commands", new Message("input.debug_commands"))
                .AddBinding(KeyCode.D, ctrl: true, shift: true)
                .BlocksGameInput = true;

            AddAction("read_gold", new Message("input.read_gold"))
                .AddBinding(KeyCode.G, ctrl: true)
                .AddBinding(ControllerInput.A, ControllerInput.RightTrigger)
                .BlocksGameInput = true;

            AddAction("read_pyre_health", new Message("input.read_pyre_health"))
                .AddBinding(KeyCode.H, ctrl: true)
                .AddBinding(ControllerInput.A, ControllerInput.LeftTrigger)
                .BlocksGameInput = true;

            AddAction("read_ember", new Message("input.read_ember"))
                .AddBinding(KeyCode.R)
                .AddBinding(ControllerInput.X, ControllerInput.LeftTrigger)
                .BlocksGameInput = true;

            AddAction("read_forge_points", new Message("input.read_forge_points"))
                .AddBinding(KeyCode.R, ctrl: true)
                .AddBinding(ControllerInput.X, ControllerInput.RightTrigger)
                .BlocksGameInput = true;

            AddAction("read_unit_outcome", new Message("input.read_unit_outcome"))
                .AddBinding(KeyCode.I)
                .AddBinding(ControllerInput.Y, ControllerInput.RightTrigger)
                .BlocksGameInput = true;

            AddAction("read_floor_outcomes", new Message("input.read_floor_outcomes"))
                .AddBinding(KeyCode.I, ctrl: true)
                .AddBinding(ControllerInput.B, ControllerInput.RightTrigger)
                .BlocksGameInput = true;

            AddAction("read_all_floor_outcomes", new Message("input.read_all_floor_outcomes"))
                .AddBinding(KeyCode.I, ctrl: true, shift: true)
                .AddBinding(ControllerInput.Y, ControllerInput.LeftTrigger)
                .BlocksGameInput = true;

            AddAction("read_floor_capacity", new Message("input.read_floor_capacity"))
                .AddBinding(KeyCode.B)
                .AddBinding(ControllerInput.B, ControllerInput.LeftTrigger)
                .BlocksGameInput = true;

            AddAction("read_all_floor_capacity", new Message("input.read_all_floor_capacity"))
                .AddBinding(KeyCode.B, shift: true)
                .AddBinding(ControllerInput.DpadUp, ControllerInput.RightTrigger)
                .BlocksGameInput = true;

            AddAction("jump_to_hand", new Message("input.jump_to_hand"))
                .AddBinding(KeyCode.G)
                .BlocksGameInput = true;

        }

        private static InputAction AddAction(string key, Message label)
        {
            InputAction action = new InputAction(key, label?.Resolve() ?? key);
            _actions.Add(action);
            _actionsByKey[key] = action;
            return action;
        }

        public static bool ShouldBlockBufferNavigation(string movementActionKey)
        {
            string bufferActionKey;
            switch (movementActionKey)
            {
                case "ui_up":
                    bufferActionKey = "buffer_prev_item";
                    break;
                case "ui_down":
                    bufferActionKey = "buffer_next_item";
                    break;
                case "ui_left":
                    bufferActionKey = "buffer_prev";
                    break;
                case "ui_right":
                    bufferActionKey = "buffer_next";
                    break;
                default:
                    return false;
            }

            InputAction action = GetAction(bufferActionKey);
            return action != null && action.BlocksGameInput && IsActionHeld(action);
        }

        private static bool PollAction(InputAction action)
        {
            bool isHeld = IsActionHeld(action);
            bool pendingUnmodifiedChord = !isHeld && IsPendingUnmodifiedChord(action);
            bool wasHeld = _activeActions.Contains(action.Key);
            bool shouldBlockNative = (isHeld || pendingUnmodifiedChord) && action.BlocksGameInput && ModScreenManager.BlocksGameInputForAction(action);
            if (shouldBlockNative)
            {
                _blockedNativeActions.Add(action.Key);
            }

            bool blocksGameInput = (isHeld || pendingUnmodifiedChord) && _blockedNativeActions.Contains(action.Key);

            if (isHeld)
            {
                if (!wasHeld)
                {
                    _activeActions.Add(action.Key);
                    ModScreenManager.DispatchAction(action, InputActionState.JustPressed);
                }
                else
                {
                    ModScreenManager.DispatchAction(action, InputActionState.Pressed);
                }

                return blocksGameInput;
            }

            if (pendingUnmodifiedChord)
            {
                return blocksGameInput;
            }

            if (wasHeld)
            {
                _activeActions.Remove(action.Key);
                ModScreenManager.DispatchAction(action, InputActionState.JustReleased);
                _blockedNativeActions.Remove(action.Key);
            }
            else
            {
                _blockedNativeActions.Remove(action.Key);
            }

            return false;
        }

        private static bool IsActionHeld(InputAction action)
        {
            IReadOnlyList<InputBinding> bindings = action.Bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                KeyboardBinding keyboardBinding = bindings[i] as KeyboardBinding;
                if (keyboardBinding != null && IsBindingHeld(keyboardBinding))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PollControllerAction(InputAction action)
        {
            ControllerBinding binding = GetHeldControllerBinding(action);
            bool isHeld = binding != null;
            bool wasHeld = _activeControllerActions.Contains(action.Key);
            bool shouldBlockNative = isHeld && action.BlocksGameInput && ModScreenManager.BlocksGameInputForAction(action);
            if (shouldBlockNative)
            {
                _blockedNativeControllerInputs.Add(binding.Input);
                if (binding.Modifier != null)
                {
                    _blockedNativeControllerInputs.Add(binding.Modifier.Value);
                }
            }

            if (isHeld)
            {
                if (!wasHeld)
                {
                    _activeControllerActions.Add(action.Key);
                    ModScreenManager.DispatchAction(action, InputActionState.JustPressed);
                }
                else
                {
                    ModScreenManager.DispatchAction(action, InputActionState.Pressed);
                }

                return shouldBlockNative;
            }

            if (wasHeld)
            {
                _activeControllerActions.Remove(action.Key);
                ModScreenManager.DispatchAction(action, InputActionState.JustReleased);
            }

            return false;
        }

        private static void StopSpeechForControllerPresses()
        {
            if (_justPressedControllerInputs.Count > 0)
            {
                StopSpeechForControllerPress();
            }
        }

        private static ControllerBinding GetHeldControllerBinding(InputAction action)
        {
            if (action == null)
            {
                return null;
            }

            IReadOnlyList<InputBinding> bindings = action.Bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                ControllerBinding controllerBinding = bindings[i] as ControllerBinding;
                if (controllerBinding != null && controllerBinding.Matches(controllerBinding.Input, IsControllerInputHeld))
                {
                    return controllerBinding;
                }
            }

            return null;
        }

        private static bool IsControllerInputHeld(ControllerInput input)
        {
            return _heldControllerInputs.Contains(input) && !_suppressedControllerInputsUntilRelease.Contains(input);
        }

        private static void UpdateHeldControllerInputs(CoreInputDriverGamepad driver)
        {
            _previousHeldControllerInputs.Clear();
            foreach (ControllerInput input in _heldControllerInputs)
            {
                _previousHeldControllerInputs.Add(input);
            }

            _heldControllerInputs.Clear();
            _justPressedControllerInputs.Clear();
            int count = driver.GetGamepadCount();
            for (int i = 0; i < count; i++)
            {
                AddHeldButton(driver, i, GamepadButton.Action1, ControllerInput.A);
                AddHeldButton(driver, i, GamepadButton.Action2, ControllerInput.B);
                AddHeldButton(driver, i, GamepadButton.Action3, ControllerInput.X);
                AddHeldButton(driver, i, GamepadButton.Action4, ControllerInput.Y);
                AddHeldButton(driver, i, GamepadButton.LeftBumper, ControllerInput.LeftShoulder);
                AddHeldButton(driver, i, GamepadButton.RightBumper, ControllerInput.RightShoulder);
                AddHeldButton(driver, i, GamepadButton.LeftStickButton, ControllerInput.LeftStickClick);
                AddHeldButton(driver, i, GamepadButton.RightStickButton, ControllerInput.RightStickClick);
                AddHeldButton(driver, i, GamepadButton.Start, ControllerInput.Start);
                AddHeldButton(driver, i, GamepadButton.Back, ControllerInput.Back);
                AddHeldButton(driver, i, GamepadButton.DpadUp, ControllerInput.DpadUp);
                AddHeldButton(driver, i, GamepadButton.DpadDown, ControllerInput.DpadDown);
                AddHeldButton(driver, i, GamepadButton.DpadLeft, ControllerInput.DpadLeft);
                AddHeldButton(driver, i, GamepadButton.DpadRight, ControllerInput.DpadRight);

                AddHeldAxis(driver, i, GamepadAxis.LeftStickY, InputFilter.PositiveAxis, ControllerInput.LeftStickUp);
                AddHeldAxis(driver, i, GamepadAxis.LeftStickY, InputFilter.NegativeAxis, ControllerInput.LeftStickDown);
                AddHeldAxis(driver, i, GamepadAxis.LeftStickX, InputFilter.NegativeAxis, ControllerInput.LeftStickLeft);
                AddHeldAxis(driver, i, GamepadAxis.LeftStickX, InputFilter.PositiveAxis, ControllerInput.LeftStickRight);
                AddHeldAxis(driver, i, GamepadAxis.RightStickY, InputFilter.PositiveAxis, ControllerInput.RightStickUp);
                AddHeldAxis(driver, i, GamepadAxis.RightStickY, InputFilter.NegativeAxis, ControllerInput.RightStickDown);
                AddHeldAxis(driver, i, GamepadAxis.RightStickX, InputFilter.NegativeAxis, ControllerInput.RightStickLeft);
                AddHeldAxis(driver, i, GamepadAxis.RightStickX, InputFilter.PositiveAxis, ControllerInput.RightStickRight);
                AddHeldAxis(driver, i, GamepadAxis.LeftTrigger, InputFilter.PositiveAxis, ControllerInput.LeftTrigger);
                AddHeldAxis(driver, i, GamepadAxis.RightTrigger, InputFilter.PositiveAxis, ControllerInput.RightTrigger);
            }

            foreach (ControllerInput input in _heldControllerInputs)
            {
                if (!_previousHeldControllerInputs.Contains(input))
                {
                    _justPressedControllerInputs.Add(input);
                }
            }

            ControllerInput[] suppressed = new ControllerInput[_suppressedControllerInputsUntilRelease.Count];
            _suppressedControllerInputsUntilRelease.CopyTo(suppressed);
            for (int i = 0; i < suppressed.Length; i++)
            {
                if (!_heldControllerInputs.Contains(suppressed[i]))
                {
                    _suppressedControllerInputsUntilRelease.Remove(suppressed[i]);
                }
            }

            ControllerInput[] initialHeld = new ControllerInput[_listenInitialHeldControllerInputs.Count];
            _listenInitialHeldControllerInputs.CopyTo(initialHeld);
            for (int i = 0; i < initialHeld.Length; i++)
            {
                if (!_heldControllerInputs.Contains(initialHeld[i]))
                {
                    _listenInitialHeldControllerInputs.Remove(initialHeld[i]);
                }
            }
        }

        private static void AddHeldButton(CoreInputDriverGamepad driver, int gamepadIndex, GamepadButton button, ControllerInput input)
        {
            CoreInputDataButton data = driver.GetButton(button, gamepadIndex);
            if (data != null && data.IsHeld())
            {
                _heldControllerInputs.Add(input);
            }
        }

        private static void AddHeldAxis(CoreInputDriverGamepad driver, int gamepadIndex, GamepadAxis axis, InputFilter filter, ControllerInput input)
        {
            CoreInputDataAxis data = driver.GetAxis(axis, gamepadIndex);
            if (data != null && data.IsGestureSignaled(InputGesture.Held, filter))
            {
                _heldControllerInputs.Add(input);
            }
        }

        private static bool IsBindingHeld(KeyboardBinding binding)
        {
            if (!UnityEngine.Input.GetKey(binding.Keycode))
            {
                _unmodifiedChordCandidateFrames.Remove(binding.Keycode);
                _unmodifiedChordSuppressedUntilRelease.Remove(binding.Keycode);
                _suppressedKeysUntilRelease.Remove(binding.Keycode);
                return false;
            }

            if (_suppressedKeysUntilRelease.Contains(binding.Keycode))
            {
                return false;
            }

            bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
            bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
            bool alt = UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);

            if (!binding.Ctrl && !binding.Shift && !binding.Alt && HasModifiedBindingForKey(binding.Keycode))
            {
                return IsUnmodifiedChordBindingHeld(binding.Keycode, ctrl, shift, alt);
            }

            return ctrl == binding.Ctrl &&
                shift == binding.Shift &&
                alt == binding.Alt;
        }

        private static bool IsUnmodifiedChordBindingHeld(KeyCode keycode, bool ctrl, bool shift, bool alt)
        {
            if (ctrl || shift || alt)
            {
                _unmodifiedChordCandidateFrames.Remove(keycode);
                _unmodifiedChordSuppressedUntilRelease.Add(keycode);
                return false;
            }

            if (_unmodifiedChordSuppressedUntilRelease.Contains(keycode))
            {
                return false;
            }

            int candidateFrame;
            if (UnityEngine.Input.GetKeyDown(keycode))
            {
                _unmodifiedChordCandidateFrames[keycode] = UnityEngine.Time.frameCount;
                return false;
            }

            if (_unmodifiedChordCandidateFrames.TryGetValue(keycode, out candidateFrame) &&
                UnityEngine.Time.frameCount <= candidateFrame)
            {
                return false;
            }

            return true;
        }

        private static bool IsPendingUnmodifiedChord(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            IReadOnlyList<InputBinding> bindings = action.Bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                KeyboardBinding keyboardBinding = bindings[i] as KeyboardBinding;
                if (keyboardBinding == null ||
                    keyboardBinding.Ctrl ||
                    keyboardBinding.Shift ||
                    keyboardBinding.Alt ||
                    !HasModifiedBindingForKey(keyboardBinding.Keycode))
                {
                    continue;
                }

                if (!UnityEngine.Input.GetKey(keyboardBinding.Keycode))
                {
                    continue;
                }

                bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
                bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                bool alt = UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);
                if (ctrl || shift || alt)
                {
                    continue;
                }

                int candidateFrame;
                if (UnityEngine.Input.GetKeyDown(keyboardBinding.Keycode) ||
                    (_unmodifiedChordCandidateFrames.TryGetValue(keyboardBinding.Keycode, out candidateFrame) &&
                    UnityEngine.Time.frameCount <= candidateFrame))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasModifiedBindingForKey(KeyCode keycode)
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                IReadOnlyList<InputBinding> bindings = _actions[i].Bindings;
                for (int j = 0; j < bindings.Count; j++)
                {
                    KeyboardBinding keyboardBinding = bindings[j] as KeyboardBinding;
                    if (keyboardBinding != null &&
                        keyboardBinding.Keycode == keycode &&
                        (keyboardBinding.Ctrl || keyboardBinding.Shift || keyboardBinding.Alt))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void CaptureKeyboardBinding()
        {
            if (!UnityEngine.Input.anyKeyDown)
            {
                return;
            }

            PruneReleasedListenInitialKeys();
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (!UnityEngine.Input.GetKeyDown(keyCode) ||
                    _listenInitialHeldKeys.Contains(keyCode) ||
                    !IsBindableKeyboardKey(keyCode))
                {
                    continue;
                }

                bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
                bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                bool alt = UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);
                KeyboardBinding binding = new KeyboardBinding(keyCode, ctrl, shift, alt);
                SuppressBindingUntilRelease(binding);
                _listenCallback?.Invoke(binding);
                return;
            }
        }

        private static void CaptureControllerBinding()
        {
            if (_listenControllerModifier != null)
            {
                ControllerInput modifier = _listenControllerModifier.Value;
                if (!_heldControllerInputs.Contains(modifier))
                {
                    _listenControllerModifier = null;
                    ControllerBinding binding = new ControllerBinding(modifier);
                    SuppressBindingUntilRelease(binding);
                    _listenCallback?.Invoke(binding);
                    return;
                }

                ControllerInput? chordInput = FirstJustPressedControllerInputExcept(modifier);
                if (chordInput != null)
                {
                    _listenControllerModifier = null;
                    ControllerBinding binding = new ControllerBinding(chordInput.Value, modifier);
                    SuppressBindingUntilRelease(binding);
                    _listenCallback?.Invoke(binding);
                }
                return;
            }

            ControllerInput? pressed = FirstJustPressedControllerInput();
            if (pressed == null)
            {
                return;
            }

            ControllerInput input = pressed.Value;
            ControllerInput? heldModifier = FirstHeldControllerModifierExcept(input);
            if (heldModifier != null)
            {
                ControllerBinding binding = new ControllerBinding(input, heldModifier.Value);
                SuppressBindingUntilRelease(binding);
                _listenCallback?.Invoke(binding);
                return;
            }

            if (IsControllerModifier(input))
            {
                _listenControllerModifier = input;
                return;
            }

            ControllerBinding soloBinding = new ControllerBinding(input);
            SuppressBindingUntilRelease(soloBinding);
            _listenCallback?.Invoke(soloBinding);
        }

        public static void SuppressBindingUntilRelease(InputBinding binding)
        {
            KeyboardBinding keyboardBinding = binding as KeyboardBinding;
            if (keyboardBinding != null)
            {
                _suppressedKeysUntilRelease.Add(keyboardBinding.Keycode);
                return;
            }

            ControllerBinding controllerBinding = binding as ControllerBinding;
            if (controllerBinding != null)
            {
                _suppressedControllerInputsUntilRelease.Add(controllerBinding.Input);
                if (controllerBinding.Modifier != null)
                {
                    _suppressedControllerInputsUntilRelease.Add(controllerBinding.Modifier.Value);
                }
            }
        }

        private static ControllerInput? FirstJustPressedControllerInput()
        {
            foreach (ControllerInput input in System.Enum.GetValues(typeof(ControllerInput)))
            {
                if (_justPressedControllerInputs.Contains(input) && !_listenInitialHeldControllerInputs.Contains(input))
                {
                    return input;
                }
            }

            return null;
        }

        private static ControllerInput? FirstJustPressedControllerInputExcept(ControllerInput except)
        {
            foreach (ControllerInput input in System.Enum.GetValues(typeof(ControllerInput)))
            {
                if (input != except &&
                    _justPressedControllerInputs.Contains(input) &&
                    !_listenInitialHeldControllerInputs.Contains(input))
                {
                    return input;
                }
            }

            return null;
        }

        private static ControllerInput? FirstHeldControllerModifierExcept(ControllerInput except)
        {
            foreach (ControllerInput input in System.Enum.GetValues(typeof(ControllerInput)))
            {
                if (input != except &&
                    IsControllerModifier(input) &&
                    _heldControllerInputs.Contains(input) &&
                    !_listenInitialHeldControllerInputs.Contains(input))
                {
                    return input;
                }
            }

            return null;
        }

        private static bool IsControllerModifier(ControllerInput input)
        {
            switch (input)
            {
                case ControllerInput.LeftShoulder:
                case ControllerInput.RightShoulder:
                case ControllerInput.LeftTrigger:
                case ControllerInput.RightTrigger:
                    return true;
                default:
                    return false;
            }
        }

        private static void SnapshotListenInitialHeldKeys()
        {
            _listenInitialHeldKeys.Clear();
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (UnityEngine.Input.GetKey(keyCode))
                {
                    _listenInitialHeldKeys.Add(keyCode);
                }
            }
        }

        private static void PruneReleasedListenInitialKeys()
        {
            KeyCode[] keys = new KeyCode[_listenInitialHeldKeys.Count];
            _listenInitialHeldKeys.CopyTo(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                if (!UnityEngine.Input.GetKey(keys[i]))
                {
                    _listenInitialHeldKeys.Remove(keys[i]);
                }
            }
        }

        private static bool IsBindableKeyboardKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.None:
                case KeyCode.Mouse0:
                case KeyCode.Mouse1:
                case KeyCode.Mouse2:
                case KeyCode.Mouse3:
                case KeyCode.Mouse4:
                case KeyCode.Mouse5:
                case KeyCode.Mouse6:
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                case KeyCode.LeftApple:
                case KeyCode.RightApple:
                case KeyCode.LeftWindows:
                case KeyCode.RightWindows:
                    return false;
                default:
                    return keyCode < KeyCode.JoystickButton0;
            }
        }

        private static ControllerInput? ToControllerInput(CoreInputControlMapping mapping)
        {
            if (mapping == null || mapping.deviceID != InputDeviceType.Gamepad)
            {
                return null;
            }

            if (mapping.inputTypeID == InputType.Button)
            {
                switch (mapping.padButtonID)
                {
                    case GamepadButton.Action1: return ControllerInput.A;
                    case GamepadButton.Action2: return ControllerInput.B;
                    case GamepadButton.Action3: return ControllerInput.X;
                    case GamepadButton.Action4: return ControllerInput.Y;
                    case GamepadButton.LeftBumper: return ControllerInput.LeftShoulder;
                    case GamepadButton.RightBumper: return ControllerInput.RightShoulder;
                    case GamepadButton.LeftStickButton: return ControllerInput.LeftStickClick;
                    case GamepadButton.RightStickButton: return ControllerInput.RightStickClick;
                    case GamepadButton.Start: return ControllerInput.Start;
                    case GamepadButton.Back: return ControllerInput.Back;
                    case GamepadButton.DpadUp: return ControllerInput.DpadUp;
                    case GamepadButton.DpadDown: return ControllerInput.DpadDown;
                    case GamepadButton.DpadLeft: return ControllerInput.DpadLeft;
                    case GamepadButton.DpadRight: return ControllerInput.DpadRight;
                }
            }

            if (mapping.inputTypeID != InputType.Axis)
            {
                return null;
            }

            switch (mapping.padAxisID)
            {
                case GamepadAxis.LeftStickY:
                    return mapping.inputFilterID == InputFilter.PositiveAxis ? ControllerInput.LeftStickUp : ControllerInput.LeftStickDown;
                case GamepadAxis.LeftStickX:
                    return mapping.inputFilterID == InputFilter.PositiveAxis ? ControllerInput.LeftStickRight : ControllerInput.LeftStickLeft;
                case GamepadAxis.RightStickY:
                    return mapping.inputFilterID == InputFilter.PositiveAxis ? ControllerInput.RightStickUp : ControllerInput.RightStickDown;
                case GamepadAxis.RightStickX:
                    return mapping.inputFilterID == InputFilter.PositiveAxis ? ControllerInput.RightStickRight : ControllerInput.RightStickLeft;
                case GamepadAxis.LeftTrigger:
                    return ControllerInput.LeftTrigger;
                case GamepadAxis.RightTrigger:
                    return ControllerInput.RightTrigger;
                default:
                    return null;
            }
        }
    }
}
