using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyUnlockContent : GameObjectElement
    {
        private readonly global::UnlockScreen.UnlockDisplayData _item;
        private readonly RewardDetailsUI _rewardDetails;

        public ProxyUnlockContent(global::UnlockScreen.UnlockDisplayData item, RewardDetailsUI rewardDetails)
            : base(rewardDetails != null ? rewardDetails.gameObject : null, label: null)
        {
            _item = item;
            _rewardDetails = rewardDetails;
        }

        public override bool IsVisible => (_rewardDetails == null || _rewardDetails.gameObject.activeInHierarchy) &&
            (_item?.unlockedCardData != null || _item?.unlockedRelicData != null);

        public override Message GetLabel()
        {
            if (_item?.unlockedCardData != null)
            {
                return Message.RawCleaned(_item.unlockedCardData.GetName());
            }

            return _item?.unlockedRelicData != null
                ? Message.RawCleaned(_item.unlockedRelicData.GetName())
                : null;
        }

        public override Message GetTooltip()
        {
            if (_item?.unlockedCardData != null)
            {
                return ProxyCombatCard.Description(NewCardState(_item.unlockedCardData));
            }

            return _item?.unlockedRelicData != null
                ? ProxyRelicInfo.FromData(_item.unlockedRelicData, includeDynamicInfo: true)
                : null;
        }

        internal static CardState NewCardState(CardData card)
        {
            SaveManager saveManager = AllGameManagers.Instance?.OrNull()?.GetSaveManager();
            return card != null ? new CardState(card, saveManager) : null;
        }
    }
}
