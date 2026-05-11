using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyIntroCurrentUserButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;

        public ProxyIntroCurrentUserButton(GameUISelectableButton button)
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
            return new Message("menu.main.current_user_only");
        }

        public override Message GetStatusString()
        {
            return Message.RawCleaned(AppManager.PlatformServices.GetPlayerDisplayName());
        }
    }
}
