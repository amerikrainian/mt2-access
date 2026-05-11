using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Relics;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRelicIcon : GameObjectElement
    {
        private readonly RelicTooltipProvider _relic;

        public ProxyRelicIcon(RelicTooltipProvider relic)
            : base(
                relic?.SelectableUI,
                typeKey: "button",
                label: null)
        {
            _relic = relic;
        }

        public override bool IsVisible => _relic != null && _relic.gameObject.activeInHierarchy;
        public override Message GetLabel() => ProxyRelicInfo.Label(_relic);
        public override Message GetTooltip() => ProxyRelicInfo.Tooltip(_relic);

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<RelicPresentationSource> buffer = buffers?.GetBuffer("relic") as PresentationBuffer<RelicPresentationSource>;
            if (buffer == null || _relic == null)
            {
                return "ui";
            }

            buffer.Bind(RelicPresentationSource.FromProvider(_relic));
            buffers.EnableBuffer("relic", true);
            return "relic";
        }
    }
}
