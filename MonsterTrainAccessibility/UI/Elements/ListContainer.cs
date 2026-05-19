using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    public enum NavigationAxis
    {
        None,
        Vertical,
        Horizontal
    }

    public class ListContainer : Container, INavigationActionHandler, INavigationTargetElement, IActivatableElement
    {
        private UIElement _focusedChild;

        public ListContainer()
        {
        }

        public ListContainer(string label, NavigationAxis axis)
        {
            ContainerLabel = label;
            AnnounceName = true;
            AnnouncePosition = true;
            NavigationAxis = axis;
        }

        public NavigationAxis NavigationAxis { get; set; }

        public UIElement FocusedChild => IndexOf(_focusedChild) >= 0 ? _focusedChild : null;

        public int FocusIndex => IndexOf(_focusedChild);

        public override bool IsVisible
        {
            get
            {
                if (NavigationAxis == NavigationAxis.None)
                {
                    return base.IsVisible;
                }

                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i].IsVisible)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override Message GetLabel()
        {
            if (NavigationAxis == NavigationAxis.None)
            {
                return base.GetLabel();
            }

            EnsureFocusedChild();
            Message group = base.GetLabel();
            Message child = FocusedChild?.GetLabel();
            if (group != null && child != null)
            {
                return Message.Join(", ", group, child);
            }

            return child ?? group;
        }

        public override Message GetExtrasString()
        {
            return NavigationAxis != NavigationAxis.None ? FocusedChildWithFallback?.GetExtrasString() : base.GetExtrasString();
        }

        public override string GetTypeKey()
        {
            return NavigationAxis != NavigationAxis.None ? FocusedChildWithFallback?.GetTypeKey() : base.GetTypeKey();
        }

        public override string GetSubtypeKey()
        {
            return NavigationAxis != NavigationAxis.None ? FocusedChildWithFallback?.GetSubtypeKey() : base.GetSubtypeKey();
        }

        public override Message GetStatusString()
        {
            return NavigationAxis != NavigationAxis.None ? FocusedChildWithFallback?.GetStatusString() : base.GetStatusString();
        }

        public override Message GetTooltip()
        {
            return NavigationAxis != NavigationAxis.None ? FocusedChildWithFallback?.GetTooltip() : base.GetTooltip();
        }

        public override Message GetPositionString(UIElement child)
        {
            int position = 0;
            int total = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                if (!Children[i].IsVisible)
                {
                    continue;
                }
                total++;
                if (Children[i] == child)
                {
                    position = total;
                }
            }

            if (position == 0)
            {
                return null;
            }

            return Message.Localized("ui", "POSITIONS.LIST", new { position, total });
        }

        public bool HandleAction(InputAction action)
        {
            if (NavigationAxis == NavigationAxis.None || action == null)
            {
                return false;
            }

            switch (action.Key)
            {
                case "ui_down":
                    return NavigationAxis == NavigationAxis.Vertical ? MoveRelative(1) : false;
                case "ui_up":
                    return NavigationAxis == NavigationAxis.Vertical ? MoveRelative(-1) : false;
                case "ui_left":
                    return NavigationAxis == NavigationAxis.Horizontal ? MoveRelative(-1) : HandleFocusedChildAction(action);
                case "ui_right":
                    return NavigationAxis == NavigationAxis.Horizontal ? MoveRelative(1) : HandleFocusedChildAction(action);
                case "ui_accept":
                case "ui_select":
                    return Activate();
                default:
                    return false;
            }
        }

        public void SelectForNavigation()
        {
            if (NavigationAxis == NavigationAxis.None || !EnsureFocusedChild())
            {
                return;
            }

            SelectFocusedChild();
        }

        public bool Activate()
        {
            if (NavigationAxis == NavigationAxis.None)
            {
                return false;
            }

            EnsureFocusedChild();
            IActivatableElement activatable = FocusedChild as IActivatableElement;
            return activatable != null && activatable.Activate();
        }

        public void FocusFirst()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].IsVisible)
                {
                    SetFocus(i, selectForNavigation: true);
                    return;
                }
            }
        }

        public bool MoveRelative(int direction)
        {
            if (Children.Count == 0)
            {
                return false;
            }

            int index = FocusIndex;
            if (index < 0)
            {
                FocusFirst();
                return true;
            }

            while (true)
            {
                index += direction;
                if (index < 0 || index >= Children.Count)
                {
                    return true;
                }

                if (Children[index].IsVisible)
                {
                    SetFocus(index, selectForNavigation: true);
                    return true;
                }
            }
        }

        public void SetFocusTo(UIElement element)
        {
            SetFocusTo(element, selectForNavigation: true);
        }

        public void SetFocusTo(UIElement element, bool selectForNavigation)
        {
            int index = IndexOf(element);
            if (index >= 0)
            {
                SetFocus(index, selectForNavigation);
            }
        }

        public void SetFocusIndex(int index)
        {
            SetFocusIndex(index, selectForNavigation: true);
        }

        public void SetFocusIndex(int index, bool selectForNavigation)
        {
            if (index < 0 || index >= Children.Count || !Children[index].IsVisible)
            {
                if (selectForNavigation)
                {
                    FocusFirst();
                }
                return;
            }

            SetFocus(index, selectForNavigation);
        }

        protected override void OnFocus()
        {
            if (NavigationAxis != NavigationAxis.None)
            {
                FocusFirst();
            }
        }

        protected override void OnUnfocus()
        {
            if (NavigationAxis != NavigationAxis.None && FocusedChild != null && FocusedChild.IsFocused)
            {
                FocusedChild.Unfocus();
            }
        }

        private UIElement FocusedChildWithFallback
        {
            get
            {
                EnsureFocusedChild();
                return FocusedChild;
            }
        }

        private bool EnsureFocusedChild()
        {
            int focusedIndex = FocusIndex;
            if (focusedIndex >= 0 && _focusedChild.IsVisible)
            {
                return true;
            }

            int start = focusedIndex;
            if (start >= 0)
            {
                for (int offset = 1; offset < Children.Count; offset++)
                {
                    int right = start + offset;
                    if (right < Children.Count && Children[right].IsVisible)
                    {
                        SetFocus(right, selectForNavigation: true);
                        return true;
                    }

                    int left = start - offset;
                    if (left >= 0 && Children[left].IsVisible)
                    {
                        SetFocus(left, selectForNavigation: true);
                        return true;
                    }
                }
            }

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].IsVisible)
                {
                    SetFocus(i, selectForNavigation: true);
                    return true;
                }
            }

            return false;
        }

        private bool HandleFocusedChildAction(InputAction action)
        {
            INavigationActionHandler actionHandler = FocusedChild as INavigationActionHandler;
            if (actionHandler != null && actionHandler.HandleAction(action))
            {
                return true;
            }

            return true;
        }

        private void SetFocus(int index, bool selectForNavigation)
        {
            if (_focusedChild != null && _focusedChild.IsFocused)
            {
                _focusedChild.Unfocus();
            }

            UIElement child = Children[index];
            _focusedChild = child;
            if (!child.IsFocused)
            {
                child.Focus();
            }

            if (selectForNavigation)
            {
                SelectFocusedChild();
            }

            ListContainer childList = child as ListContainer;
            if (childList != null && childList.NavigationAxis == NavigationAxis.Horizontal)
            {
                return;
            }

            UIManager.SetFocusedElement(child);
        }

        private void SelectFocusedChild()
        {
            INavigationTargetElement navigationTarget = FocusedChild as INavigationTargetElement;
            navigationTarget?.SelectForNavigation();
        }
    }
}
