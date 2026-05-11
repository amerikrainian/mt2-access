using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;
using MonsterTrainAccessibility.Speech;
using ModInputManager = MonsterTrainAccessibility.Input.InputManager;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class BindingListenScreen : Screen
    {
        private readonly BindingSetting _setting;
        private readonly bool _isController;
        private readonly InputBinding _replacing;
        private bool _listening;

        public BindingListenScreen(BindingSetting setting, bool isController, InputBinding replacing)
        {
            _setting = setting;
            _isController = isController;
            _replacing = replacing;
            ClaimAllActions();
        }

        public override string ScreenName => _setting?.Action?.Label;

        public override void OnPush()
        {
            _listening = true;
            ModInputManager.StartListening(OnInputCaptured);
            SpeechManager.Output(Message.Localized("ui", _isController ? "KEYBINDINGS.PRESS_BUTTON" : "KEYBINDINGS.PRESS_KEY"));
        }

        public override void OnPop()
        {
            _listening = false;
            ModInputManager.StopListening();
        }

        public override bool OnActionJustPressed(InputAction action) => true;
        public override bool OnActionPressed(InputAction action) => true;
        public override bool OnActionJustReleased(InputAction action) => true;
        public override bool BlocksGameInput(InputAction action) => true;
        public override bool ShouldAcceptGameSelection() => false;

        private void OnInputCaptured(InputBinding binding)
        {
            if (!_listening || binding == null)
            {
                return;
            }

            KeyboardBinding keyboard = binding as KeyboardBinding;
            if (keyboard != null && keyboard.Keycode == UnityEngine.KeyCode.Escape)
            {
                ModInputManager.SuppressBindingUntilRelease(binding);
                Close(Message.Localized("ui", "KEYBINDINGS.CANCELLED"));
                return;
            }

            if (!AcceptsBindingType(binding))
            {
                return;
            }

            InputAction conflict = FindConflict(binding);
            if (conflict != null)
            {
                Close(Message.Localized("ui", "KEYBINDINGS.ALREADY_BOUND", new
                {
                    key = binding.DisplayName,
                    action = conflict.Label
                }));
                return;
            }

            if (_setting != null)
            {
                if (_replacing != null)
                {
                    _setting.Action.RemoveBinding(_replacing);
                }

                _setting.Action.AddBinding(binding);
                BindingListScreen.ReturningFromListen(_setting, binding);
            }

            Close(Message.Localized("ui", "KEYBINDINGS.BOUND_TO", new { key = binding.DisplayName }));
        }

        private bool AcceptsBindingType(InputBinding binding)
        {
            return _isController ? binding is ControllerBinding : binding is KeyboardBinding;
        }

        private InputAction FindConflict(InputBinding binding)
        {
            foreach (InputAction action in ModInputManager.Actions)
            {
                if (!InputBindingSettings.IsModBindableAction(action))
                {
                    continue;
                }

                for (int i = 0; i < action.Bindings.Count; i++)
                {
                    InputBinding existing = action.Bindings[i];
                    if (ReferenceEquals(action, _setting?.Action) &&
                        _replacing != null &&
                        InputBindingComparer.Same(existing, _replacing))
                    {
                        continue;
                    }

                    if (InputBindingComparer.Same(existing, binding))
                    {
                        return action;
                    }
                }
            }

            return null;
        }

        private void Close(Message message)
        {
            _listening = false;
            ModInputManager.StopListening();
            ScreenManager.RemoveScreen(this);
            SpeechManager.Output(message);
        }
    }
}
