using System;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "card_elixir",
        PresentationSlot.Title,
        PresentationSlot.Cost,
        PresentationSlot.Description,
        PresentationSlot.Subtitle,
        PresentationSlot.Upgrade,
        PresentationSlot.Tooltip,
        GroupKey = "card_group",
        MatchPriority = 100)]
    internal sealed class CardElixirProfile
    {
        public static Type SourceType => typeof(CardState);

        public static bool Matches(CardState card)
        {
            return card != null &&
                (card.HasTrait<CardTraitInfusion>() || card.HasTrait<CardTraitCraftedSpike>());
        }
    }
}
