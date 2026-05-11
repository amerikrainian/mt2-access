using MonsterTrainAccessibility.UI.Screens;
using System;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumUpgradeLevelNode : ProxyCompendiumGameButton
    {
        private readonly global::UpgradeLevelNode _node;
        private readonly global::CardUpgradeData _upgrade;
        private readonly int _level;
        private readonly Func<bool> _isDiscovered;

        public ProxyCompendiumUpgradeLevelNode(
            global::UpgradeLevelNode node,
            global::CardUpgradeData upgrade,
            int level,
            Func<bool> isDiscovered)
            : base(node?.Button)
        {
            _node = node;
            _upgrade = upgrade;
            _level = level;
            _isDiscovered = isDiscovered;
        }

        public override bool IsVisible => _node != null && _node.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.Localized("ui", "COMPENDIUM.CHAMP_UPGRADES.LEVEL", new { level = _level });
        }

        public override Message GetStatusString()
        {
            return IsDiscovered() ? null : Message.Localized("messages", "state.locked");
        }

        public override Message GetTooltip()
        {
            return IsDiscovered() ? new ProxyCardUpgrade(_upgrade).GetTooltip() : null;
        }

        private bool IsDiscovered()
        {
            return _isDiscovered == null || _isDiscovered();
        }
    }
}
