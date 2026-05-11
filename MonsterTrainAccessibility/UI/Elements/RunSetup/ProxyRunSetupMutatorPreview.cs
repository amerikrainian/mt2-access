using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSetupMutatorPreview : GameObjectElement
    {
        private readonly GameUISelectableWithNavigation _selectable;
        private readonly TooltipProviderComponent _tooltip;
        private readonly GameObject _target;

        public ProxyRunSetupMutatorPreview(GameUISelectableWithNavigation selectable, TooltipProviderComponent tooltip)
            : base(
                selectable?.component != null ? selectable.component.gameObject : null,
                typeKey: "button",
                label: null)
        {
            _selectable = selectable;
            _tooltip = tooltip;
            _target = selectable?.component != null ? selectable.component.gameObject : null;
        }

        public override bool IsVisible => _target != null && _target.activeInHierarchy && _selectable != null && _selectable.interactable;

        public override Message GetLabel()
        {
            return Message.FromText(TooltipText.FirstTitle(_tooltip));
        }

        public override Message GetTooltip()
        {
            return TooltipText.ForComponent(_tooltip);
        }
    }
}
