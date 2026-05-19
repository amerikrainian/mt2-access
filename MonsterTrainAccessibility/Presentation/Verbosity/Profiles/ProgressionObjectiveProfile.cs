using System;
using MonsterTrainAccessibility.Presentation.Progression;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "progression_objective",
        PresentationSlot.Title,
        PresentationSlot.Subtitle,
        PresentationSlot.Description)]
    internal sealed class ProgressionObjectiveProfile
    {
        public static Type SourceType => typeof(ProgressionObjectivePresentationSource);
    }
}
