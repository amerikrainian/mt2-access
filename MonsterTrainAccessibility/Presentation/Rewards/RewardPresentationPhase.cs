using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using MonsterTrainAccessibility.Util;

namespace MonsterTrainAccessibility.Presentation.Rewards
{
    internal sealed class RewardPresentationPhase : IPhase<RewardPresentationSource>
    {
        public bool Matches(PresentationContext<RewardPresentationSource> ctx)
        {
            return ctx?.Source?.Reward != null;
        }

        public void Apply(PresentationContext<RewardPresentationSource> ctx, PresentationBuilder builder)
        {
            RewardPresentationSource source = ctx.Source;
            GrantableRewardData reward = source.Reward;
            builder.SetTitle(ProxyRewardItem.Label(reward));
            builder.SetSubtitle(ProxyRewardItem.Detail(reward));
            for (int i = 0; i < source.ContextLines.Count; i++)
            {
                builder.AddSection(SectionKind.Annotation, null, source.ContextLines[i]);
            }
            builder.SetDescription(ProxyRewardItem.Description(reward));
            for (int i = 0; i < source.Tooltips.Count; i++)
            {
                TooltipContent tooltip = source.Tooltips[i];
                builder.AddSection(
                    SectionKind.Tooltip,
                    MessageList.TooltipTitle(tooltip),
                    Message.FromText(tooltip.body),
                    MessageList.TooltipKey(tooltip));
            }
        }
    }
}
