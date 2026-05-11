using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("card.deck_shuffled")]
    internal sealed class DeckShuffledEvent : GameEvent
    {
        public override Message GetMessage()
        {
            return Message.Localized("events", "CARD.DECK_SHUFFLED");
        }
    }
}
