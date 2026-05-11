using System;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation.Relics;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRegionMutatorReward : GameObjectElement
    {
        private readonly RegionSelectionRewardDisplay _display;
        private readonly Func<MutatorData> _mutator;

        public ProxyRegionMutatorReward(RegionSelectionRewardDisplay display, Func<MutatorData> mutator)
            : base(display?.RewardSelectable?.gameObject, typeKey: null, label: null)
        {
            _display = display;
            _mutator = mutator;
        }

        public override bool IsVisible =>
            _display != null &&
            _display.gameObject.activeInHierarchy &&
            _mutator?.Invoke() != null;

        public override Message GetLabel()
        {
            MutatorData mutator = _mutator?.Invoke();
            return Message.Localized("ui", "REGION_SELECTION.MUTATOR", new
            {
                mutator = mutator?.GetName() ?? string.Empty
            });
        }

        public override Message GetTooltip()
        {
            MutatorData mutator = _mutator?.Invoke();
            return mutator != null ? ProxyRelicInfo.Tooltip(new MutatorState(mutator, null), includeDynamicInfo: false) : null;
        }

        public override bool Activate()
        {
            SelectForNavigation();
            return true;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<RelicPresentationSource> buffer = buffers?.GetBuffer("relic") as PresentationBuffer<RelicPresentationSource>;
            MutatorData mutator = _mutator?.Invoke();
            if (buffer == null || mutator == null)
            {
                return "ui";
            }

            buffer.Bind(RelicPresentationSource.FromState(new MutatorState(mutator, null), includeDynamicInfo: false));
            buffers.EnableBuffer("relic", true);
            return "relic";
        }
    }
}
