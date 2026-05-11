using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyTooltipInfo : GameObjectElement
    {
        private readonly Component _component;
        private readonly string _fallbackLabelKey;

        public ProxyTooltipInfo(Component component, string fallbackLabelKey)
            : base(
                target: component != null ? component.gameObject : null,
                typeKey: null,
                label: null)
        {
            _component = component;
            _fallbackLabelKey = fallbackLabelKey;
        }

        public override bool IsVisible => _component != null && _component.gameObject.activeInHierarchy;
        public override Message GetLabel() => TooltipTitle(_component) ?? Message.Localized("ui", _fallbackLabelKey);
        public override Message GetTooltip() => _component != null ? TooltipText.ForComponent(_component) : null;

        private static Message TooltipTitle(Component component)
        {
            TooltipProviderComponent provider = component != null ? component.GetComponent<TooltipProviderComponent>() : null;
            string title = TooltipText.FirstTitle(provider);
            return !string.IsNullOrWhiteSpace(title) ? Message.RawCleaned(title) : null;
        }
    }
}
