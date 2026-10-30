using System.Collections.Generic;
using System.Linq;

namespace MonsterTrainAccessibility.Module.Map
{
    /// <summary>
    /// A normal run's route, read from the game's map (<see cref="global::MapScreen"/>): one
    /// <see cref="MapSection"/> per ring, its nodes held by branch (left, right, and shared by both), its
    /// battles at either end. What the player may choose follows the save's game sequence:
    /// <list type="bullet">
    /// <item>after a battle, while the game offers the branch choice: the first node of each branch,
    /// side by side;</item>
    /// <item>in the ring: the nodes of the chosen branch and the shared ones the game lets the player
    /// act on now (its own <c>CanActivate</c>), those not yet visited first, in the order the train
    /// meets them; then the battle that ends the ring.</item>
    /// </list>
    /// The route itself: a battle leads into its ring, where the route forks into the branches; a
    /// branch runs node by node into the shared nodes, and those run into the next battle. A route may
    /// end at a ring (Soul Savior's region): past the battle that closes it, nothing follows.
    /// </summary>
    internal sealed class TrainRoute : IRunRoute
    {
        private readonly global::MapScreen _map;
        private readonly int _lastRing;

        public TrainRoute(global::MapScreen map, int lastRing = int.MaxValue)
        {
            _map = map;
            _lastRing = lastRing;
        }

        private SaveManager Save => _map.saveManager;

        private int CurrentRing => Save.GetCurrentDistance();

        private MapSection Section(int ring)
            => _map.sections != null && ring >= 0 && ring < _map.sections.Count ? _map.sections[ring] : null;

        public ChoiceLayout Layout => BranchChoiceOpen() ? ChoiceLayout.Row : ChoiceLayout.Column;

        public IReadOnlyList<MapStop> Choices()
        {
            var choices = new List<MapStop>();
            if (Save == null) return choices;
            int ring = CurrentRing;
            if (BranchChoiceOpen())
            {
                foreach (var branch in Branches(ring))
                    choices.Add(Nodes(ring, branch)[0]);
                return choices;
            }
            if (Save.GetGameSequence() != SaveData.GameSequence.DestinationReached) return choices;
            // In the route's order, the one Next walks (the branch's line, then the shared line), so the
            // arrows and the preview agree.
            var branchNow = (global::MapScreen.BranchSelection)Save.GetCurrentBranch();
            choices.AddRange(Nodes(ring, branchNow).Concat(Nodes(ring, global::MapScreen.BranchSelection.NoBranch))
                .Where(stop => stop.Node.CanActivate));
            var exit = Exit(ring);
            if (exit != null && exit.Battle.gameObject.activeInHierarchy && exit.Button != null && exit.Button.interactable)
                choices.Add(exit);
            return choices;
        }

        public MapStop Origin() => Save != null ? Entry(CurrentRing) : null;

        public IReadOnlyList<MapStop> Next(MapStop from)
        {
            if (Save == null) return new List<MapStop>();
            if (from == null) return StartOf(CurrentRing);
            if (from.IsBattle) return from.Ring > _lastRing ? new List<MapStop>() : StartOf(from.Ring);
            var after = new List<MapStop>();
            var line = Nodes(from.Ring, from.Branch);
            int at = line.FindIndex(stop => stop.SameAs(from));
            if (at >= 0 && at + 1 < line.Count)
            {
                after.Add(line[at + 1]);
                return after;
            }
            if (from.Branch != global::MapScreen.BranchSelection.NoBranch)
            {
                var shared = Nodes(from.Ring, global::MapScreen.BranchSelection.NoBranch);
                if (shared.Count > 0)
                {
                    after.Add(shared[0]);
                    return after;
                }
            }
            var exit = Exit(from.Ring);
            if (exit != null) after.Add(exit);
            return after;
        }

        public void Preview(MapStop choice)
        {
            if (choice == null) return;
            LightBranch(choice);
            Interop.GameInput.Hover(choice.Button);
        }

        // A press as the game takes it. At the branch choice the game picks the branch it has lit, so
        // the choice's branch is lit first (hovering lights it only while no branch is).
        public void Choose(MapStop choice)
        {
            if (choice == null) return;
            LightBranch(choice);
            Interop.GameInput.Submit(choice.Button);
        }

        private void LightBranch(MapStop choice)
        {
            if (choice.IsBattle || choice.Branch == global::MapScreen.BranchSelection.NoBranch || !BranchChoiceOpen()) return;
            var section = Section(choice.Ring);
            if (section != null && section.highlightedBranch != choice.Branch) section.HighlightBranch(choice.Branch);
        }

        // The game offers the branch choice after a battle when the ring forks (MapSection's own test).
        // Its enabled flag, not its branch buttons: those show only in keyboard or pad mode, and the game
        // falls into mouse mode once the mouse moves (focus mode keeps its keys from bringing it back),
        // while a press on a branch's node chooses it in either mode.
        private bool BranchChoiceOpen()
        {
            if (Save == null || Save.GetGameSequence() != SaveData.GameSequence.LeavingBattle) return false;
            var section = Section(CurrentRing);
            return section != null && section.branchChoiceEnabled && Branches(CurrentRing).Count > 1;
        }

        // Where a ring's route starts: the first node of each branch, else of the shared ones, else its
        // closing battle.
        private List<MapStop> StartOf(int ring)
        {
            var starts = new List<MapStop>();
            foreach (var branch in Branches(ring)) starts.Add(Nodes(ring, branch)[0]);
            if (starts.Count > 0) return starts;
            var shared = Nodes(ring, global::MapScreen.BranchSelection.NoBranch);
            if (shared.Count > 0)
            {
                starts.Add(shared[0]);
                return starts;
            }
            var exit = Exit(ring);
            if (exit != null) starts.Add(exit);
            return starts;
        }

        // The ring's branches that hold nodes, left first.
        private List<global::MapScreen.BranchSelection> Branches(int ring)
        {
            var branches = new List<global::MapScreen.BranchSelection>();
            foreach (var branch in new[] { global::MapScreen.BranchSelection.LeftBranch, global::MapScreen.BranchSelection.RightBranch })
                if (Nodes(ring, branch).Count > 0) branches.Add(branch);
            return branches;
        }

        // A branch's nodes the game has set, in the order the train meets them (bottom to top, as the
        // game orders them itself), each keeping its place in the game's list as its index.
        private List<MapStop> Nodes(int ring, global::MapScreen.BranchSelection branch)
        {
            var stops = new List<MapStop>();
            var section = Section(ring);
            if (section == null || section.mapNodesByBranch == null || !section.mapNodesByBranch.TryGetValue(branch, out var nodes)) return stops;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node != null && node.GetData() != null && node.gameObject.activeInHierarchy) stops.Add(MapStop.At(node, ring, branch, i));
            }
            return stops.OrderBy(stop => stop.Node.transform.position.y).ToList();
        }

        // The battle that opens a ring; the first ring has none.
        private MapStop Entry(int ring)
        {
            var section = Section(ring);
            if (section == null) return null;
            var battle = section.PreviousBattleNodeAlternate != null ? section.PreviousBattleNodeAlternate : section.PreviousBattleNode;
            return battle != null && battle.gameObject.activeInHierarchy ? MapStop.BattleInto(battle, ring) : null;
        }

        // The battle that closes a ring, which opens the next.
        private MapStop Exit(int ring)
        {
            var section = Section(ring);
            var battle = section != null ? section.NextBattleNode : null;
            return battle != null ? MapStop.BattleInto(battle, ring + 1) : null;
        }
    }
}
