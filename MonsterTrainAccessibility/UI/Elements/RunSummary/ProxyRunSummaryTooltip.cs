using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSummaryTooltip : UIElement
    {
        private readonly Component _component;
        private readonly string _labelKey;

        public ProxyRunSummaryTooltip(Component component, string labelKey)
        {
            _component = component;
            _labelKey = labelKey;
        }

        public override bool IsVisible => _component != null &&
            _component.gameObject.activeInHierarchy &&
            AccessibleScreenText.Tooltip(_component) != null;

        public override Message GetLabel()
        {
            Message value = AccessibleScreenText.Tooltip(_component);
            return value != null
                ? Message.Join(", ", Message.Localized("ui", _labelKey), value)
                : null;
        }
    }
}
