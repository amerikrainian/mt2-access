using ShinyShoe;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal static class ProxyFactory
    {
        public static UIElement Create(GameObject target)
        {
            if (target == null || !target.activeInHierarchy)
            {
                return null;
            }

            IGameUIComponent component = target.GetComponent<IGameUIComponent>();
            if (component != null)
            {
                return Create(component);
            }

            GameUISelectableButton button = target.GetComponent<GameUISelectableButton>();
            if (button != null)
            {
                return new BasicProxy(target, typeKey: "button");
            }

            Selectable selectable = target.GetComponent<Selectable>();
            if (selectable != null)
            {
                return new BasicProxy(target, typeKey: "button");
            }

            TooltipProviderComponent tooltip = target.GetComponent<TooltipProviderComponent>();
            if (tooltip != null)
            {
                return new BasicProxy(target);
            }

            return null;
        }

        public static UIElement Create(IGameUIComponent component)
        {
            if (component?.component == null || !component.component.gameObject.activeInHierarchy)
            {
                return null;
            }

            GameObject target = component.component.gameObject;
            GameUISelectableButton button = target.GetComponent<GameUISelectableButton>();
            return new BasicProxy(component, typeKey: button != null ? "button" : null);
        }
    }
}
