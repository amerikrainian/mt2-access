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
        PresentationSlot.Tooltip)]
    internal sealed class CardProfile
    {
        public static Type SourceType => typeof(CardState);
    }
}
