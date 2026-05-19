using System;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "creature",
        PresentationSlot.Title,
        PresentationSlot.Subtitle,
        PresentationSlot.Description,
        PresentationSlot.Tooltip,
        PresentationSlot.Annotation)]
    internal sealed class CreatureProfile
    {
        public static Type SourceType => typeof(CharacterState);
    }
}
