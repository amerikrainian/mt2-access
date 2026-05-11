using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Map;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyMapPoint : ProxyElement, INavigationTargetElement, IActivatableElement
    {
        private readonly MapNode _node;

        public ProxyMapPoint(MapNode node)
            : base(node?.Target)
        {
            _node = node;
        }

        public MapNode Node => _node;

        public override Message GetLabel()
        {
            return MapNavigationScreen.Current?.DescribePoint(_node, includeChoicePrefix: false)
                ?? MapNodeAnnouncementFormatter.DescribeNode(_node, new[] { _node }, includeChoicePrefix: false);
        }

        public override string GetTypeKey() => null;

        public override Message GetStatusString() => null;

        public override Message GetTooltip() => null;

        protected override void OnFocus()
        {
            MapNavigationScreen.Current?.UpdateStartPoint(_node);
        }

        public void SelectForNavigation()
        {
            SelectTarget();
        }

        public bool Activate()
        {
            MapNavigationScreen screen = MapNavigationScreen.Current;
            if (screen != null && !screen.CanSubmitNode(_node))
            {
                return false;
            }

            if (screen?.ActivateNode(_node) == true)
            {
                return true;
            }

            if (!SelectTarget())
            {
                return false;
            }

            return DispatchSubmit();
        }

        private bool SelectTarget()
        {
            if (_node == null || _node.Target == null || !_node.Target.activeInHierarchy)
            {
                return false;
            }

            IGameUIComponent component = _node.Target.GetComponent<IGameUIComponent>();
            if (component != null && global::InputManager.Inst != null)
            {
                return global::InputManager.Inst.SelectGameUIComponent(component, allowClearingSelection: false);
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            eventSystem.SetSelectedGameObject(_node.Target);
            return true;
        }

        private bool DispatchSubmit()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || _node == null || _node.Target == null)
            {
                return false;
            }

            CoreInputControlMapping mapping = new CoreInputControlMapping(global::InputManager.Controls.Submit, InputType.None, fake: true);
            CoreSignals.ControlMappingTriggered.Dispatch(mapping);

            if (_node.Target.GetComponent<IGameUIComponent>() == null)
            {
                ExecuteEvents.Execute(_node.Target, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            }

            return true;
        }
    }
}
