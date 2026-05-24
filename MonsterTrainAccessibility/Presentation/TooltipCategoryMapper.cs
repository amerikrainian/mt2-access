using System;

namespace MonsterTrainAccessibility.Presentation
{
    internal static class TooltipCategoryMapper
    {
        public static SectionKind ToSectionKind(TooltipCategory category)
        {
            switch (category)
            {
                case TooltipCategory.Keyword: return SectionKind.TooltipKeyword;
                case TooltipCategory.Status: return SectionKind.TooltipStatus;
                case TooltipCategory.Trigger: return SectionKind.TooltipTrigger;
                case TooltipCategory.Effect: return SectionKind.TooltipEffect;
                case TooltipCategory.Ability: return SectionKind.TooltipAbility;
                case TooltipCategory.Upgrade: return SectionKind.TooltipUpgrade;
                case TooltipCategory.Equipment: return SectionKind.TooltipEquipment;
                case TooltipCategory.RoomEffect: return SectionKind.TooltipRoomEffect;
                case TooltipCategory.GeneratedContent: return SectionKind.TooltipGeneratedContent;
                default: return SectionKind.TooltipOther;
            }
        }

        public static TooltipCategory Refine(TooltipContent tooltip, TooltipCategory fallback)
        {
            if (tooltip.IsEmpty())
            {
                return fallback;
            }

            if (tooltip.designType == TooltipDesigner.TooltipDesignType.Trigger)
            {
                return TooltipCategory.Trigger;
            }

            if (tooltip.designType == TooltipDesigner.TooltipDesignType.Ability)
            {
                return TooltipCategory.Ability;
            }

            string id = tooltip.tooltipId ?? string.Empty;
            if (Contains(id, "summoned_unit") || Contains(id, "generated"))
            {
                return TooltipCategory.GeneratedContent;
            }

            if (IsStatusId(id))
            {
                return TooltipCategory.Status;
            }

            if (tooltip.designType == TooltipDesigner.TooltipDesignType.Keyword)
            {
                return TooltipCategory.Keyword;
            }

            if (tooltip.designType == TooltipDesigner.TooltipDesignType.Equipment)
            {
                return TooltipCategory.Equipment;
            }

            if (tooltip.designType == TooltipDesigner.TooltipDesignType.StateModifier)
            {
                return TooltipCategory.Upgrade;
            }

            if (Contains(id, "room") || Contains(id, "train_room"))
            {
                return TooltipCategory.RoomEffect;
            }

            if (Contains(id, "grafted_equipment") || Contains(id, "equipment"))
            {
                return TooltipCategory.Equipment;
            }

            return fallback;
        }

        private static bool IsStatusId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            StatusEffectManager manager = StatusEffectManager.Instance;
            return manager != null && manager.GetStatusEffectDataById(id, expectToFind: false) != null;
        }

        private static bool Contains(string text, string value)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
