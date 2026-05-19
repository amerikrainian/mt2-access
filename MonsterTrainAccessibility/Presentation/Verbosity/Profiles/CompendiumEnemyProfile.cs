using System;
using MonsterTrainAccessibility.Presentation.Compendium;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "compendium_enemy",
        PresentationSlot.Title,
        PresentationSlot.Stats,
        PresentationSlot.Description,
        PresentationSlot.Context,
        PresentationSlot.Tooltip)]
    internal sealed class CompendiumEnemyProfile
    {
        public static Type SourceType => typeof(CompendiumEnemyPresentationSource);
    }
}
