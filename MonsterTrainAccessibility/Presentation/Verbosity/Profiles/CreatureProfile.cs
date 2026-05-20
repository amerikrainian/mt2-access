using System;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "creature",
        PresentationSlot.Title,
        PresentationSlot.Subtitle,
        PresentationSlot.Stats,
        PresentationSlot.Status,
        PresentationSlot.Ability,
        PresentationSlot.Trigger,
        PresentationSlot.Intent,
        PresentationSlot.Tooltip,
        PresentationSlot.Annotation)]
    internal sealed class CreatureProfile
    {
        public static Type SourceType => typeof(CharacterState);
    }
}
