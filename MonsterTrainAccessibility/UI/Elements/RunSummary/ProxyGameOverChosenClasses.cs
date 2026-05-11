using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal class ProxyGameOverChosenClasses : UIElement, INavigationTargetElement
    {
        private static readonly FieldInfo ChosenClassesSelectableField = AccessTools.Field(typeof(global::ChosenClassesUI), "_selectableUI")!;

        private readonly global::ChosenClassesUI _classes;

        public ProxyGameOverChosenClasses(global::ChosenClassesUI classes)
        {
            _classes = classes;
        }

        public override bool IsVisible => _classes != null && _classes.gameObject.activeInHierarchy;
        public override Message GetLabel() => AccessibleScreenText.Tooltip(_classes);
        public UnityEngine.GameObject Target => _classes != null ? _classes.gameObject : null;

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        public IGameUIComponent Selectable =>
            ChosenClassesSelectableField.GetValue(_classes) as IGameUIComponent ?? _classes?.SelectableUI;
    }
}
