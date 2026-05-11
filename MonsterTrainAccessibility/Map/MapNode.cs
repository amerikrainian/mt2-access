using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using UnityEngine;

namespace MonsterTrainAccessibility.Map
{
    internal sealed class MapNode
    {
        public MapPoint Point { get; }
        public List<MapEdge> ForwardEdges { get; } = new List<MapEdge>();
        public List<MapEdge> BackwardEdges { get; } = new List<MapEdge>();

        public int Distance => Point.Distance;
        public global::MapScreen.BranchSelection Branch => Point.Branch;
        public GameObject Target => Point.Target;
        public int Row => Point.Distance + 1;
        public int Column { get; set; } = 1;

        public MapNode(MapPoint point)
        {
            Point = point;
        }

        public string GetDisplayName()
        {
            string name = string.Empty;

            if (Point.RewardNode != null && Point.RewardNode.GetData() != null)
            {
                name = Message.Clean(Point.RewardNode.GetData().GetTooltipTitle());
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = TooltipText.FirstTitle(Point.Target?.GetComponent<TooltipProviderComponent>());
            }

            if (string.IsNullOrWhiteSpace(name) && Point.Kind == MapPoint.PointKind.Battle)
            {
                name = Message.Localized("map_nav", "NODE.BATTLE").Resolve();
            }

            return name;
        }

        public string GetDetails()
        {
            string details = string.Empty;

            if (Point.RewardNode != null && Point.RewardNode.GetData() != null)
            {
                details = Message.Clean(Point.RewardNode.GetData().GetTooltipBody());
            }

            if (string.IsNullOrWhiteSpace(details))
            {
                details = TooltipText.FirstBody(Point.Target?.GetComponent<TooltipProviderComponent>());
            }

            return details;
        }

        public Message GetBranchMessage()
        {
            switch (Branch)
            {
                case global::MapScreen.BranchSelection.LeftBranch:
                    return Message.Localized("map_nav", "BRANCH.LEFT");
                case global::MapScreen.BranchSelection.RightBranch:
                    return Message.Localized("map_nav", "BRANCH.RIGHT");
                default:
                    return Message.Localized("map_nav", "BRANCH.SHARED");
            }
        }

        public bool CanBeSelected()
        {
            if (Point.Button == null || Point.Button.gameObject == null)
            {
                return Point.Target != null && Point.Target.activeInHierarchy;
            }

            return Point.Button.gameObject.activeInHierarchy &&
                Point.Button.interactable &&
                Point.Button.state != global::ShinyShoe.GameUISelectableButton.State.Disabled &&
                Point.Button.state != global::ShinyShoe.GameUISelectableButton.State.Locked;
        }

        public bool IsVisited(global::SaveManager saveManager)
        {
            if (Point.Kind != MapPoint.PointKind.Reward || Point.RewardNode?.GetData() == null || Point.Location == null || saveManager == null)
            {
                return false;
            }

            return Point.RewardNode.GetData().HasBeenVisited(Point.Location, saveManager);
        }

        public bool CanBeReached(global::SaveManager saveManager)
        {
            if (Point.Kind == MapPoint.PointKind.Battle)
            {
                return true;
            }

            if (Point.RewardNode?.GetData() == null || Point.Location == null || saveManager == null)
            {
                return false;
            }

            return Point.RewardNode.GetData().CanBeTriggered(Point.Location, saveManager);
        }
    }
}
