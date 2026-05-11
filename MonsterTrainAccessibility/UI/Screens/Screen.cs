using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.UI.Elements;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    public abstract class Screen
    {
        private readonly struct ClaimInfo
        {
            public readonly bool Propagate;
            public readonly bool FocusedOnly;

            public ClaimInfo(bool propagate, bool focusedOnly)
            {
                Propagate = propagate;
                FocusedOnly = focusedOnly;
            }
        }

        private readonly Dictionary<string, ClaimInfo> _claimedActions = new Dictionary<string, ClaimInfo>();
        private bool _claimAll;

        public virtual string ScreenName => null;

        public Container RootElement { get; protected set; }

        public Screen Parent { get; private set; }
        public Screen ActiveChild { get; private set; }

        public void PushChild(Screen child)
        {
            if (ActiveChild != null)
            {
                RemoveChild(ActiveChild);
            }
            child.Parent = this;
            ActiveChild = child;
            child.OnPush();
        }

        public void RemoveChild(Screen child)
        {
            if (child.ActiveChild != null)
            {
                child.RemoveChild(child.ActiveChild);
            }
            child.OnPop();
            child.Parent = null;
            if (ActiveChild == child)
            {
                ActiveChild = null;
            }
        }

        public Screen DeepestActiveScreen()
        {
            Screen s = this;
            while (s.ActiveChild != null)
            {
                s = s.ActiveChild;
            }
            return s;
        }

        public virtual bool OnActionJustPressed(InputAction action) => HandleRootAction(action);
        public virtual bool OnActionPressed(InputAction action) => false;
        public virtual bool OnActionJustReleased(InputAction action) => false;

        protected bool HandleRootAction(InputAction action)
        {
            ListContainer navigable = RootElement as ListContainer;
            if (navigable != null && navigable.NavigationAxis != NavigationAxis.None && IsContainerNavigationAction(action?.Key))
            {
                return navigable.HandleAction(action);
            }

            return false;
        }

        protected static bool IsContainerNavigationAction(string actionKey)
        {
            switch (actionKey)
            {
                case "ui_up":
                case "ui_down":
                case "ui_left":
                case "ui_right":
                case "ui_accept":
                case "ui_select":
                    return true;
                default:
                    return false;
            }
        }

        public virtual void OnPush() { }
        public virtual void OnPop() { }
        public virtual void OnFocus() { }
        public virtual void OnUnfocus() { }
        public virtual void OnUpdate() { }
        public virtual void BeforeNavigationInput() { }
        public virtual bool ShouldAnnounceFocus(UIElement element) => true;
        public virtual bool ShouldRestoreNavigationFocus() => true;
        public virtual bool ShouldAcceptGameSelection() => true;
        public virtual bool IsActionAvailable(InputAction action) => true;
        public virtual void ConfigurePersistentBuffers(HashSet<string> keys) { }
        internal virtual void ConfigureBuffers(BufferManager buffers) { }

        public virtual UIElement GetElement(GameObject go) => null;

        protected void ClaimAction(string actionKey, bool propagate = false, bool focusedOnly = false)
        {
            _claimedActions[actionKey] = new ClaimInfo(propagate, focusedOnly);
        }

        protected void ClaimAllActions()
        {
            _claimAll = true;
        }

        public bool HasClaimed(string actionKey)
        {
            if (_claimAll) return true;
            ListContainer rootList = RootElement as ListContainer;
            if (IsContainerNavigationAction(actionKey) && rootList != null && rootList.NavigationAxis != NavigationAxis.None) return true;
            ClaimInfo info;
            if (!_claimedActions.TryGetValue(actionKey, out info)) return false;
            if (info.FocusedOnly && !ReferenceEquals(ScreenManager.CurrentScreen, this)) return false;
            return true;
        }

        public virtual bool ShouldPropagate(string actionKey)
        {
            ClaimInfo info;
            return _claimedActions.TryGetValue(actionKey, out info) && info.Propagate;
        }

        public virtual bool BlocksGameInput(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.Key)
            {
                case "ui_accept":
                case "ui_select":
                case "ui_cancel":
                    return false;
                default:
                    return !ShouldPropagate(action.Key);
            }
        }
    }
}
