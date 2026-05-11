using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;

namespace MonsterTrainAccessibility.Patches
{
    internal static class SemanticLocalizationHooks
    {
        public static bool StatusEffectsDisplayData_GetTMPSpriteTag_Prefix(global::StatusEffectsDisplayData.IconDisplayData displayData, ref string __result)
        {
            if (!AccessibilityLocalizationScope.IsActive)
            {
                return true;
            }

            string spriteName = displayData.iconSprite != null ? displayData.iconSprite.name : string.Empty;
            __result = Message.ResolveSpriteLabel(spriteName);
            return false;
        }

        public static bool TooltipUI_FormatTitleWithIcon_Prefix(string title, string icon, ref string __result)
        {
            if (!AccessibilityLocalizationScope.IsActive)
            {
                return true;
            }

            string cleanTitle = Message.NormalizeResolvedText(title);
            string cleanIcon = Message.NormalizeResolvedText(icon);

            if (!Message.ShouldAdd(cleanIcon))
            {
                __result = cleanTitle;
                return false;
            }

            if (!Message.ShouldAdd(cleanTitle))
            {
                __result = cleanIcon;
                return false;
            }

            if (string.Equals(cleanTitle, cleanIcon, System.StringComparison.OrdinalIgnoreCase))
            {
                __result = cleanTitle;
                return false;
            }

            __result = Message.JoinText(cleanIcon, cleanTitle);
            return false;
        }
    }
}
