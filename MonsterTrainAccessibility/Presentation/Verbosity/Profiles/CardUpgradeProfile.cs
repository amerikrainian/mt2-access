using System;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "card_upgrade",
        PresentationSlot.Title,
        PresentationSlot.Annotation)]
    internal sealed class CardUpgradeProfile
    {
        public static Type SourceType => typeof(CardUpgradeData);
    }
}
