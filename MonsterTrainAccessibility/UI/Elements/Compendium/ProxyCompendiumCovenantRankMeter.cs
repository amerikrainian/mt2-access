using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Screens;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumCovenantRankMeter : ProxyElement, INavigationTargetElement
    {
        private static readonly FieldInfo TooltipProviderField = AccessTools.Field(typeof(global::CovenantRankMeter), "tooltipProvider")!;

        private readonly global::CovenantRankMeter _meter;

        public ProxyCompendiumCovenantRankMeter(global::CovenantRankMeter meter)
            : base(meter != null ? meter.gameObject : null)
        {
            _meter = meter;
        }

        public override bool IsVisible => _meter != null && _meter.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            TooltipProviderComponent provider = ReflectionUtil.Get<TooltipProviderComponent>(_meter, TooltipProviderField);
            return Message.FromText(TooltipText.FirstTitle(provider))
                ?? Message.Localized("ui", "COMPENDIUM.CHECKLIST.COVENANT_RANK");
        }

        public override Message GetTooltip()
        {
            TooltipProviderComponent provider = ReflectionUtil.Get<TooltipProviderComponent>(_meter, TooltipProviderField);
            return provider != null ? TooltipText.ForComponent(provider) : null;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
