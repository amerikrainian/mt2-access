using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.UI.Elements;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal abstract class GridNavigationGameScreen : GameScreen
    {
        protected readonly GridContainer Grid = new GridContainer
        {
            AnnounceName = false,
            AnnouncePosition = true
        };

        private string _signature;

        public override void OnPush()
        {
            base.OnPush();
            if (ShouldFocusFirstOnPush())
            {
                Grid.FocusFirst();
            }
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            return IsContainerNavigationAction(action?.Key) && Grid.HandleAction(action);
        }

        public override void OnUpdate()
        {
            string nextSignature = BuildSignature();
            if (!string.Equals(nextSignature, _signature, System.StringComparison.Ordinal))
            {
                int oldIndex = Grid.FocusIndex;
                BuildRegistry();
                Grid.SetFocusIndex(oldIndex);
            }
        }

        protected override void BuildRegistry()
        {
            ClearRegistry();
            Grid.ClearGrid();
            RootElement = Grid;
            PopulateGrid();
            _signature = BuildSignature();
        }

        protected abstract void PopulateGrid();

        protected virtual bool ShouldFocusFirstOnPush() => true;

        protected void ClaimGridMovementActions()
        {
            ClaimAction("ui_up");
            ClaimAction("ui_down");
            ClaimAction("ui_left");
            ClaimAction("ui_right");
        }

        protected virtual string BuildSignature()
        {
            return CountActiveTargets(RootElement).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        protected void RegisterElement(UIElement element, params GameObject[] targets)
        {
            Register(element, targets);
        }

        private static int CountActiveTargets(UIElement element)
        {
            Container container = element as Container;
            if (container == null)
            {
                return element != null && element.IsVisible ? 1 : 0;
            }

            int count = 0;
            for (int i = 0; i < container.Children.Count; i++)
            {
                count += CountActiveTargets(container.Children[i]);
            }
            return count;
        }
    }
}
