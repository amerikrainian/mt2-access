using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGameOverClassInfo : UIElement, INavigationTargetElement
    {
        private static readonly FieldInfo ClassNameLabelField = AccessTools.Field(typeof(global::GameOverClassInfoUI), "classNameLabel")!;

        private readonly global::GameOverClassInfoUI _classInfo;

        public ProxyGameOverClassInfo(global::GameOverClassInfoUI classInfo)
        {
            _classInfo = classInfo;
        }

        public override bool IsVisible => _classInfo != null && _classInfo.gameObject.activeInHierarchy;
        public override Message GetLabel() => AccessibleScreenText.Text(Get<TMP_Text>(_classInfo, ClassNameLabelField));

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
