using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Map
{
    /// <summary>
    /// A stop on a run's route: a map node (a merchant, an event, a reward) or a battle. Holds the live
    /// widget and where the stop stands (its ring, its branch, its place on the branch); everything said
    /// about it is read off the widget when said. Two stops are the same stop when they stand in the
    /// same place (<see cref="Key"/>), whichever widget the game draws it with.
    /// </summary>
    internal sealed class MapStop
    {
        public readonly int Ring;
        public readonly global::MapScreen.BranchSelection Branch;
        public readonly int Index;
        public readonly MapNodeUI Node;
        public readonly MapBattleNodeUI Battle;

        private MapStop(int ring, global::MapScreen.BranchSelection branch, int index, MapNodeUI node, MapBattleNodeUI battle)
        {
            Ring = ring;
            Branch = branch;
            Index = index;
            Node = node;
            Battle = battle;
        }

        /// <summary>A node, at its place on its branch (in the order the train meets them).</summary>
        public static MapStop At(MapNodeUI node, int ring, global::MapScreen.BranchSelection branch, int index)
            => node != null ? new MapStop(ring, branch, index, node, null) : null;

        /// <summary>The battle that opens a ring (fought at the end of the one before).</summary>
        public static MapStop BattleInto(MapBattleNodeUI battle, int ring)
            => battle != null ? new MapStop(ring, global::MapScreen.BranchSelection.NoBranch, -1, null, battle) : null;

        public bool IsBattle => Battle != null;

        public Component Widget => IsBattle ? (Component)Battle : Node;

        public GameUISelectableButton Button => IsBattle ? Battle.button : Node.button;

        /// <summary>Where the stop stands, for identity and control ids.</summary>
        public string Key => IsBattle ? "battle:" + Ring : "node:" + Ring + ":" + (int)Branch + ":" + Index;

        public bool SameAs(MapStop other) => other != null && other.Key == Key;
    }
}
