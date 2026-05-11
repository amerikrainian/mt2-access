using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class CompendiumCardElement : UIElement, INavigationTargetElement
    {
        private readonly global::CardUI _cardUI;

        public CompendiumCardElement(global::CardUI cardUI)
        {
            _cardUI = cardUI;
        }

        public override bool IsVisible => _cardUI != null && _cardUI.gameObject.activeInHierarchy;

        public global::CardState Card => _cardUI?.GetCardState();

        public override Message GetLabel() => ProxyCombatCard.FocusSummary(Card);

        public override Message GetTooltip() => ProxyCombatCard.AccessibilitySummaryWithLore(Card);

        public void SelectForNavigation()
        {
            CompendiumScreen.ClearGameSelection();
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            global::CardState card = Card;
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
