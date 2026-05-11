using System;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation.Relics;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyMutatorButton : GameObjectElement
    {
        private readonly MutatorButtonUI _mutator;
        private readonly Func<bool> _activate;
        private readonly bool _buttonRoleRequiresInteractable;

        public ProxyMutatorButton(
            MutatorButtonUI mutator,
            Func<bool> activate = null,
            bool buttonRoleRequiresInteractable = false)
            : base(
                mutator?.Button != null ? mutator.Button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _mutator = mutator;
            _activate = activate;
            _buttonRoleRequiresInteractable = buttonRoleRequiresInteractable;
        }

        public override bool IsVisible => _mutator != null && _mutator.gameObject.activeInHierarchy && _mutator.MutatorState != null;

        public override string GetTypeKey()
        {
            if (_buttonRoleRequiresInteractable && _mutator?.Button?.interactable != true)
            {
                return null;
            }

            return base.GetTypeKey();
        }

        public override Message GetLabel() => Message.FromText(_mutator?.MutatorState?.GetName());
        public override Message GetStatusString() => _mutator?.Chosen == true ? Message.Localized("messages", "state.selected") : null;
        public override Message GetTooltip() => Message.FromText(_mutator?.MutatorState?.GetDescription());

        public override bool Activate()
        {
            return _activate != null ? _activate() : base.Activate();
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<RelicPresentationSource> buffer = buffers?.GetBuffer("relic") as PresentationBuffer<RelicPresentationSource>;
            if (buffer == null || _mutator?.MutatorState == null)
            {
                return "ui";
            }

            buffer.Bind(RelicPresentationSource.FromState(_mutator.MutatorState, includeDynamicInfo: false));
            buffers.EnableBuffer("relic", true);
            return "relic";
        }
    }
}
