using System;
using MonsterTrainAccessibility.Presentation.Rewards;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "reward",
        PresentationSlot.Title,
        PresentationSlot.Subtitle,
        PresentationSlot.Annotation,
        PresentationSlot.Description,
        PresentationSlot.Tooltip)]
    internal sealed class RewardProfile
    {
        public static Type SourceType => typeof(RewardPresentationSource);
    }
}
