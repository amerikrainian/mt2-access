using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxySoulHudItem : GameObjectElement
    {
        private readonly global::SoulHudItemUI _soulItem;

        public ProxySoulHudItem(global::SoulHudItemUI soulItem)
            : base(
                target: soulItem?.GetSoulItemButton()?.gameObject,
                typeKey: "button",
                label: null)
        {
            _soulItem = soulItem;
        }

        public override bool IsVisible => _soulItem != null && _soulItem.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.RawCleaned(_soulItem?.GetSoulState()?.GetName());
        public override Message GetTooltip() => _soulItem != null ? TooltipText.ForComponent(_soulItem) : null;
    }
}
