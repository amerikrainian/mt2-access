using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyCardDetailsCard : GameObjectElement
    {
        private readonly CardUI _card;

        public ProxyCardDetailsCard(CardUI card)
            : base(
                card?.SelectableUI?.component != null ? card.SelectableUI.component.gameObject : card != null ? card.gameObject : null,
                typeKey: null,
                label: null)
        {
            _card = card;
        }

        public override bool IsVisible => Target != null && Target.activeInHierarchy;

        private CardState Card => _card?.GetCardState();

        public override Message GetLabel()
        {
            return ProxyCombatCard.FocusSummary(Card);
        }

        public override Message GetTooltip()
        {
            return ProxyCombatCard.AccessibilitySummaryWithLore(Card);
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            CardState card = Card;
            if (buffer == null || card == null)
            {
                return "ui";
            }

            buffer.Bind(card, ProxyCombatCard.LoreLines(card));
            buffers.EnableBuffer("card", true);
            return "card";
        }
    }
}
