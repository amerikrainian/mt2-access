using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumEndlessRecord : ProxyElement, INavigationTargetElement
    {
        private static readonly FieldInfo CountLabelField = AccessTools.Field(typeof(global::ChecklistEndlessUI), "countLabel")!;
        private static readonly FieldInfo TooltipProviderField = AccessTools.Field(typeof(global::ChecklistEndlessUI), "tooltipProvider")!;

        private readonly global::ChecklistEndlessUI _endless;

        public ProxyCompendiumEndlessRecord(global::ChecklistEndlessUI endless)
            : base(endless != null ? endless.gameObject : null)
        {
            _endless = endless;
        }

        public override bool IsVisible => _endless != null && _endless.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            TooltipProviderComponent provider = ReflectionUtil.Get<TooltipProviderComponent>(_endless, TooltipProviderField);
            return Message.FromText(TooltipText.FirstTitle(provider))
                ?? Screens.CompendiumScreen.TextOrNull(ReflectionUtil.Get<TMP_Text>(_endless, CountLabelField))
                ?? Message.Localized("ui", "COMPENDIUM.CHECKLIST.ENDLESS_RECORD");
        }

        public override Message GetTooltip()
        {
            TooltipProviderComponent provider = ReflectionUtil.Get<TooltipProviderComponent>(_endless, TooltipProviderField);
            return provider != null ? TooltipText.ForComponent(provider) : null;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
