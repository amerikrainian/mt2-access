using System;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.Localization
{
    internal static class GameLocStrings
    {
        public static Message StatusName(string statusId, int stackCount = 1, bool showStacks = false)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                return null;
            }

            return Message.FromText(AccessibilityLocalizationScope.Run(() => global::StatusEffectManager.GetLocalizedName(
                statusId,
                stackCount,
                inBold: false,
                showStacks: showStacks)));
        }

        public static Message CharacterTriggerName(global::CharacterTriggerData.Trigger trigger)
        {
            string keyword = AccessibilityLocalizationScope.Run(() =>
                global::CharacterTriggerData.GetKeywordText(trigger, inBold: false));
            return string.IsNullOrWhiteSpace(keyword) ? null : Message.FromText(keyword);
        }

        public static Message CharacterTriggerName(string triggerId)
        {
            return Enum.TryParse(triggerId, out global::CharacterTriggerData.Trigger trigger)
                ? CharacterTriggerName(trigger)
                : null;
        }

        public static Message CardTriggerName(global::CardTriggerType trigger)
        {
            string localizedName = AccessibilityLocalizationScope.Run(() =>
            {
                global::CardTriggerTypeMethods.GetLocalizedName(trigger, out string resolvedName, inBold: false);
                return resolvedName;
            });
            return Message.FromText(localizedName);
        }

        public static Message CardTriggerName(string triggerId)
        {
            return Enum.TryParse(triggerId, out global::CardTriggerType trigger)
                ? CardTriggerName(trigger)
                : null;
        }
    }
}
