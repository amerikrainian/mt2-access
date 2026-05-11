using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumLockedCard : ProxyElement, INavigationTargetElement
    {
        private readonly global::LockedCardUI _card;

        public ProxyCompendiumLockedCard(global::LockedCardUI card)
            : base(card != null ? card.gameObject : null)
        {
            _card = card;
        }

        public override bool IsVisible => _card != null && _card.gameObject.activeInHierarchy;

        public override string GetTypeKey() => "card";

        public override Message GetLabel()
        {
            return MessageList.TooltipList(_card?.Tooltips);
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
