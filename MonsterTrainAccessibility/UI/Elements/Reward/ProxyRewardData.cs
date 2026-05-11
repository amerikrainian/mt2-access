using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Rewards;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRewardData : UIElement
    {
        private readonly GameObject _target;
        private readonly GrantableRewardData _rewardData;

        public ProxyRewardData(GameObject target, GrantableRewardData rewardData)
        {
            _target = target;
            _rewardData = rewardData;
        }

        public GrantableRewardData RewardData => _rewardData;
        public override bool IsVisible => _target == null || _target.activeInHierarchy;
        public override Message GetLabel() => ProxyRewardItem.Label(_rewardData);
        public override Message GetExtrasString() => ProxyRewardItem.Detail(_rewardData);
        public override Message GetTooltip() => ProxyRewardItem.Tooltip(_rewardData);

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            if (_rewardData == null)
            {
                return "ui";
            }

            if (_rewardData is CardRewardData cardReward && BindCardPresentationBuffer(buffers, cardReward))
            {
                return "card";
            }

            PresentationBuffer<RewardPresentationSource> buffer = buffers?.GetBuffer("reward") as PresentationBuffer<RewardPresentationSource>;
            if (buffer == null)
            {
                return "ui";
            }

            buffer.Bind(new RewardPresentationSource(_rewardData));
            buffers.EnableBuffer("reward", true);
            return "reward";
        }

        private static bool BindCardPresentationBuffer(BufferManager buffers, CardRewardData reward)
        {
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            CardData cardData = reward?.GetCardData();
            if (buffer == null || cardData == null)
            {
                return false;
            }

            buffer.Bind(new CardState(cardData, reward.saveManager));
            buffers.EnableBuffer("card", true);
            return true;
        }
    }
}
