using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Map
{
    internal sealed class TreeMapViewer
    {
        private readonly MapHandler _handler;
        private readonly List<MapNode> _choiceNodes = new List<MapNode>();
        private int _choiceIndex = -1;
        private string _choiceOriginKey;
        private string _announcedChoiceOriginKey;
        private readonly List<MapNode> _virtualChoices = new List<MapNode>();
        private readonly List<VirtualCursorFrame> _virtualOriginStack = new List<VirtualCursorFrame>();
        private MapNode _virtualOrigin;
        private MapNode _virtualSelection;
        private int _virtualChoiceIndex = -1;
        private string _virtualChoiceOriginKey;
        private string _announcedVirtualChoiceOriginKey;

        public MapNode CurrentNode { get; private set; }
        public bool LastIncludedChoicePrefix { get; private set; }
        public bool UsesCurrentRingList => IsCurrentRingListActive();

        public TreeMapViewer(MapHandler handler)
        {
            _handler = handler;
        }

        public void SetStartNode(MapNode node)
        {
            if (node == null)
            {
                return;
            }

            CurrentNode = node;
            int index = _choiceNodes.IndexOf(node);
            if (index >= 0)
            {
                _choiceIndex = index;
            }

            if (_virtualOrigin == null)
            {
                ResetVirtualCursorToActualChoices();
            }
        }

        public bool IsCurrentNode(MapNode node)
        {
            return SameNodeIdentity(CurrentNode, node);
        }

        public Message CycleChoice(int direction)
        {
            RefreshChoices();
            if (_choiceNodes.Count == 0)
            {
                return NoForward("CycleChoice");
            }

            if (_choiceIndex < 0 || _choiceIndex >= _choiceNodes.Count)
            {
                _choiceIndex = direction < 0 ? _choiceNodes.Count - 1 : 0;
            }
            else if (_choiceIndex + direction < 0 || _choiceIndex + direction >= _choiceNodes.Count)
            {
                return null;
            }
            else
            {
                _choiceIndex += direction;
            }

            CurrentNode = _choiceNodes[_choiceIndex];
            ResetVirtualCursorToActualChoices();
            bool includeChoicePrefix = _choiceNodes.Count > 1 &&
                !string.Equals(_announcedChoiceOriginKey, _choiceOriginKey, StringComparison.Ordinal);
            LastIncludedChoicePrefix = includeChoicePrefix;
            if (includeChoicePrefix)
            {
                _announcedChoiceOriginKey = _choiceOriginKey;
            }

            return MapNodeAnnouncementFormatter.DescribeNode(CurrentNode, _choiceNodes, includeChoicePrefix);
        }

        public Message MoveVirtualCursor(int deltaCol, int deltaRow)
        {
            LastIncludedChoicePrefix = false;

            EnsureVirtualCursor();

            if (deltaCol < 0)
            {
                return CycleVirtualChoice(-1);
            }

            if (deltaCol > 0)
            {
                return CycleVirtualChoice(1);
            }

            if (deltaRow > 0)
            {
                return AdvanceVirtualOrigin();
            }

            if (deltaRow < 0)
            {
                return RetreatVirtualOrigin();
            }

            return null;
        }

        public Message SnapToCurrentOrigin()
        {
            LastIncludedChoicePrefix = false;

            MapNode origin = GetCurrentOrigin();
            if (origin == null)
            {
                return null;
            }

            CurrentNode = origin;
            ResetVirtualCursorToActualChoices();
            return MapNodeAnnouncementFormatter.DescribeNode(CurrentNode, new[] { CurrentNode }, includeChoicePrefix: false);
        }

        private void RefreshChoices()
        {
            MapNode previousChoice = CurrentNode;
            string previousOriginKey = _choiceOriginKey;
            _choiceNodes.Clear();
            MapNode origin = GetCurrentOrigin();
            _choiceOriginKey = GetOriginKey(origin);
            if (!string.Equals(previousOriginKey, _choiceOriginKey, StringComparison.Ordinal))
            {
                _announcedChoiceOriginKey = null;
            }

            foreach (MapNode node in GetImmediateChoices(origin, previewFuture: false))
            {
                if (!ContainsEquivalent(_choiceNodes, node))
                {
                    _choiceNodes.Add(node);
                }
            }

            if (!IsCurrentRingListActive())
            {
                _choiceNodes.Sort(CompareMapNodes);
            }

            _choiceIndex = previousChoice != null ? _choiceNodes.IndexOf(previousChoice) : -1;
        }

        private void ResetVirtualCursorToActualChoices()
        {
            _virtualOriginStack.Clear();
            _virtualOrigin = CurrentNode ?? (ShouldUseCurrentRingVirtualPreview() ? null : GetCurrentOrigin());
            _virtualChoices.Clear();
            _announcedVirtualChoiceOriginKey = null;
            _virtualChoiceOriginKey = GetOriginKey(_virtualOrigin);
            _virtualChoiceIndex = -1;
            _virtualSelection = null;
        }

        private void EnsureVirtualCursor()
        {
            if (_virtualOrigin == null && _virtualSelection == null && !ShouldUseCurrentRingVirtualPreview())
            {
                ResetVirtualCursorToActualChoices();
            }
        }

        private void RefreshVirtualChoices(MapNode origin)
        {
            string previousOriginKey = _virtualChoiceOriginKey;
            _virtualChoices.Clear();
            foreach (MapNode node in GetImmediateChoices(origin, previewFuture: true))
            {
                if (!ContainsEquivalent(_virtualChoices, node))
                {
                    _virtualChoices.Add(node);
                }
            }

            _virtualChoices.Sort(CompareMapNodes);
            _virtualChoiceOriginKey = GetOriginKey(origin);
            if (!string.Equals(previousOriginKey, _virtualChoiceOriginKey, StringComparison.Ordinal))
            {
                _announcedVirtualChoiceOriginKey = null;
            }
        }

        private Message CycleVirtualChoice(int direction)
        {
            if (_virtualChoices.Count <= 1)
            {
                return null;
            }

            if (_virtualChoiceIndex < 0 || _virtualChoiceIndex >= _virtualChoices.Count)
            {
                _virtualChoiceIndex = direction < 0 ? _virtualChoices.Count - 1 : 0;
            }
            else if (_virtualChoiceIndex + direction < 0 || _virtualChoiceIndex + direction >= _virtualChoices.Count)
            {
                return null;
            }
            else
            {
                _virtualChoiceIndex += direction;
            }

            _virtualOrigin = _virtualChoices[_virtualChoiceIndex];
            _virtualSelection = _virtualOrigin;
            return DescribeVirtualSelection();
        }

        private Message AdvanceVirtualOrigin()
        {
            MapNode previousOrigin = _virtualOrigin;
            VirtualCursorFrame previousFrame = CaptureVirtualCursorFrame(previousOrigin);
            RefreshVirtualChoices(previousOrigin);
            if (_virtualChoices.Count == 0)
            {
                return NoForward("AdvanceVirtualOrigin:no-choices");
            }

            _virtualChoiceIndex = 0;
            MapNode destination = _virtualChoices[_virtualChoiceIndex];
            bool includeChoicePrefix = _virtualChoices.Count > 1 &&
                !string.Equals(_announcedVirtualChoiceOriginKey, _virtualChoiceOriginKey, StringComparison.Ordinal);
            if (includeChoicePrefix)
            {
                _announcedVirtualChoiceOriginKey = _virtualChoiceOriginKey;
            }

            Message announcement = MapNodeAnnouncementFormatter.DescribeNode(destination, _virtualChoices, includeChoicePrefix);
            _virtualOriginStack.Add(previousFrame);
            _virtualOrigin = destination;
            _virtualSelection = destination;
            return announcement;
        }

        private Message RetreatVirtualOrigin()
        {
            if (_virtualOriginStack.Count == 0)
            {
                return MapNodeAnnouncementFormatter.NoBackward();
            }

            VirtualCursorFrame frame = _virtualOriginStack[_virtualOriginStack.Count - 1];
            _virtualOriginStack.RemoveAt(_virtualOriginStack.Count - 1);
            _virtualOrigin = frame.Origin;
            _virtualChoices.Clear();
            _virtualChoices.AddRange(frame.Choices);
            _virtualChoiceIndex = frame.ChoiceIndex;
            _virtualChoiceOriginKey = frame.ChoiceOriginKey;
            if (_virtualChoiceIndex < 0 || _virtualChoiceIndex >= _virtualChoices.Count ||
                !SameNodeIdentity(_virtualChoices[_virtualChoiceIndex], _virtualOrigin))
            {
                _virtualChoiceIndex = FindEquivalentIndex(_virtualChoices, _virtualOrigin);
            }

            _virtualSelection = _virtualChoiceIndex >= 0 && _virtualChoiceIndex < _virtualChoices.Count
                ? _virtualChoices[_virtualChoiceIndex]
                : null;

            if (_virtualChoices.Count > 1)
            {
                _announcedVirtualChoiceOriginKey = null;
            }

            return _virtualSelection != null ? DescribeVirtualSelection() : DescribeVirtualOrigin();
        }

        private VirtualCursorFrame CaptureVirtualCursorFrame(MapNode origin)
        {
            return new VirtualCursorFrame(origin, _virtualChoices, _virtualChoiceIndex, _virtualChoiceOriginKey);
        }

        private Message DescribeVirtualOrigin()
        {
            return _virtualOrigin != null
                ? MapNodeAnnouncementFormatter.DescribeNode(_virtualOrigin, new[] { _virtualOrigin }, includeChoicePrefix: false)
                : null;
        }

        private Message DescribeVirtualSelection()
        {
            if (_virtualSelection == null)
            {
                return null;
            }

            bool includeChoicePrefix = _virtualChoices.Count > 1 &&
                !string.Equals(_announcedVirtualChoiceOriginKey, _virtualChoiceOriginKey, StringComparison.Ordinal);
            if (includeChoicePrefix)
            {
                _announcedVirtualChoiceOriginKey = _virtualChoiceOriginKey;
            }

            return MapNodeAnnouncementFormatter.DescribeNode(_virtualSelection, _virtualChoices, includeChoicePrefix);
        }

        private IEnumerable<MapNode> GetImmediateChoices(MapNode origin, bool previewFuture)
        {
            if (!previewFuture && IsCurrentRingListActive())
            {
                return GetCurrentRingChoices(includeBattle: true);
            }

            if (previewFuture && ShouldUseCurrentRingVirtualPreview() && origin == null)
            {
                return GetCurrentRingChoices(includeBattle: true);
            }

            int currentDistance = _handler.SaveManager?.GetCurrentDistance() ?? 0;
            if (origin != null)
            {
                return previewFuture
                    ? GetPreviewChoicesFromOrigin(origin, currentDistance)
                    : origin.ForwardEdges
                        .Select(edge => edge.To)
                        .Where(node => node != null &&
                            !SameNodeIdentity(origin, node) &&
                            node.Distance >= currentDistance &&
                            CanUseNodeForChoice(node, currentDistance, previewFuture));
            }

            return _handler.Nodes
                .Where(node => node.Distance == currentDistance && node.Point.Kind == MapPoint.PointKind.Reward)
                .Where(node => node != null &&
                    !SameNodeIdentity(origin, node) &&
                    node.Distance >= currentDistance &&
                    CanUseNodeForChoice(node, currentDistance, previewFuture));
        }

        private IEnumerable<MapNode> GetPreviewChoicesFromOrigin(MapNode origin, int currentDistance)
        {
            List<MapNode> choices = new List<MapNode>();
            HashSet<MapNode> seen = new HashSet<MapNode>();
            Queue<MapNode> queue = new Queue<MapNode>();
            for (int i = 0; i < origin.ForwardEdges.Count; i++)
            {
                queue.Enqueue(origin.ForwardEdges[i].To);
            }

            while (queue.Count > 0)
            {
                MapNode node = queue.Dequeue();
                if (node == null || !seen.Add(node) || SameNodeIdentity(origin, node) || node.Distance < currentDistance)
                {
                    continue;
                }

                if (CanUseNodeForChoice(node, currentDistance, previewFuture: true))
                {
                    if (!ContainsEquivalent(choices, node))
                    {
                        choices.Add(node);
                    }

                    continue;
                }

                if (node.Distance == currentDistance)
                {
                    for (int i = 0; i < node.ForwardEdges.Count; i++)
                    {
                        queue.Enqueue(node.ForwardEdges[i].To);
                    }
                }
            }

            return choices;
        }

        private IEnumerable<MapNode> GetCurrentRingChoices(bool includeBattle)
        {
            return _handler.GetCurrentRingChoices(includeBattle);
        }

        private bool IsCurrentRingListActive()
        {
            return _handler.SaveManager?.GetGameSequence() == global::SaveData.GameSequence.DestinationReached;
        }

        private bool ShouldUseCurrentRingVirtualPreview()
        {
            return IsCurrentRingListActive() && _handler.SaveManager?.IsRegionRun == true;
        }

        private bool CanUseNodeForChoice(MapNode node, int currentDistance, bool previewFuture)
        {
            if (node == null)
            {
                return false;
            }

            if (!previewFuture)
            {
                return node.CanBeReached(_handler.SaveManager);
            }

            if (node.Point.Kind == MapPoint.PointKind.Battle)
            {
                return true;
            }

            if (node.Distance > currentDistance)
            {
                return true;
            }

            return node.CanBeReached(_handler.SaveManager);
        }

        private MapNode GetCurrentOrigin()
        {
            global::SaveManager saveManager = _handler.SaveManager;
            if (saveManager == null)
            {
                return null;
            }

            int distance = saveManager.GetCurrentDistance();
            if (saveManager.GetGameSequence() == global::SaveData.GameSequence.LeavingBattle)
            {
                return _handler.GetBattleNodeAtDistance(distance);
            }

            List<MapNode> route = GetCurrentRouteNodes(distance, (global::MapScreen.BranchSelection)saveManager.GetCurrentBranch());
            MapNode entry = _handler.GetBattleNodeAtDistance(distance);
            MapNode previous = entry;
            for (int i = 0; i < route.Count; i++)
            {
                MapNode node = route[i];
                if (!node.IsVisited(saveManager) && node.CanBeReached(saveManager))
                {
                    return previous;
                }

                previous = node;
            }

            return previous;
        }

        private List<MapNode> GetCurrentRouteNodes(int distance, global::MapScreen.BranchSelection branch)
        {
            MapNode entry = _handler.GetBattleNodeAtDistance(distance);
            List<MapNode> route = new List<MapNode>();
            HashSet<MapNode> seen = new HashSet<MapNode>();
            Queue<MapNode> queue = new Queue<MapNode>();

            if (entry != null)
            {
                for (int i = 0; i < entry.ForwardEdges.Count; i++)
                {
                    queue.Enqueue(entry.ForwardEdges[i].To);
                }
            }
            else
            {
                foreach (MapNode node in _handler.Nodes.Where(node => node.Distance == distance))
                {
                    queue.Enqueue(node);
                }
            }

            while (queue.Count > 0)
            {
                MapNode node = queue.Dequeue();
                if (node == null || !seen.Add(node) || node.Distance != distance)
                {
                    continue;
                }

                if (node.Point.Kind == MapPoint.PointKind.Reward &&
                    (node.Branch == branch || node.Branch == global::MapScreen.BranchSelection.NoBranch))
                {
                    route.Add(node);
                    for (int i = 0; i < node.ForwardEdges.Count; i++)
                    {
                        queue.Enqueue(node.ForwardEdges[i].To);
                    }
                }
            }

            return route;
        }

        private static int CompareMapNodes(MapNode left, MapNode right)
        {
            int row = left.Row.CompareTo(right.Row);
            if (row != 0)
            {
                return row;
            }

            int column = left.Column.CompareTo(right.Column);
            if (column != 0)
            {
                return column;
            }

            int branch = left.Branch.CompareTo(right.Branch);
            if (branch != 0)
            {
                return branch;
            }

            return left.Point.Index.CompareTo(right.Point.Index);
        }

        private static string GetOriginKey(MapNode origin)
        {
            return origin != null
                ? origin.Distance + ":" + origin.Branch + ":" + origin.Point.Index + ":" + origin.Point.Kind
                : "start";
        }

        private Message NoForward(string context)
        {
            LogNoForward(context);
            return MapNodeAnnouncementFormatter.NoForward();
        }

        private void LogNoForward(string context)
        {
            try
            {
                global::SaveManager saveManager = _handler.SaveManager;
                int currentDistance = saveManager?.GetCurrentDistance() ?? -1;
                string gameSequence = saveManager != null ? saveManager.GetGameSequence().ToString() : "none";
                string currentBranch = saveManager != null ? ((global::MapScreen.BranchSelection)saveManager.GetCurrentBranch()).ToString() : "none";
                bool previewFuture = !context.StartsWith("CycleChoice", StringComparison.Ordinal);
                List<MapNode> rawCandidates = GetRawImmediateCandidates(_virtualOrigin).Where(node => node != null).ToList();

                Log.Info("[AccessibilityMod] MapNavigation no-forward"
                    + " context=" + context
                    + " currentDistance=" + currentDistance
                    + " gameSequence=" + gameSequence
                    + " currentBranch=" + currentBranch
                    + " currentRingListActive=" + IsCurrentRingListActive()
                    + " previewFuture=" + previewFuture
                    + " choiceOriginKey=" + (_choiceOriginKey ?? "null")
                    + " virtualChoiceOriginKey=" + (_virtualChoiceOriginKey ?? "null")
                    + " choiceIndex=" + _choiceIndex
                    + "/" + _choiceNodes.Count
                    + " virtualChoiceIndex=" + _virtualChoiceIndex
                    + "/" + _virtualChoices.Count
                    + " virtualStack=" + DescribeVirtualStackForLog(saveManager)
                    + " actualCurrent=" + DescribeNodeForLog(CurrentNode, saveManager)
                    + " virtualOrigin=" + DescribeNodeForLog(_virtualOrigin, saveManager)
                    + " virtualSelection=" + DescribeNodeForLog(_virtualSelection, saveManager)
                    + " rawCandidates=" + rawCandidates.Count
                    + " acceptedChoices=" + _virtualChoices.Count);

                Log.Info("[AccessibilityMod] MapNavigation no-forward actualChoices=" + DescribeNodeListForLog(_choiceNodes, saveManager));
                Log.Info("[AccessibilityMod] MapNavigation no-forward virtualChoices=" + DescribeNodeListForLog(_virtualChoices, saveManager));
                Log.Info("[AccessibilityMod] MapNavigation no-forward currentRingChoices=" + DescribeNodeListForLog(_handler.GetCurrentRingChoices(includeBattle: true), saveManager));
                Log.Info("[AccessibilityMod] MapNavigation no-forward currentRoute=" + DescribeNodeListForLog(
                    GetCurrentRouteNodes(currentDistance, saveManager != null ? (global::MapScreen.BranchSelection)saveManager.GetCurrentBranch() : global::MapScreen.BranchSelection.NoBranch),
                    saveManager));

                for (int i = 0; i < rawCandidates.Count; i++)
                {
                    MapNode candidate = rawCandidates[i];
                    Log.Info("[AccessibilityMod] MapNavigation no-forward candidate[" + i + "] "
                        + DescribeNodeForLog(candidate, saveManager)
                        + " rejectReason=" + GetCandidateRejectReason(_virtualOrigin, candidate, currentDistance, previewFuture, saveManager));
                }

                LogGraphSnapshot(currentDistance, saveManager);
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] MapNavigation no-forward diagnostics failed: " + ex);
            }
        }

        private IEnumerable<MapNode> GetRawImmediateCandidates(MapNode origin)
        {
            int currentDistance = _handler.SaveManager?.GetCurrentDistance() ?? 0;
            if (origin != null)
            {
                return origin.ForwardEdges.Select(edge => edge.To);
            }

            return _handler.Nodes.Where(node => node.Distance == currentDistance && node.Point.Kind == MapPoint.PointKind.Reward);
        }

        private static string GetCandidateRejectReason(MapNode origin, MapNode candidate, int currentDistance, bool previewFuture, global::SaveManager saveManager)
        {
            if (candidate == null)
            {
                return "null";
            }

            if (SameNodeIdentity(origin, candidate))
            {
                return "same-node";
            }

            if (candidate.Distance < currentDistance)
            {
                return "behind-current-distance";
            }

            if (previewFuture)
            {
                if (candidate.Point.Kind == MapPoint.PointKind.Battle)
                {
                    return "accepted-preview-battle";
                }

                if (candidate.Distance > currentDistance)
                {
                    return "accepted-preview-future-distance";
                }
            }

            if (!candidate.CanBeReached(saveManager))
            {
                return candidate.IsVisited(saveManager) ? "visited" : "cannot-be-reached";
            }

            return "accepted";
        }

        private void LogGraphSnapshot(int currentDistance, global::SaveManager saveManager)
        {
            List<MapNode> nodes = _handler.Nodes
                .Where(node => node.Distance >= currentDistance - 1 && node.Distance <= currentDistance + 2)
                .OrderBy(node => node.Distance)
                .ThenBy(node => node.Branch)
                .ThenBy(node => node.Point.Kind)
                .ThenBy(node => node.Point.Index)
                .ToList();

            Log.Info("[AccessibilityMod] MapNavigation no-forward graphSnapshot distances="
                + (currentDistance - 1)
                + ".."
                + (currentDistance + 2)
                + " nodes="
                + nodes.Count
                + " totalNodes="
                + _handler.Nodes.Count);

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNode node = nodes[i];
                Log.Info("[AccessibilityMod] MapNavigation graph node[" + i + "] "
                    + DescribeNodeForLog(node, saveManager)
                    + " outgoing=" + DescribeEdgesForLog(node.ForwardEdges, describeOutgoing: true, saveManager)
                    + " incoming=" + DescribeEdgesForLog(node.BackwardEdges, describeOutgoing: false, saveManager));
            }
        }

        private string DescribeVirtualStackForLog(global::SaveManager saveManager)
        {
            if (_virtualOriginStack.Count == 0)
            {
                return "[]";
            }

            List<MapNode> origins = new List<MapNode>();
            for (int i = 0; i < _virtualOriginStack.Count; i++)
            {
                origins.Add(_virtualOriginStack[i].Origin);
            }

            return DescribeNodeListForLog(origins, saveManager);
        }

        private static string DescribeNodeListForLog(IList<MapNode> nodes, global::SaveManager saveManager)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return "[]";
            }

            StringBuilder builder = new StringBuilder("[");
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                builder.Append(DescribeNodeIdForLog(nodes[i], saveManager));
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string DescribeEdgesForLog(IList<MapEdge> edges, bool describeOutgoing, global::SaveManager saveManager)
        {
            if (edges == null || edges.Count == 0)
            {
                return "[]";
            }

            StringBuilder builder = new StringBuilder("[");
            for (int i = 0; i < edges.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                MapNode other = describeOutgoing ? edges[i]?.To : edges[i]?.From;
                builder.Append(DescribeNodeIdForLog(other, saveManager));
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string DescribeNodeForLog(MapNode node, global::SaveManager saveManager)
        {
            if (node == null)
            {
                return "null";
            }

            string name = string.Empty;
            try
            {
                name = node.GetDisplayName();
            }
            catch (Exception ex)
            {
                name = "name-error:" + ex.GetType().Name;
            }

            return "{kind=" + node.Point.Kind
                + ", name=\"" + name + "\""
                + ", distance=" + node.Distance
                + ", row=" + node.Row
                + ", col=" + node.Column
                + ", branch=" + node.Branch
                + ", index=" + node.Point.Index
                + ", outgoing=" + node.ForwardEdges.Count
                + ", incoming=" + node.BackwardEdges.Count
                + ", reachable=" + node.CanBeReached(saveManager)
                + ", visited=" + node.IsVisited(saveManager)
                + ", selectable=" + node.CanBeSelected()
                + ", active=" + (node.Target != null && node.Target.activeInHierarchy)
                + "}";
        }

        private static string DescribeNodeIdForLog(MapNode node, global::SaveManager saveManager)
        {
            if (node == null)
            {
                return "null";
            }

            return node.Point.Kind
                + ":d"
                + node.Distance
                + ":"
                + node.Branch
                + ":i"
                + node.Point.Index
                + ":r"
                + node.Row
                + "c"
                + node.Column
                + ":reachable="
                + node.CanBeReached(saveManager)
                + ":visited="
                + node.IsVisited(saveManager)
                + ":selectable="
                + node.CanBeSelected();
        }

        private static bool ContainsEquivalent(List<MapNode> nodes, MapNode candidate)
        {
            return FindEquivalentIndex(nodes, candidate) >= 0;
        }

        private static int FindEquivalentIndex(IList<MapNode> nodes, MapNode candidate)
        {
            if (nodes == null || candidate == null)
            {
                return -1;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (SameNodeIdentity(nodes[i], candidate))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool SameNodeIdentity(MapNode left, MapNode right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (left.Point.Kind != right.Point.Kind || left.Distance != right.Distance)
            {
                return false;
            }

            if (left.Point.Kind == MapPoint.PointKind.Battle)
            {
                return true;
            }

            return left.Branch == right.Branch && left.Point.Index == right.Point.Index;
        }

        private sealed class VirtualCursorFrame
        {
            public VirtualCursorFrame(
                MapNode origin,
                IEnumerable<MapNode> choices,
                int choiceIndex,
                string choiceOriginKey)
            {
                Origin = origin;
                Choices = new List<MapNode>(choices ?? Enumerable.Empty<MapNode>());
                ChoiceIndex = choiceIndex;
                ChoiceOriginKey = choiceOriginKey;
            }

            public MapNode Origin { get; }
            public List<MapNode> Choices { get; }
            public int ChoiceIndex { get; }
            public string ChoiceOriginKey { get; }
        }
    }
}
