using System;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "card_blight",
        PresentationSlot.Title,
        PresentationSlot.Subtitle,
        PresentationSlot.Cost,
        PresentationSlot.Description,
        PresentationSlot.Upgrade,
        PresentationSlot.TooltipKeyword,
        PresentationSlot.TooltipStatus,
        PresentationSlot.TooltipTrigger,
        PresentationSlot.TooltipAbility,
        PresentationSlot.TooltipEffect,
        PresentationSlot.TooltipGeneratedContent,
        PresentationSlot.TooltipOther,
        GroupKey = "card_group",
        MatchPriority = 10)]
    internal sealed class CardBlightProfile
    {
        public static Type SourceType => typeof(CardState);

        public static bool Matches(CardState card)
        {
            return card != null && card.GetCardType() == CardType.Blight;
        }
    }
}
