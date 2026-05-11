using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChampionUpgradeChoice : GameObjectElement
    {
        private readonly UpgradeCardChoiceItem _item;

        public ProxyChampionUpgradeChoice(UpgradeCardChoiceItem item)
            : base(
                item?.SelectableUI,
                typeKey: "card",
                label: null)
        {
            _item = item;
        }

        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Screens.ChampionUpgradeScreen.UpgradeFocusSummary(_item);
        }

        public override Message GetTooltip()
        {
            return Screens.ChampionUpgradeScreen.UpgradeTooltip(_item);
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            CardState card = _item?.upgradeData?.postCardState;
            if (buffer == null || card == null)
            {
                return "ui";
            }

            buffer.Bind(card);
            buffers.EnableBuffer("card", true);
            return "card";
        }
    }
}
