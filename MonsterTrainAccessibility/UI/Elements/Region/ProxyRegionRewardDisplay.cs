using System;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRegionRewardDisplay : GameObjectElement
    {
        private readonly RegionSelectionRewardDisplay _display;
        private readonly Func<MapNodeData> _reward;

        public ProxyRegionRewardDisplay(RegionSelectionRewardDisplay display, Func<MapNodeData> reward)
            : base(display?.RewardSelectable?.gameObject, typeKey: null, label: null)
        {
            _display = display;
            _reward = reward;
        }

        public override bool IsVisible =>
            _display != null &&
            _display.gameObject.activeInHierarchy &&
            _reward?.Invoke() != null;

        public override Message GetLabel()
        {
            return Message.Localized("ui", "REGION_SELECTION.REWARD", new
            {
                reward = Message.Clean(_reward?.Invoke()?.GetTooltipTitle())
            });
        }

        public override Message GetStatusString()
        {
            return _reward?.Invoke()?.GetIsSoulSaviorUpgradedNode() == true
                ? Message.Localized("ui", "REGION_SELECTION.UPGRADED")
                : null;
        }

        public override Message GetTooltip()
        {
            return Message.FromText(_reward?.Invoke()?.GetTooltipBody());
        }

        public override bool Activate()
        {
            SelectForNavigation();
            return true;
        }
    }
}
