using System;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "card",
        PresentationSlot.Title,
        PresentationSlot.Subtitle,
        PresentationSlot.Cost,
        PresentationSlot.Stats,
        PresentationSlot.Description,
        PresentationSlot.Upgrade,
        PresentationSlot.TooltipKeyword,
        PresentationSlot.TooltipStatus,
        PresentationSlot.TooltipTrigger,
        PresentationSlot.TooltipAbility,
        PresentationSlot.TooltipEffect,
        PresentationSlot.TooltipEquipment,
        PresentationSlot.TooltipGeneratedContent,
        PresentationSlot.TooltipOther,
        GroupKey = "card_group",
        MatchPriority = -100)]
    internal sealed class CardProfile
    {
        public static Type SourceType => typeof(CardState);
    }
}
