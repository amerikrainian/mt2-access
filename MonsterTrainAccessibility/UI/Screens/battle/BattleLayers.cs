using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class BattleLayer
    {
        public readonly BattleListContainer Container;
        public readonly List<UIElement> Items = new List<UIElement>();
        public readonly List<UIElement> HiddenItems = new List<UIElement>();
        public int FocusIndex;

        public BattleLayer(BattleListContainer container)
        {
            Container = container;
        }

        public int VisibleCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i].IsVisible)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public void Add(UIElement element)
        {
            if (element == null)
            {
                return;
            }

            Items.Add(element);
            Container.Add(element);
        }

        public void AddHidden(UIElement element)
        {
            if (element == null)
            {
                return;
            }

            HiddenItems.Add(element);
            element.Parent = Container;
        }

        public bool Contains(UIElement element)
        {
            return element != null && (Items.Contains(element) || HiddenItems.Contains(element));
        }

        public int IndexOf(UIElement element)
        {
            return element != null ? Items.IndexOf(element) : -1;
        }

        public int GetNearestVisibleIndex(int preferred)
        {
            if (Items.Count == 0)
            {
                return -1;
            }

            int clamped = Mathf.Clamp(preferred, 0, Items.Count - 1);
            if (Items[clamped].IsVisible)
            {
                return clamped;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].IsVisible)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetVisibleOrdinal(int index)
        {
            int ordinal = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i].IsVisible)
                {
                    continue;
                }

                if (i == index)
                {
                    return ordinal;
                }

                ordinal++;
            }

            return -1;
        }

        public int GetIndexForVisibleOrdinal(int ordinal)
        {
            int current = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i].IsVisible)
                {
                    continue;
                }

                if (current == ordinal)
                {
                    return i;
                }

                current++;
            }

            return GetNearestVisibleIndex(0);
        }
    }

    internal sealed class BattleListContainer : ListContainer
    {
        public override Message GetPositionString(UIElement child)
        {
            int visible = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].IsVisible)
                {
                    visible++;
                }
            }

            return visible > 1 ? base.GetPositionString(child) : null;
        }
    }
}
