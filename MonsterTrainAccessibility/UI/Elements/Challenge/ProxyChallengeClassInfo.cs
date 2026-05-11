using System;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeClassInfo : UIElement, IActivatableElement, INavigationTargetElement, INavigationActionHandler
    {
        private static readonly FieldInfo ClassTitleLabelField = AccessTools.Field(typeof(global::ClassLevelInfoUI), "titleLabel")!;

        private readonly global::ClassLevelInfoUI _info;
        private readonly IGameUIComponent _component;
        private readonly string _labelKey;
        private readonly Func<bool> _activate;
        private readonly Action _selected;
        private readonly Func<bool> _swapChampion;

        public ProxyChallengeClassInfo(
            global::ClassLevelInfoUI info,
            IGameUIComponent component,
            string labelKey,
            Func<bool> activate,
            Action selected = null,
            Func<bool> swapChampion = null)
        {
            _info = info;
            _component = component;
            _labelKey = labelKey;
            _activate = activate;
            _selected = selected;
            _swapChampion = swapChampion;
        }

        public override bool IsVisible => _info != null && _info.gameObject.activeInHierarchy;
        public override string GetTypeKey() => _component is GameUISelectableButton ? "button" : null;

        public override Message GetLabel()
        {
            Message value = ChallengePresentation.ClassWithChampion(_info?.ClassData, _info?.ChampionIndex ?? 0) ??
                Message.FromText(ClassDisplayTitle(_info));
            return value != null
                ? Message.Localized("ui", _labelKey, new { clan = value.Resolve() })
                : null;
        }

        public override Message GetTooltip()
        {
            return ChallengePresentation.ClassDescription(_info?.ClassData, _info != null && _info.IsMainClass);
        }

        public void SelectForNavigation()
        {
            _selected?.Invoke();
            if (_component != null && InputManager.Inst != null)
            {
                InputManager.Inst.SelectGameUIComponent(_component, allowClearingSelection: false);
            }
        }

        public bool Activate() => _activate != null && _activate();

        public bool HandleAction(InputAction action)
        {
            switch (action?.Key)
            {
                case "ui_left":
                case "ui_right":
                    return _swapChampion != null && _swapChampion();
                default:
                    return false;
            }
        }

        private static string ClassDisplayTitle(global::ClassLevelInfoUI info)
        {
            string title = info?.ClassData?.GetTitle();
            return !string.IsNullOrWhiteSpace(title)
                ? title
                : AccessibilityText.ReadLocalizedText(Get<TMP_Text>(info, ClassTitleLabelField));
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
