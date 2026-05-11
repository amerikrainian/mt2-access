using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDragonsHoard : GameObjectElement
    {
        private readonly global::DragonsHoardUI _hoard;
        private readonly GameUISelectableButton _button;

        public ProxyDragonsHoard(global::DragonsHoardUI hoard, GameUISelectableButton button)
            : base(
                target: button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _hoard = hoard;
            _button = button;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.DRAGONS_HOARD");
        public override Message GetStatusString()
        {
            SaveManager saveManager = GameManagers.GetSaveManager();
            return Message.Localized("ui", "HUD.DRAGONS_HOARD_VALUE", new { amount = saveManager?.GetDragonsHoardAmount() ?? 0, cap = saveManager?.GetDragonsHoardCap() ?? 0 });
        }

        public override Message GetTooltip() => _hoard != null ? TooltipText.ForComponent(_hoard) : null;
    }
}
