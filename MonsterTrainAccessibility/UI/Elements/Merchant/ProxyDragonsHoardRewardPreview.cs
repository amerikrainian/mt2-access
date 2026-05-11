using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Rewards;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyDragonsHoardRewardPreview : GameObjectElement
    {
        private readonly RewardItemUI _reward;
        private readonly GrantableRewardData _rewardData;
        private readonly Message _label;
        private readonly Message _tooltip;

        public ProxyDragonsHoardRewardPreview(RewardItemUI reward)
            : base(reward != null ? reward.gameObject : null, label: null)
        {
            _reward = reward;
            _rewardData = reward?.rewardState?.RewardData;
            _label = MainScreenRewardLabel(_rewardData);
            _tooltip = ProxyRewardItem.Tooltip(_rewardData, reward?.Tooltips);
        }

        public override bool IsVisible => _reward != null && _reward.gameObject.activeInHierarchy && (_label != null || _tooltip != null);
        public override Message GetLabel() => _label;
        public override Message GetTooltip() => _tooltip;

        internal override string HandleBuffers(BufferManager buffers)
        {
            LineBuffer uiBuffer = buffers?.GetBuffer("ui");
            if (uiBuffer == null)
            {
                return "ui";
            }

            uiBuffer.Clear();
            List<Message> parts = new List<Message>(PresentationRenderer.BufferLines(
                PhaseRegistry.Rewards.Build(new RewardPresentationSource(_rewardData, _reward?.Tooltips))));
            if (parts.Count == 0)
            {
                MessageList.Add(parts, _tooltip);
                MessageList.Add(parts, _label);
            }

            for (int i = 0; i < parts.Count; i++)
            {
                uiBuffer.Add(parts[i]);
            }

            buffers.EnableBuffer("ui", true);
            return "ui";
        }

        private static Message MainScreenRewardLabel(GrantableRewardData reward)
        {
            if (reward == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, ProxyRewardItem.Label(reward));
            MessageList.Add(parts, ProxyRewardItem.Detail(reward));
            if (parts.Count == 0)
            {
                MessageList.Add(parts, ProxyRewardItem.Tooltip(reward));
            }

            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }
    }
}
