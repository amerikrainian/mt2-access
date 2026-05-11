using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumWinStreak : ProxyElement, INavigationTargetElement
    {
        private static readonly FieldInfo CountLabelField = AccessTools.Field(typeof(global::WinStreakUI), "countLabel")!;
        private static readonly FieldInfo TooltipProviderField = AccessTools.Field(typeof(global::WinStreakUI), "optionalTooltipProvider")!;

        private readonly global::ChecklistWinStreakUI _streak;

        public ProxyCompendiumWinStreak(global::ChecklistWinStreakUI streak)
            : base(streak != null ? streak.gameObject : null)
        {
            _streak = streak;
        }

        public override bool IsVisible => _streak != null && _streak.ContentRootOrDefault.activeInHierarchy;

        public override Message GetLabel()
        {
            TooltipProviderComponent provider = ReflectionUtil.Get<TooltipProviderComponent>(_streak, TooltipProviderField);
            return Message.FromText(TooltipText.FirstTitle(provider))
                ?? Screens.CompendiumScreen.TextOrNull(ReflectionUtil.Get<TMP_Text>(_streak, CountLabelField))
                ?? Message.Localized("ui", "COMPENDIUM.CHECKLIST.WIN_STREAK");
        }

        public override Message GetTooltip()
        {
            TooltipProviderComponent provider = ReflectionUtil.Get<TooltipProviderComponent>(_streak, TooltipProviderField);
            return provider != null ? TooltipText.ForComponent(provider) : null;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
