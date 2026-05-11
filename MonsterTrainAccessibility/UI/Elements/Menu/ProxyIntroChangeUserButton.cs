using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyIntroChangeUserButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;

        public ProxyIntroChangeUserButton(GameUISelectableButton button)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return new Message("menu.main.button.change_user");
        }

        public override Message GetStatusString()
        {
            return new Message("menu.main.current_user", AppManager.PlatformServices.GetPlayerDisplayName());
        }
    }
}
