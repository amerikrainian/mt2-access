using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Events
{
    [EventSettings("character.status", HasSourceFilter = true)]
    internal sealed class StatusChangedEvent : GameEvent
    {
        private readonly string _characterName;
        private readonly string _statusId;
        private readonly int _oldCount;
        private readonly int _newCount;

        public StatusChangedEvent(CharacterState character, string statusId, int oldCount, int newCount)
            : base(EventSource.FromCharacter(character))
        {
            _characterName = Message.RawCleaned(character?.GetName())?.Resolve();
            _statusId = statusId;
            _oldCount = oldCount;
            _newCount = newCount;
        }

        public override Message GetMessage()
        {
            Message status = StatusName(_statusId);
            if (status == null)
            {
                return null;
            }

            int delta = _newCount - _oldCount;
            if (delta > 0)
            {
                return Message.Localized("events", "STATUS.INCREASED", new { character = _characterName, status = status.Resolve(), amount = delta });
            }

            if (delta < 0)
            {
                return Message.Localized("events", "STATUS.DECREASED", new { character = _characterName, status = status.Resolve(), amount = -delta });
            }

            return null;
        }

        private static Message StatusName(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                return null;
            }

            try
            {
                return Message.RawCleaned(StatusEffectManager.GetLocalizedName(statusId, 1, false));
            }
            catch (System.Exception ex)
            {
                Core.Log.Info("[AccessibilityMod] Failed to localize status effect " + statusId + ": " + ex);
                return Message.RawCleaned(statusId);
            }
        }
    }
}
