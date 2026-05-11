using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyUnitAbilityCard : GameObjectElement
    {
        private readonly global::UnitAbilityCardUI _cardUI;

        public ProxyUnitAbilityCard(global::UnitAbilityCardUI cardUI, global::ShinyShoe.IGameUIComponent selectable)
            : base(
                selectable,
                typeKey: null,
                label: null)
        {
            _cardUI = cardUI;
        }

        public CardState Card => _cardUI?.GetCardState();
        public override bool IsVisible => _cardUI != null && _cardUI.gameObject.activeInHierarchy && Card != null;
        public override Message GetLabel() => ProxyCombatCard.FocusSummary(Card);
        public override Message GetTooltip() => ProxyCombatCard.AccessibilitySummary(Card);

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            CardState card = Card;
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
