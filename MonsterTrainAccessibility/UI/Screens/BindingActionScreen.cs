using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class BindingActionScreen : Screen
    {
        private readonly BindingSetting _setting;
        private readonly InputBinding _binding;
        private readonly ListContainer _root;

        public BindingActionScreen(BindingSetting setting, InputBinding binding)
        {
            _setting = setting;
            _binding = binding;
            _root = new ListContainer
            {
                ContainerLabel = binding?.DisplayName,
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

            BuildControls();
        }

        public override string ScreenName => _binding?.DisplayName;

        public override void OnPush()
        {
            _root.FocusFirst();
        }

        public override void OnFocus()
        {
            if (_root.FocusIndex >= 0)
            {
                _root.SetFocusIndex(_root.FocusIndex);
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
                default:
                    return _root.HandleAction(action);
            }
        }

        public override bool BlocksGameInput(InputAction action) => action != null;
        public override bool ShouldAcceptGameSelection() => false;

        private void BuildControls()
        {
            _root.Add(new ActionElement(
                label: () => Message.Localized("ui", "KEYBINDINGS.REPLACE"),
                typeKey: "button",
                activate: () =>
                {
                    ScreenManager.ReplaceScreen(this, new BindingListenScreen(_setting, _binding is ControllerBinding, _binding));
                    return true;
                }));

            _root.Add(new ActionElement(
                label: () => Message.Localized("ui", "KEYBINDINGS.DELETE"),
                typeKey: "button",
                activate: () =>
                {
                    if (_setting != null && _binding != null)
                    {
                        _setting.Action.RemoveBinding(_binding);
                    }

                    ScreenManager.RemoveScreen(this);
                    SpeechManager.Output(Message.Localized("ui", "KEYBINDINGS.DELETED"));
                    return true;
                }));
        }
    }
}
