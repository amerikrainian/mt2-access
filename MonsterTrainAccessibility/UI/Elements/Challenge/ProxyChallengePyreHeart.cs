using System;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengePyreHeart : UIElement, IActivatableElement, INavigationTargetElement
    {
        private static readonly FieldInfo PyreHeartTitleLabelField = AccessTools.Field(typeof(global::PyreHeartInfoUI), "titleLabel")!;

        private readonly global::PyreHeartInfoUI _info;
        private readonly IGameUIComponent _component;
        private readonly Func<bool> _activate;

        public ProxyChallengePyreHeart(global::PyreHeartInfoUI info, IGameUIComponent component, Func<bool> activate)
        {
            _info = info;
            _component = component;
            _activate = activate;
        }

        public override bool IsVisible => _info != null && _info.gameObject.activeInHierarchy;
        public override string GetTypeKey() => _component is GameUISelectableButton ? "button" : null;

        public override Message GetLabel()
        {
            return ChallengePresentation.PyreHeartLabel(_info?.PyreHeartCharacterData) ??
                Message.FromText(PyreHeartDisplayTitle(_info));
        }

        public override Message GetTooltip()
        {
            return ChallengePresentation.PyreHeartTooltip(_info?.PyreHeartCharacterData);
        }

        public void SelectForNavigation()
        {
            if (_component != null && InputManager.Inst != null)
            {
                InputManager.Inst.SelectGameUIComponent(_component, allowClearingSelection: false);
            }
        }

        public bool Activate() => _activate != null && _activate();

        private static string PyreHeartDisplayTitle(global::PyreHeartInfoUI info)
        {
            string title = info?.PyreHeartCharacterData?.GetName();
            return !string.IsNullOrWhiteSpace(title)
                ? title
                : AccessibilityText.ReadLocalizedText(Get<TMP_Text>(info, PyreHeartTitleLabelField));
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
