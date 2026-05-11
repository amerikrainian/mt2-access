using System.Collections.Generic;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class BindingListScreen : Screen
    {
        private static readonly Dictionary<string, string> PendingFocusLabels = new Dictionary<string, string>(System.StringComparer.Ordinal);
        private static readonly HashSet<string> SuppressActivationUntilNextUpdate = new HashSet<string>(System.StringComparer.Ordinal);

        private readonly BindingSetting _setting;
        private readonly ListContainer _root;

        public BindingListScreen(BindingSetting setting)
        {
            _setting = setting;
            _root = new ListContainer
            {
                ContainerLabel = setting?.Action?.Label,
                AnnounceName = true,
                AnnouncePosition = true,
                NavigationAxis = NavigationAxis.Vertical
            };
            RootElement = _root;

            ClaimAction("ui_up");
            ClaimAction("ui_down");
            ClaimAction("ui_accept");
            ClaimAction("ui_select");
            ClaimAction("ui_cancel");
            ClaimAction("mod_settings");
        }

        public override string ScreenName => _setting?.Action?.Label;

        public override void OnPush()
        {
            Rebuild();
            _root.FocusFirst();
        }

        public override void OnFocus()
        {
            string focusedLabel = TakePendingFocusLabel() ?? _root.FocusedChild?.GetLabel()?.Resolve();
            Rebuild();
            if (FocusLabel(focusedLabel))
            {
                return;
            }

            _root.FocusFirst();
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            if (action == null)
            {
                return true;
            }

            switch (action.Key)
            {
                case "ui_cancel":
                case "mod_settings":
                    ScreenManager.RemoveScreen(this);
                    SpeechManager.Output(Message.Localized("ui", "KEYBINDINGS.CLOSED"));
                    return true;
                case "ui_accept":
                case "ui_select":
                    if (ShouldSuppressActivation())
                    {
                        return true;
                    }
                    return _root.HandleAction(action);
                default:
                    return _root.HandleAction(action);
            }
        }

        public override void OnUpdate()
        {
            string key = _setting?.Action?.Key;
            if (!string.IsNullOrWhiteSpace(key))
            {
                SuppressActivationUntilNextUpdate.Remove(key);
            }
        }

        public override bool BlocksGameInput(InputAction action) => action != null;
        public override bool ShouldAcceptGameSelection() => false;

        private void Rebuild()
        {
            _root.Clear();
            if (_setting == null)
            {
                return;
            }

            IReadOnlyList<InputBinding> bindings = InputBindingOrder.Ordered(_setting.Action.Bindings);
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                _root.Add(new ActionElement(
                    label: () => Message.Raw(binding.DisplayName),
                    typeKey: "button",
                    activate: () =>
                    {
                        ScreenManager.PushScreen(new BindingActionScreen(_setting, binding));
                        return true;
                    }));
            }

            _root.Add(new ActionElement(
                label: () => Message.Localized("ui", "KEYBINDINGS.ADD_KEYBOARD"),
                typeKey: "button",
                activate: () =>
                {
                    ScreenManager.PushScreen(new BindingListenScreen(_setting, isController: false, replacing: null));
                    return true;
                }));

            _root.Add(new ActionElement(
                label: () => Message.Localized("ui", "KEYBINDINGS.ADD_CONTROLLER"),
                typeKey: "button",
                activate: () =>
                {
                    ScreenManager.PushScreen(new BindingListenScreen(_setting, isController: true, replacing: null));
                    return true;
                }));

            _root.Add(new ActionElement(
                label: () => Message.Localized("ui", "KEYBINDINGS.RESET"),
                typeKey: "button",
                activate: () =>
                {
                    _setting.ResetToDefault();
                    Rebuild();
                    _root.FocusFirst();
                    SpeechManager.Output(Message.Localized("ui", "KEYBINDINGS.RESET_DONE"));
                    return true;
                }));
        }

        private bool FocusLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            for (int i = 0; i < _root.Children.Count; i++)
            {
                string candidate = _root.Children[i]?.GetLabel()?.Resolve();
                if (string.Equals(candidate, label, System.StringComparison.Ordinal))
                {
                    _root.SetFocusIndex(i);
                    return true;
                }
            }

            return false;
        }

        internal static void ReturningFromListen(BindingSetting setting, InputBinding binding)
        {
            string key = setting?.Action?.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (binding != null)
            {
                PendingFocusLabels[key] = binding.DisplayName;
                SuppressActivationUntilNextUpdate.Add(key);
            }
        }

        private string TakePendingFocusLabel()
        {
            string key = _setting?.Action?.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            string label;
            if (!PendingFocusLabels.TryGetValue(key, out label))
            {
                return null;
            }

            PendingFocusLabels.Remove(key);
            return label;
        }

        private bool ShouldSuppressActivation()
        {
            string key = _setting?.Action?.Key;
            if (string.IsNullOrWhiteSpace(key) || !SuppressActivationUntilNextUpdate.Contains(key))
            {
                return false;
            }

            SuppressActivationUntilNextUpdate.Remove(key);
            return true;
        }
    }
}
