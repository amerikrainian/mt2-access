using MonsterTrainAccessibility.UI.Screens;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal sealed class ProxyCompendiumRunStatRow : ProxyElement, INavigationTargetElement
    {
        private static readonly FieldInfo NameLabelField = AccessTools.Field(typeof(global::RunStatRow), "statNameLabel")!;
        private static readonly FieldInfo ValueLabelField = AccessTools.Field(typeof(global::RunStatRow), "statValueLabel")!;

        private readonly global::RunStatRow _row;

        public ProxyCompendiumRunStatRow(global::RunStatRow row)
            : base(row != null ? row.gameObject : null)
        {
            _row = row;
        }

        public override bool IsVisible => _row != null && _row.gameObject.activeInHierarchy;

        public override Message GetLabel()
        {
            return Message.Join(", ",
                Screens.CompendiumScreen.TextOrNull(global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(_row, NameLabelField)),
                Screens.CompendiumScreen.TextOrNull(global::MonsterTrainAccessibility.Core.ReflectionUtil.Get<TMP_Text>(_row, ValueLabelField)));
        }

        public void SelectForNavigation()
        {
            Screens.CompendiumScreen.ClearGameSelection();
        }
    }
}
