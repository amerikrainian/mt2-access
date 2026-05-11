using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyMainMenuButton : GameObjectElement
    {
        private readonly global::MainMenuScreen _screen;
        private readonly global::MainMenuButton _menuButton;
        private readonly bool _requiresDlc;

        public ProxyMainMenuButton(global::MainMenuScreen screen, global::MainMenuButton menuButton, bool requiresDlc)
            : base(
                menuButton?.Button != null ? menuButton.Button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _screen = screen;
            _menuButton = menuButton;
            _requiresDlc = requiresDlc;
        }

        public override bool IsVisible => _menuButton != null && _menuButton.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Screens.MainMenuScreen.ResolveMenuButtonLabelMessage(_screen, _menuButton);
        }

        public override Message GetStatusString()
        {
            return Screens.MainMenuScreen.ResolveMenuButtonState(_menuButton, _requiresDlc);
        }
    }
}
