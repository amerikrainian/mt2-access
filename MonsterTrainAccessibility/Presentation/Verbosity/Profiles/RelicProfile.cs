using System;
using MonsterTrainAccessibility.Presentation.Relics;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "relic",
        PresentationSlot.Title,
        PresentationSlot.Description,
        PresentationSlot.Context,
        PresentationSlot.Tooltip)]
    internal sealed class RelicProfile
    {
        public static Type SourceType => typeof(RelicPresentationSource);
    }
}
