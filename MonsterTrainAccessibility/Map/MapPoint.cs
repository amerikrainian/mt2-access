using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.Map
{
    internal sealed class MapPoint
    {
        public enum PointKind
        {
            Reward,
            Battle
        }

        public PointKind Kind { get; }
        public int Distance { get; }
        public global::MapScreen.BranchSelection Branch { get; }
        public int Index { get; }
        public GameObject Target { get; }
        public GameUISelectableButton Button { get; }
        public global::MapNodeUI RewardNode { get; }
        public global::MapBattleNodeUI BattleNode { get; }
        public global::RewardState.Location Location { get; }

        public MapPoint(
            PointKind kind,
            int distance,
            global::MapScreen.BranchSelection branch,
            int index,
            GameObject target,
            GameUISelectableButton button,
            global::MapNodeUI rewardNode,
            global::MapBattleNodeUI battleNode,
            global::RewardState.Location location)
        {
            Kind = kind;
            Distance = distance;
            Branch = branch;
            Index = index;
            Target = target;
            Button = button;
            RewardNode = rewardNode;
            BattleNode = battleNode;
            Location = location;
        }
    }
}
