using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.chatter", defaultAnnounce: false, HasSourceFilter = true)]
    internal sealed class CharacterChatterEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly string _reason;
        private readonly string _quote;

        public CharacterChatterEvent(
            CharacterState character,
            ChatterExpressionType expressionType,
            bool hasTrigger,
            CharacterTriggerData.Trigger trigger,
            string quote)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _reason = ResolveReason(expressionType, hasTrigger, trigger);
            _quote = Message.FromText(quote)?.Resolve();
        }

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

        private static string ResolveReason(
            ChatterExpressionType expressionType,
            bool hasTrigger,
            CharacterTriggerData.Trigger trigger)
        {
            Message reason = null;
            switch (expressionType)
            {
                case ChatterExpressionType.CharacterAdded:
                    reason = Message.Localized("events", "CHATTER.REASON.SUMMONED");
                    break;
                case ChatterExpressionType.CharacterAttacking:
                    reason = Message.Localized("events", "CHATTER.REASON.ATTACKING");
                    break;
                case ChatterExpressionType.CharacterSlayed:
                    reason = Message.Localized("events", "CHATTER.REASON.SLAY");
                    break;
                case ChatterExpressionType.CharacterIdle:
                    reason = Message.Localized("events", "CHATTER.REASON.IDLE");
                    break;
                case ChatterExpressionType.CharacterTrigger:
                    reason = hasTrigger ? GameLocStrings.CharacterTriggerName(trigger) : null;
                    break;
            }

            return reason?.Resolve();
        }
    }
}
