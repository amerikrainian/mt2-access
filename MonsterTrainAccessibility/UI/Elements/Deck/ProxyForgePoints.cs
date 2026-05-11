using System;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyForgePoints : GameObjectElement
    {
        private readonly global::ForgePointsUI _forge;
        private readonly GameUISelectableButton _button;
        private readonly Func<bool> _activate;

        public ProxyForgePoints(global::ForgePointsUI forge, GameUISelectableButton button, Func<bool> activate = null)
            : base(
                target: button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _forge = forge;
            _button = button;
            _activate = activate;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.FORGE_POINTS");
        public override Message GetStatusString()
        {
            SaveManager saveManager = GameManagers.GetSaveManager();
            Message count = Message.Localized("ui", "HUD.COUNT", new { count = saveManager?.GetForgePoints() ?? 0 });
            Message toggle = saveManager != null
                ? Message.Localized("messages", saveManager.IsForgeToggleActive() ? "state.on" : "state.off")
                : null;
            return Message.Join(", ", count, toggle);
        }

        public override Message GetTooltip() => _forge != null ? TooltipText.ForComponent(_forge) : null;

        public override bool Activate()
        {
            return _activate != null ? _activate() : base.Activate();
        }
    }
}
