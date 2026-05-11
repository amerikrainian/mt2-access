namespace MonsterTrainAccessibility.Events
{
    internal sealed class EventSource
    {
        private EventSource(CharacterState character, CardState card, RelicState relic)
        {
            Character = character;
            Card = card;
            Relic = relic;
        }

        public CharacterState Character { get; }

        public CardState Card { get; }

        public RelicState Relic { get; }

        public bool IsPlayerControlled => IsPlayerCharacter(Character) || IsPlayerCard(Card) || IsPlayerRelic(Relic);

        public bool IsEnemyControlled => IsEnemyCharacter(Character);

        public static EventSource FromCharacter(CharacterState character)
        {
            return character != null ? new EventSource(character, null, null) : null;
        }

        public static EventSource FromCard(CardState card)
        {
            return card != null ? new EventSource(null, card, null) : null;
        }

        public static EventSource FromRelic(RelicState relic)
        {
            return relic != null ? new EventSource(null, null, relic) : null;
        }

        private static bool IsPlayerCharacter(CharacterState character)
        {
            if (character == null)
            {
                return false;
            }

            return character.IsPyreHeart() || character.GetTeamType() == Team.Type.Monsters;
        }

        private static bool IsEnemyCharacter(CharacterState character)
        {
            return character != null && character.GetTeamType() == Team.Type.Heroes;
        }

        private static bool IsPlayerCard(CardState card)
        {
            return card != null;
        }

        private static bool IsPlayerRelic(RelicState relic)
        {
            return relic != null;
        }
    }
}
