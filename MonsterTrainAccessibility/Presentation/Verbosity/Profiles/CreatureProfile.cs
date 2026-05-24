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
        PresentationSlot.TooltipKeyword,
        PresentationSlot.TooltipStatus,
        PresentationSlot.TooltipTrigger,
        PresentationSlot.TooltipAbility,
        PresentationSlot.TooltipEffect,
        PresentationSlot.TooltipEquipment,
        PresentationSlot.TooltipRoomEffect,
        PresentationSlot.TooltipOther,
        PresentationSlot.Annotation)]
    internal sealed class CreatureProfile
    {
        public static Type SourceType => typeof(CharacterState);
    }
}
