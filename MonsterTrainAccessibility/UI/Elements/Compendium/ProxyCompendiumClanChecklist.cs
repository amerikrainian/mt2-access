using MonsterTrainAccessibility.UI.Screens;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumClanChecklist : ProxyElement, INavigationTargetElement
    {
        private static readonly FieldInfo ClassDataField = AccessTools.Field(typeof(global::ClanChecklistSection), "classData")!;
        private static readonly FieldInfo CurrentLevelField = AccessTools.Field(typeof(global::ClanChecklistSection), "currentLevel")!;
        private static readonly FieldInfo MaxLevelField = AccessTools.Field(typeof(global::ClanChecklistSection), "maxLevel")!;

        private readonly global::ClanChecklistSection _clan;

        public ProxyCompendiumClanChecklist(global::ClanChecklistSection clan)
            : base(clan != null ? clan.gameObject : null)
        {
            _clan = clan;
        }

        public override bool IsVisible => _clan != null && _clan.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            global::ClassData classData = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::ClassData>(_clan, ClassDataField);
            int currentLevel = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<int>(_clan, CurrentLevelField);
            int maxLevel = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<int>(_clan, MaxLevelField);
            return Message.Localized("ui", "COMPENDIUM.CHECKLIST.CLAN", new
            {
                clan = classData?.GetTitle(),
                level = currentLevel,
                max = maxLevel
            });
        }

        public override Message GetTooltip()
        {
            global::ClassData classData = global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<global::ClassData>(_clan, ClassDataField);
            return classData != null ? Message.RawCleaned(classData.GetDescription()) : null;
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
