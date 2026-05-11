using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal class BasicProxy : GameObjectElement
    {
        public BasicProxy(GameObject target, string typeKey = null)
            : base(
                target: target,
                typeKey: typeKey,
                label: null)
        {
        }

        public BasicProxy(IGameUIComponent component, string typeKey = null)
            : base(
                component: component,
                typeKey: typeKey,
                label: null)
        {
        }

        public override Message GetLabel()
        {
            TooltipProviderComponent tooltip = Target != null ? Target.GetComponent<TooltipProviderComponent>() : null;
            string title = TooltipText.FirstTitle(tooltip);
            return !string.IsNullOrWhiteSpace(title)
                ? Message.RawCleaned(title)
                : DefaultLabel(Target);
        }

        public override Message GetStatusString()
        {
            GameUISelectableButton button = Target != null ? Target.GetComponent<GameUISelectableButton>() : null;
            if (button != null)
            {
                return GameButtonElement.StateMessage(button);
            }

            Selectable selectable = Target != null ? Target.GetComponent<Selectable>() : null;
            return selectable != null && !selectable.interactable
                ? Message.Localized("messages", "state.disabled")
                : null;
        }

        public override Message GetTooltip()
        {
            if (Target == null)
            {
                return null;
            }

            TooltipProviderComponent tooltip = Target.GetComponent<TooltipProviderComponent>();
            return tooltip != null
                ? TooltipText.ForComponent(tooltip)
                : TooltipText.ForComponent(Target.transform);
        }
    }
}
