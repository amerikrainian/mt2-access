using System;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using ShinyShoe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal class GameObjectElement : ProxyElement, IActivatableElement, INavigationTargetElement
    {
        private readonly IGameUIComponent _component;
        private readonly Action _onSelected;
        private readonly Func<Message> _label;
        private readonly Func<Message> _status;
        private readonly Func<Message> _tooltip;
        private readonly Func<Message> _extras;
        private readonly Func<bool> _visibility;
        private readonly string _typeKey;
        public bool HasOverrideLabel { get; set; }

        public GameObjectElement(
            GameObject target,
            Func<Message> label,
            Func<Message> status = null,
            Func<Message> tooltip = null,
            Func<Message> extras = null,
            Func<bool> visibility = null,
            Action onSelected = null)
            : this(
                target: target,
                typeKey: null,
                label: label,
                status: status,
                tooltip: tooltip,
                extras: extras,
                visibility: visibility,
                onSelected: onSelected)
        {
        }

        public GameObjectElement(
            GameObject target,
            string typeKey,
            Func<Message> label,
            Func<Message> status = null,
            Func<Message> tooltip = null,
            Func<Message> extras = null,
            Func<bool> visibility = null,
            Action onSelected = null)
            : base(target)
        {
            _label = label;
            _status = status;
            _tooltip = tooltip;
            _extras = extras;
            _visibility = visibility;
            _typeKey = typeKey;
            _component = target != null ? target.GetComponent<IGameUIComponent>() : null;
            _onSelected = onSelected;
        }

        public GameObjectElement(
            IGameUIComponent component,
            string typeKey,
            Func<Message> label,
            Func<Message> status = null,
            Func<Message> tooltip = null,
            Func<Message> extras = null,
            Func<bool> visibility = null)
            : base(component?.component != null ? component.component.gameObject : null)
        {
            _label = label;
            _status = status;
            _tooltip = tooltip;
            _extras = extras;
            _visibility = visibility;
            _typeKey = typeKey;
            _component = component;
        }

        public override bool IsVisible => _visibility != null ? _visibility() : base.IsVisible;

        public virtual bool Activate()
        {
            if (!SelectTarget())
            {
                return false;
            }

            if ((_component ?? Target?.GetComponent<IGameUIComponent>()) != null)
            {
                return true;
            }

            return DispatchSubmit();
        }

        public override Message GetLabel()
        {
            if (HasOverrideLabel)
            {
                return Message.RawCleaned(OverrideLabel);
            }

            return _label != null ? _label() : null;
        }

        public override Message GetStatusString() => _status != null ? _status() : null;
        public override Message GetTooltip() => _tooltip != null ? _tooltip() : null;
        public override Message GetExtrasString() => _extras != null ? _extras() : null;
        public override string GetTypeKey() => _typeKey;

        public virtual void SelectForNavigation()
        {
            SelectTarget();
        }

        protected bool SelectTarget()
        {
            _onSelected?.Invoke();

            if (Target == null)
            {
                return _component == null && _onSelected != null;
            }

            IGameUIComponent component = _component ?? Target.GetComponent<IGameUIComponent>();
            if (component != null && global::InputManager.Inst != null)
            {
                return global::InputManager.Inst.SelectGameUIComponent(component, allowClearingSelection: false);
            }

            if (!Target.activeInHierarchy)
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

            if ((_component ?? Target.GetComponent<IGameUIComponent>()) == null)
            {
                ExecuteEvents.Execute(Target, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            }

            return true;
        }

        protected static Message DefaultLabel(GameObject target)
        {
            return Message.RawCleaned(ResolveLabel(target));
        }

        protected static Message DefaultLabel(IGameUIComponent component)
        {
            return null;
        }

        protected static string ResolveLabel(GameObject target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            global::ShinyShoe.GameUISelectableButton gameButton = target.GetComponent<global::ShinyShoe.GameUISelectableButton>();
            if (gameButton != null)
            {
                string label = Message.Clean(GameUIButtonSupport.ResolveLabel(gameButton));
                if (!string.IsNullOrWhiteSpace(label))
                {
                    return label;
                }
            }

            return string.Empty;
        }
    }
}
