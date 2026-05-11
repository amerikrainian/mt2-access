using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyBattleIntroFightButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;

        public ProxyBattleIntroFightButton(GameUISelectableButton button)
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
            return Screens.BattleIntroScreen.FightButtonLabel(_button);
        }
    }
}
