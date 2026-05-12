using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    public class GridContainer : Container
    {
        private readonly Dictionary<UIElement, (int x, int y)> _positions = new Dictionary<UIElement, (int x, int y)>();
        private int _focusIndex = -1;

        public int MaxX { get; private set; }
        public int MaxY { get; private set; }

        public UIElement FocusedChild =>
            _focusIndex >= 0 && _focusIndex < Children.Count ? Children[_focusIndex] : null;

        public int FocusIndex => _focusIndex;

        public void Add(UIElement child, int x, int y)
        {
            base.Add(child);
            _positions[child] = (x, y);
            if (x >= MaxX) MaxX = x + 1;
            if (y >= MaxY) MaxY = y + 1;
        }

        public void ClearGrid()
        {
            base.Clear();
            _positions.Clear();
            _focusIndex = -1;
            MaxX = 0;
            MaxY = 0;
        }

        public override Message GetPositionString(UIElement child)
        {
            (int x, int y) pos;
            if (!_positions.TryGetValue(child, out pos))
            {
                return null;
            }

            int row = pos.y + 1;
            int column = pos.x + 1;
            return Message.Localized("ui", "POSITIONS.GRID", new { row, column });
        }

        public bool HandleAction(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.Key)
            {
                case "ui_left":
                    return Move(-1, 0);
                case "ui_right":
                    return Move(1, 0);
                case "ui_up":
                    return Move(0, -1);
                case "ui_down":
                    return Move(0, 1);
                case "ui_accept":
                case "ui_select":
                    return ActivateFocused();
                default:
                    return false;
            }
        }

        public void FocusFirst()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].IsVisible)
                {
                    SetFocus(i);
                    return;
                }
            }
        }

        public void SetFocusIndex(int index)
        {
            if (index < 0 || index >= Children.Count || !Children[index].IsVisible)
            {
                FocusFirst();
                return;
            }

            SetFocus(index);
        }

        public void SetFocusTo(UIElement element)
        {
            int index = IndexOf(element);
            if (index >= 0)
            {
                SetFocus(index);
            }
        }

        private bool Move(int dx, int dy)
        {
            UIElement focused = FocusedChild;
            if (focused == null || !_positions.TryGetValue(focused, out (int x, int y) current))
            {
                FocusFirst();
                return true;
            }

            int bestIndex = -1;
            int bestPrimaryDistance = int.MaxValue;
            int bestSecondaryDistance = int.MaxValue;

            for (int i = 0; i < Children.Count; i++)
            {
                UIElement candidate = Children[i];
                if (ReferenceEquals(candidate, focused) || !candidate.IsVisible || !_positions.TryGetValue(candidate, out (int x, int y) pos))
                {
                    continue;
                }

                int primaryDistance;
                int secondaryDistance;
                if (dx != 0)
                {
                    int delta = pos.x - current.x;
                    if (pos.y != current.y || delta * dx <= 0)
                    {
                        continue;
                    }
                    primaryDistance = Mathf.Abs(delta);
                    secondaryDistance = 0;
                }
                else
                {
                    int delta = pos.y - current.y;
                    if (delta * dy <= 0 || (AnnouncePosition && pos.x != current.x))
                    {
                        continue;
                    }
                    primaryDistance = Mathf.Abs(delta);
                    secondaryDistance = AnnouncePosition ? 0 : Mathf.Abs(pos.x - current.x);
                }

                if (primaryDistance < bestPrimaryDistance ||
                    primaryDistance == bestPrimaryDistance && secondaryDistance < bestSecondaryDistance)
                {
                    bestIndex = i;
                    bestPrimaryDistance = primaryDistance;
                    bestSecondaryDistance = secondaryDistance;
                }
            }

            if (bestIndex >= 0)
            {
                SetFocus(bestIndex);
            }

            return true;
        }

        private void SetFocus(int index)
        {
            if (_focusIndex >= 0 && _focusIndex < Children.Count && Children[_focusIndex].IsFocused)
            {
                Children[_focusIndex].Unfocus();
            }

            _focusIndex = index;
            UIElement child = Children[index];
            child.Focus();
            INavigationTargetElement navigationTarget = child as INavigationTargetElement;
            navigationTarget?.SelectForNavigation();
            UIManager.SetFocusedElement(child);
        }

        private bool ActivateFocused()
        {
            IActivatableElement activatable = FocusedChild as IActivatableElement;
            return activatable != null && activatable.Activate();
        }
    }
}
