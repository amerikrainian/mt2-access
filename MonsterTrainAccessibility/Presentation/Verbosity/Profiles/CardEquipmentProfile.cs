using System;

namespace MonsterTrainAccessibility.Presentation.Verbosity.Profiles
{
    [VerbosityProfile(
        "card_equipment",
        PresentationSlot.Title,
        PresentationSlot.Subtitle,
        PresentationSlot.Cost,
        PresentationSlot.Stats,
        PresentationSlot.Description,
        PresentationSlot.Upgrade,
        PresentationSlot.Tooltip,
        GroupKey = "card_group",
        MatchPriority = 10)]
    internal sealed class CardEquipmentProfile
    {
        public static Type SourceType => typeof(CardState);

        public static bool Matches(CardState card)
        {
            return card != null && card.GetCardType() == CardType.Equipment;
        }
    }
}
