using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal abstract class ButtonProxy : ProxyElement, IActivatableElement, INavigationTargetElement
    {
        private readonly IGameUIComponent _component;

        protected ButtonProxy(IGameUIComponent component)
            : base(component?.component != null ? component.component.gameObject : null)
        {
            _component = component;
        }

        protected ButtonProxy(GameObject target)
            : base(target)
        {
            _component = target != null ? target.GetComponent<IGameUIComponent>() : null;
        }

        public override string GetTypeKey() => "button";

        public virtual bool Activate()
        {
            if (!SelectTarget())
            {
                return false;
            }

            if (Component != null)
            {
                return true;
            }

            return DispatchSubmit();
        }

        public virtual void SelectForNavigation()
        {
            SelectTarget();
        }

        protected IGameUIComponent Component => _component ?? (Target != null ? Target.GetComponent<IGameUIComponent>() : null);

        protected virtual bool SelectTarget()
        {
            IGameUIComponent component = Component;
            if (component != null && global::InputManager.Inst != null)
            {
                return global::InputManager.Inst.SelectGameUIComponent(component, allowClearingSelection: false);
            }

            if (Target == null || !Target.activeInHierarchy)
            {
                return false;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            eventSystem.SetSelectedGameObject(Target);
            return true;
        }

        protected bool DispatchSubmit()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || Target == null)
            {
                return false;
            }

            CoreInputControlMapping mapping = new CoreInputControlMapping(global::InputManager.Controls.Submit, InputType.None, fake: true);
            CoreSignals.ControlMappingTriggered.Dispatch(mapping);
            ExecuteEvents.Execute(Target, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            return true;
        }

        protected static Message ButtonState(global::ShinyShoe.GameUISelectableButton button)
        {
            return GameButtonElement.StateMessage(button);
        }
    }
}
