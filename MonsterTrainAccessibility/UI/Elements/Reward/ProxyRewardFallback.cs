using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRewardFallback : GameObjectElement
    {
        private readonly global::RewardDetailsUI _details;

        public ProxyRewardFallback(global::RewardDetailsUI details)
            : base(
                details != null ? details.gameObject : null,
                label: null)
        {
            _details = details;
        }

        public override bool IsVisible => _details != null && _details.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.RawCleaned(_details?.RewardData?.RewardTitle);
        }

        public override Message GetTooltip()
        {
            return AccessibleScreenText.RewardDataTooltip(_details);
        }
    }
}
