using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGoldUI : GameObjectElement
    {
        private readonly global::GoldUI _gold;

        public ProxyGoldUI(global::GoldUI gold)
            : base(
                target: gold != null ? gold.gameObject : null,
                typeKey: null,
                label: null)
        {
            _gold = gold;
        }

        public override bool IsVisible => _gold != null && _gold.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.GOLD");
        public override Message GetStatusString() => Message.Localized("ui", "HUD.COUNT", new { count = GameManagers.GetSaveManager()?.GetGold() ?? 0 });
        public override Message GetTooltip() => _gold != null ? TooltipText.ForComponent(_gold) : null;
    }
}
