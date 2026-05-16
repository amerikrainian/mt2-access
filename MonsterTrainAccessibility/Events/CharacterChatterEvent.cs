using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    internal abstract class CharacterChatterEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly string _reason;
        private readonly string _quote;

        protected CharacterChatterEvent(
            CharacterState character,
            Message reason,
            string quote)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _reason = reason?.Resolve();
            _quote = Message.FromText(quote)?.Resolve();
        }

        public override string BufferKey => "monster_quotes";

        public override Message GetMessage()
        {
            if (string.IsNullOrWhiteSpace(_characterName) || string.IsNullOrWhiteSpace(_quote))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(_reason)
                ? Message.Localized("events", "CHATTER.NO_REASON", new { character = _characterName, quote = _quote })
                : Message.Localized("events", "CHATTER.WITH_REASON", new { character = _characterName, reason = _reason, quote = _quote });
        }

        public static GameEvent Create(
            CharacterState character,
            ChatterExpressionType expressionType,
            bool hasTrigger,
            CharacterTriggerData.Trigger trigger,
            string quote)
        {
            switch (expressionType)
            {
                case ChatterExpressionType.CharacterAdded:
                    return new CharacterSummonedChatterEvent(character, quote);
                case ChatterExpressionType.CharacterAttacking:
                    return new CharacterAttackingChatterEvent(character, quote);
                case ChatterExpressionType.CharacterSlayed:
                    return new CharacterSlayChatterEvent(character, quote);
                case ChatterExpressionType.CharacterIdle:
                    return new CharacterIdleChatterEvent(character, quote);
                case ChatterExpressionType.CharacterTrigger:
                    return new CharacterTriggerChatterEvent(
                        character,
                        hasTrigger ? GameLocStrings.CharacterTriggerName(trigger) : null,
                        quote);
                default:
                    return null;
            }
        }
    }

    [EventSettings("character.chatter.summoned", defaultAnnounce: false, HasSourceFilter = true)]
    internal sealed class CharacterSummonedChatterEvent : CharacterChatterEvent
    {
        public CharacterSummonedChatterEvent(CharacterState character, string quote)
            : base(character, Message.Localized("events", "CHATTER.REASON.SUMMONED"), quote)
        {
        }
    }

    [EventSettings("character.chatter.attacking", defaultAnnounce: false, HasSourceFilter = true)]
    internal sealed class CharacterAttackingChatterEvent : CharacterChatterEvent
    {
        public CharacterAttackingChatterEvent(CharacterState character, string quote)
            : base(character, Message.Localized("events", "CHATTER.REASON.ATTACKING"), quote)
        {
        }
    }

    [EventSettings("character.chatter.slay", defaultAnnounce: false, HasSourceFilter = true)]
    internal sealed class CharacterSlayChatterEvent : CharacterChatterEvent
    {
        public CharacterSlayChatterEvent(CharacterState character, string quote)
            : base(character, Message.Localized("events", "CHATTER.REASON.SLAY"), quote)
        {
        }
    }

    [EventSettings("character.chatter.idle", defaultAnnounce: false, HasSourceFilter = true)]
    internal sealed class CharacterIdleChatterEvent : CharacterChatterEvent
    {
        public CharacterIdleChatterEvent(CharacterState character, string quote)
            : base(character, Message.Localized("events", "CHATTER.REASON.IDLE"), quote)
        {
        }
    }

    [EventSettings("character.chatter.trigger", defaultAnnounce: false, HasSourceFilter = true)]
    internal sealed class CharacterTriggerChatterEvent : CharacterChatterEvent
    {
        public CharacterTriggerChatterEvent(CharacterState character, Message reason, string quote)
            : base(character, reason, quote)
        {
        }
    }
}
