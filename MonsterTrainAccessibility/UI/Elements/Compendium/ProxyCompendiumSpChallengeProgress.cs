using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumSpChallengeProgress : ProxyElement, INavigationTargetElement
    {
        private static readonly FieldInfo LabelField = AccessTools.Field(typeof(global::SpChallengeProgressUI), "label")!;
        private static readonly FieldInfo TooltipProviderField = AccessTools.Field(typeof(global::SpChallengeProgressUI), "tooltipProvider")!;

        private readonly global::SpChallengeProgressUI _progress;

        public ProxyCompendiumSpChallengeProgress(global::SpChallengeProgressUI progress)
            : base(progress != null ? progress.gameObject : null)
        {
            _progress = progress;
        }

        public override bool IsVisible => _progress != null && _progress.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            TooltipProviderComponent provider = ReflectionUtil.Get<TooltipProviderComponent>(_progress, TooltipProviderField);
            return Message.FromText(TooltipText.FirstTitle(provider));
        }

        public override Message GetStatusString()
        {
            return Screens.CompendiumScreen.TextOrNull(ReflectionUtil.Get<TMP_Text>(_progress, LabelField));
        }

        public override Message GetTooltip()
        {
            TooltipProviderComponent provider = ReflectionUtil.Get<TooltipProviderComponent>(_progress, TooltipProviderField);
            return provider != null ? TooltipText.ForComponent(provider) : null;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
