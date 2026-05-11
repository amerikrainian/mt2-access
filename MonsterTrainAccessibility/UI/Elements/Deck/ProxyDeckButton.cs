using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDeckButton : GameObjectElement
    {
        private readonly global::DeckCountUI _deck;

        public ProxyDeckButton(global::DeckCountUI deck)
            : base(
                target: deck != null ? deck.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _deck = deck;
        }

        public override bool IsVisible => _deck != null && _deck.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized("ui", "HUD.DECK");
        public override Message GetStatusString() => Message.Localized("ui", "HUD.COUNT", new { count = GameManagers.GetSaveManager()?.GetVisibleDeckCount() ?? 0 });
        public override Message GetTooltip() => _deck != null ? TooltipText.ForComponent(_deck) : null;
    }
}
