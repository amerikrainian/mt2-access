using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyStartRunOptionButton : GameObjectElement
    {
        private readonly global::GameModeOption _option;
        private readonly GameUISelectableButton _button;
        private readonly global::GameModeDisplay _display;

        public ProxyStartRunOptionButton(global::GameModeOption option, GameUISelectableButton button, global::GameModeDisplay display)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _option = option;
            _button = button;
            _display = display;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Screens.StartRunOptionsDialogScreen.ResolveOptionLabel(_option, _button, _display);
        }

        public override Message GetTooltip()
        {
            return Screens.StartRunOptionsDialogScreen.ResolveOptionDetails(_option, _display);
        }
    }
}
