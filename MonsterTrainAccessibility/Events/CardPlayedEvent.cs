using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("card.played")]
    internal sealed class CardPlayedEvent : GameEvent
    {
        private readonly string _cardName;

        public CardPlayedEvent(CardState card)
            : base(EventSource.FromCard(card))
        {
            _cardName = Message.RawCleaned(card?.GetTitle())?.Resolve();
        }

        public override Message GetMessage()
        {
            return Message.Localized("events", "CARD.PLAYED", new { card = _cardName });
        }
    }
}
