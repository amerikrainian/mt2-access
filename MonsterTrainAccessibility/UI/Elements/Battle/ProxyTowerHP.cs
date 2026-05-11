using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyTowerHP : GameObjectElement
    {
        private readonly global::TowerHPUI _pyre;

        public ProxyTowerHP(global::TowerHPUI pyre)
            : base(
                target: pyre != null ? pyre.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _pyre = pyre;
        }

        public override bool IsVisible => _pyre != null && _pyre.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.PYRE_HEALTH");
        public override Message GetStatusString()
        {
            SaveManager saveManager = GameManagers.GetSaveManager();
            return Message.Localized("ui", "HUD.PYRE_HEALTH_VALUE", new { hp = saveManager?.GetTowerHP() ?? 0, max = saveManager?.GetMaxTowerHP() ?? 0 });
        }

        public override Message GetTooltip() => _pyre != null ? TooltipText.ForComponent(_pyre) : null;
    }
}
