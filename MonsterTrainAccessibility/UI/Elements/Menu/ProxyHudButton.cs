using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyHudButton : GameObjectElement
    {
        private readonly GameUISelectableButton _button;
        private readonly string _scope;
        private readonly string _labelKey;
        private readonly Component _tooltipSource;

        public ProxyHudButton(GameUISelectableButton button, string scope, string labelKey, Component tooltipSource = null)
            : base(
                target: button != null ? button.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _button = button;
            _scope = scope;
            _labelKey = labelKey;
            _tooltipSource = tooltipSource;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.Localized(_scope, _labelKey);
        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);
        public override Message GetTooltip()
        {
            Component source = _tooltipSource ?? _button;
            return source != null ? TooltipText.ForComponent(source) : null;
        }
    }
}
