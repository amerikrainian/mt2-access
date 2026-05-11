using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyPyreArmor : GameObjectElement
    {
        private readonly global::PyreArmorUI _armor;

        public ProxyPyreArmor(global::PyreArmorUI armor)
            : base(
                target: armor != null ? armor.gameObject : null,
                typeKey: null,
                label: null)
        {
            _armor = armor;
        }

        public override bool IsVisible => _armor != null && _armor.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.PYRE_ARMOR");
        public override Message GetTooltip() => _armor != null ? TooltipText.ForComponent(_armor) : null;
    }
}
