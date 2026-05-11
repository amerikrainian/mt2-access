using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Presentation.Rewards
{
    internal sealed class RewardPresentationSource
    {
        public RewardPresentationSource(
            GrantableRewardData reward,
            IEnumerable<TooltipContent> tooltips = null,
            IEnumerable<Message> contextLines = null)
        {
            Reward = reward;
            Tooltips = CopyTooltips(tooltips);
            ContextLines = CopyMessages(contextLines);
        }

        public GrantableRewardData Reward { get; }
        public IReadOnlyList<TooltipContent> Tooltips { get; }
        public IReadOnlyList<Message> ContextLines { get; }

        private static IReadOnlyList<TooltipContent> CopyTooltips(IEnumerable<TooltipContent> tooltips)
        {
            List<TooltipContent> copy = new List<TooltipContent>();
            if (tooltips == null)
            {
                return copy;
            }

            foreach (TooltipContent tooltip in tooltips)
            {
                if (!tooltip.IsEmpty())
                {
                    copy.Add(tooltip);
                }
            }

            return copy;
        }

        private static IReadOnlyList<Message> CopyMessages(IEnumerable<Message> messages)
        {
            List<Message> copy = new List<Message>();
            if (messages == null)
            {
                return copy;
            }

            foreach (Message message in messages)
            {
                if (message != null)
                {
                    copy.Add(message);
                }
            }

            return copy;
        }
    }
}
