using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI;
using ShinyShoe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.Map
{
    internal sealed class MapHandler
    {
        private static readonly FieldInfo SectionsField = AccessTools.Field(typeof(global::MapScreen), "sections")!;
        private static readonly FieldInfo SaveManagerField = AccessTools.Field(typeof(global::MapScreen), "saveManager")!;
        private static readonly FieldInfo MapDistanceField = AccessTools.Field(typeof(global::MapSection), "mapDistance")!;
        private static readonly FieldInfo MapNodesByBranchField = AccessTools.Field(typeof(global::MapSection), "mapNodesByBranch")!;
        private static readonly FieldInfo PreviousBattleNodeField = AccessTools.Field(typeof(global::MapSection), "previousBattleNode")!;
        private static readonly FieldInfo BranchChoiceUIField = AccessTools.Field(typeof(global::MapSection), "branchChoiceUI")!;
        private static readonly FieldInfo BranchButtonsField = AccessTools.Field(typeof(global::BranchChoiceUI), "branchButtons")!;
        private static readonly MethodInfo HighlightBranchMethod = AccessTools.Method(typeof(global::MapSection), "HighlightBranch")!;

        private readonly List<MapNode> _nodes = new List<MapNode>();
        private readonly List<MapEdge> _edges = new List<MapEdge>();
        private readonly Dictionary<GameObject, MapNode> _nodesByTarget = new Dictionary<GameObject, MapNode>();
        private readonly Dictionary<int, MapNode> _battleNodesByDistance = new Dictionary<int, MapNode>();
        private readonly Dictionary<int, global::MapSection> _sectionsByDistance = new Dictionary<int, global::MapSection>();

        private global::SaveManager _saveManager;
        private bool _isRegionRun;
        private int _regionMin;
        private int _regionMax;

        public IReadOnlyList<MapNode> Nodes => _nodes;
        public global::SaveManager SaveManager => _saveManager;

        public bool Build(global::MapScreen screen)
        {
            Clear();
            if (screen == null)
            {
                Log.Error("[AccessibilityMod] MapHandler: MapScreen is null");
                return false;
            }

            _saveManager = SaveManagerField.GetValue(screen) as global::SaveManager;
            List<global::MapSection> sections = SectionsField.GetValue(screen) as List<global::MapSection>;
            if (sections == null || sections.Count == 0)
            {
                return false;
            }

            InitializeRegionRunScope();

            for (int i = 0; i < sections.Count; i++)
            {
                AddSectionNodes(sections[i], i);
            }

            PruneRegionRunNodes();
            BuildEdges();
            AssignCoordinates();
            AddBranchChoiceAliases(sections);
            Log.Info("[AccessibilityMod] MapHandler: built graph with " + _nodes.Count + " nodes, " + _edges.Count + " edges");
            return _nodes.Count > 0;
        }

        public MapNode GetNodeForTarget(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            for (Transform current = target.transform; current != null; current = current.parent)
            {
                if (_nodesByTarget.TryGetValue(current.gameObject, out MapNode node))
                {
                    return node;
                }
            }

            return null;
        }

        public MapNode GetDefaultNode()
        {
            int currentDistance = _saveManager?.GetCurrentDistance() ?? 0;
            global::MapScreen.BranchSelection currentBranch =
                (global::MapScreen.BranchSelection)(_saveManager?.GetCurrentBranch() ?? 0);

            if (_saveManager?.GetGameSequence() == global::SaveData.GameSequence.DestinationReached)
            {
                List<MapNode> currentRingChoices = GetCurrentRingChoices(includeBattle: true);
                if (currentRingChoices.Count > 0)
                {
                    return currentRingChoices[0];
                }
            }

            MapNode preferred = _nodes
                .Where(node => node.Distance == currentDistance && node.Branch == currentBranch && node.CanBeSelected() && node.CanBeReached(_saveManager))
                .OrderBy(node => node.Point.Index)
                .FirstOrDefault();
            if (preferred != null)
            {
                return preferred;
            }

            preferred = _nodes
                .Where(node => node.Distance == currentDistance && node.CanBeSelected() && node.CanBeReached(_saveManager))
                .OrderBy(node => node.Branch)
                .ThenBy(node => node.Point.Index)
                .FirstOrDefault();
            if (preferred != null)
            {
                return preferred;
            }

            return _nodes.FirstOrDefault(node => node.CanBeSelected() && node.CanBeReached(_saveManager)) ?? _nodes.FirstOrDefault();
        }

        public MapNode GetBranchChoiceOrigin()
        {
            return _saveManager != null && _saveManager.GetGameSequence() == global::SaveData.GameSequence.LeavingBattle
                ? GetBattleNodeAtDistance(_saveManager.GetCurrentDistance())
                : null;
        }

        public List<MapNode> GetCurrentRingChoices(bool includeBattle)
        {
            List<MapNode> choices = new List<MapNode>();
            if (_saveManager == null)
            {
                return choices;
            }

            int currentDistance = _saveManager.GetCurrentDistance();
            global::MapScreen.BranchSelection currentBranch =
                (global::MapScreen.BranchSelection)_saveManager.GetCurrentBranch();

            AddCurrentRingNodes(choices, currentDistance, currentBranch, unvisitedOnly: true, merchantOnly: false);
            AddCurrentRingNodes(choices, currentDistance, global::MapScreen.BranchSelection.NoBranch, unvisitedOnly: true, merchantOnly: false);
            AddCurrentRingNodes(choices, currentDistance, currentBranch, unvisitedOnly: false, merchantOnly: true);
            AddCurrentRingNodes(choices, currentDistance, global::MapScreen.BranchSelection.NoBranch, unvisitedOnly: false, merchantOnly: true);
            AddCurrentRingNodes(choices, currentDistance, currentBranch, unvisitedOnly: false, merchantOnly: false);
            AddCurrentRingNodes(choices, currentDistance, global::MapScreen.BranchSelection.NoBranch, unvisitedOnly: false, merchantOnly: false);

            MapNode nextBattle = includeBattle ? GetBattleNodeAtDistance(currentDistance + 1) : null;
            if (nextBattle != null && nextBattle.CanBeReached(_saveManager) && nextBattle.CanBeSelected() && !choices.Contains(nextBattle))
            {
                choices.Add(nextBattle);
            }

            return choices;
        }

        public List<MapNode> GetNodesAtDistance(int distance)
        {
            return _nodes
                .Where(node => node.Distance == distance)
                .OrderBy(node => node.Branch)
                .ThenBy(node => node.Point.Index)
                .ToList();
        }

        public MapNode GetBattleNodeAtDistance(int distance)
        {
            return _battleNodesByDistance.TryGetValue(distance, out MapNode node) ? node : null;
        }

        public bool TryHighlightBranch(MapNode node)
        {
            if (node == null || node.Branch == global::MapScreen.BranchSelection.NoBranch)
            {
                return false;
            }

            if (!_sectionsByDistance.TryGetValue(node.Distance, out global::MapSection section) || section == null)
            {
                return false;
            }

            try
            {
                HighlightBranchMethod.Invoke(section, new object[] { node.Branch });
                return true;
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] MapHandler: failed to highlight branch " + node.Branch + " at distance " + node.Distance + ": " + ex);
                return false;
            }
        }

        private void Clear()
        {
            _nodes.Clear();
            _edges.Clear();
            _nodesByTarget.Clear();
            _battleNodesByDistance.Clear();
            _sectionsByDistance.Clear();
            _saveManager = null;
            _isRegionRun = false;
            _regionMin = 0;
            _regionMax = 0;
        }

        private void InitializeRegionRunScope()
        {
            _isRegionRun = _saveManager?.IsRegionRun == true;
            if (!_isRegionRun)
            {
                return;
            }

            int currentDistance = _saveManager.GetCurrentDistance();
            if (currentDistance == 0)
            {
                _regionMin = 0;
                _regionMax = 0;
            }
            else
            {
                int regionIndex = (currentDistance - 1) / 2;
                _regionMin = regionIndex * 2 + 1;
                _regionMax = _regionMin + 1;
            }

            Log.Info("[AccessibilityMod] MapHandler: region run scope currentDistance="
                + currentDistance
                + " regionMin="
                + _regionMin
                + " regionMax="
                + _regionMax
                + " exitBattleDistance="
                + (_regionMax + 1));
        }

        private void PruneRegionRunNodes()
        {
            if (!_isRegionRun)
            {
                return;
            }

            HashSet<MapNode> keep = new HashSet<MapNode>(_nodes.Where(IsInRegionRunScope));
            int before = _nodes.Count;
            _nodes.RemoveAll(node => !keep.Contains(node));

            List<GameObject> aliasesToRemove = _nodesByTarget
                .Where(pair => !keep.Contains(pair.Value))
                .Select(pair => pair.Key)
                .ToList();
            for (int i = 0; i < aliasesToRemove.Count; i++)
            {
                _nodesByTarget.Remove(aliasesToRemove[i]);
            }

            List<int> battlesToRemove = _battleNodesByDistance
                .Where(pair => !keep.Contains(pair.Value))
                .Select(pair => pair.Key)
                .ToList();
            for (int i = 0; i < battlesToRemove.Count; i++)
            {
                _battleNodesByDistance.Remove(battlesToRemove[i]);
            }

            Log.Info("[AccessibilityMod] MapHandler: region run pruned graph nodes "
                + before
                + " -> "
                + _nodes.Count);
        }

        private bool IsInRegionRunScope(MapNode node)
        {
            if (node == null)
            {
                return false;
            }

            if (node.Point.Kind == MapPoint.PointKind.Reward)
            {
                return node.Distance >= _regionMin && node.Distance <= _regionMax;
            }

            if (node.Point.Kind == MapPoint.PointKind.Battle)
            {
                return node.Distance >= _regionMin && node.Distance <= _regionMax + 1;
            }

            return false;
        }

        private void AddCurrentRingNodes(
            List<MapNode> choices,
            int currentDistance,
            global::MapScreen.BranchSelection branch,
            bool unvisitedOnly,
            bool merchantOnly)
        {
            List<MapNode> nodes = _nodes
                .Where(node => IsCurrentRingChoice(node, currentDistance, branch, unvisitedOnly, merchantOnly))
                .OrderBy(GetWorldY)
                .ThenBy(node => node.Point.Index)
                .ToList();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!choices.Contains(nodes[i]))
                {
                    choices.Add(nodes[i]);
                }
            }
        }

        private bool IsCurrentRingChoice(
            MapNode node,
            int currentDistance,
            global::MapScreen.BranchSelection branch,
            bool unvisitedOnly,
            bool merchantOnly)
        {
            if (node == null ||
                node.Point.Kind != MapPoint.PointKind.Reward ||
                node.Distance != currentDistance ||
                node.Branch != branch ||
                !node.CanBeReached(_saveManager) ||
                !node.CanBeSelected())
            {
                return false;
            }

            if (unvisitedOnly && node.IsVisited(_saveManager))
            {
                return false;
            }

            return !merchantOnly || IsMerchantNode(node);
        }

        private static bool IsMerchantNode(MapNode node)
        {
            return node?.Point.RewardNode?.GetData() is global::MerchantData;
        }

        private void AddSectionNodes(global::MapSection section, int fallbackDistance)
        {
            if (section == null)
            {
                return;
            }

            int distance = fallbackDistance;
            object distanceValue = MapDistanceField.GetValue(section);
            if (distanceValue is int reflectedDistance)
            {
                distance = reflectedDistance;
            }

            _sectionsByDistance[distance] = section;
            AddBattleNode(section, distance);
            Dictionary<global::MapScreen.BranchSelection, List<global::MapNodeUI>> nodesByBranch =
                MapNodesByBranchField.GetValue(section) as Dictionary<global::MapScreen.BranchSelection, List<global::MapNodeUI>>;
            if (nodesByBranch == null)
            {
                return;
            }

            foreach (KeyValuePair<global::MapScreen.BranchSelection, List<global::MapNodeUI>> pair in nodesByBranch)
            {
                List<global::MapNodeUI> branchNodes = pair.Value;
                if (branchNodes == null)
                {
                    continue;
                }

                for (int i = 0; i < branchNodes.Count; i++)
                {
                    AddRewardNode(branchNodes[i], distance, pair.Key, i);
                }
            }
        }

        private void AddRewardNode(global::MapNodeUI node, int distance, global::MapScreen.BranchSelection branch, int index)
        {
            if (node == null || node.GetData() == null || node.gameObject == null)
            {
                return;
            }

            GameUISelectableButton button = node.button;
            GameObject target = button != null ? button.gameObject : node.gameObject;
            MapPoint point = new MapPoint(
                MapPoint.PointKind.Reward,
                distance,
                branch,
                index,
                target,
                button,
                node,
                null,
                node.GetLocation());
            AddNode(new MapNode(point), node.gameObject, target);
        }

        private void AddBattleNode(global::MapSection section, int distance)
        {
            global::MapBattleNodeUI previousBattleNode = PreviousBattleNodeField.GetValue(section) as global::MapBattleNodeUI;
            global::MapBattleNodeUI alternateBattleNode = section.PreviousBattleNodeAlternate;
            global::MapBattleNodeUI battleNode = ChooseBattleNode(previousBattleNode, alternateBattleNode);
            bool forceRegionBattle = _isRegionRun && distance >= _regionMin && distance <= _regionMax + 1;
            if (battleNode == null)
            {
                LogSkippedRegionBattle(distance, "previousBattleNode and alternate are null");
                return;
            }

            if (battleNode.gameObject == null)
            {
                LogSkippedRegionBattle(distance, "gameObject is null");
                return;
            }

            if (!forceRegionBattle && !battleNode.gameObject.activeInHierarchy)
            {
                return;
            }

            GameUISelectableButton button = battleNode.button;
            GameObject target = button != null ? button.gameObject : battleNode.gameObject;
            if (!forceRegionBattle &&
                string.IsNullOrWhiteSpace(GetTooltipTitle(target)) &&
                string.IsNullOrWhiteSpace(GetTooltipBody(target)))
            {
                return;
            }

            if (forceRegionBattle &&
                (string.IsNullOrWhiteSpace(GetTooltipTitle(target)) && string.IsNullOrWhiteSpace(GetTooltipBody(target))))
            {
                Log.Info("[AccessibilityMod] MapHandler: included region battle without tooltip at distance "
                    + distance
                    + " active="
                    + battleNode.gameObject.activeInHierarchy
                    + " buttonActive="
                    + (button != null && button.gameObject.activeInHierarchy)
                    + " interactable="
                    + (button != null && button.interactable));
            }

            MapPoint point = new MapPoint(
                MapPoint.PointKind.Battle,
                distance,
                global::MapScreen.BranchSelection.NoBranch,
                -1,
                target,
                button,
                null,
                battleNode,
                null);
            MapNode node = new MapNode(point);
            List<GameObject> aliases = new List<GameObject>();
            AddBattleNodeAliases(aliases, previousBattleNode);
            AddBattleNodeAliases(aliases, alternateBattleNode);
            AddNode(node, aliases.ToArray());
            _battleNodesByDistance[distance] = node;
        }

        private static global::MapBattleNodeUI ChooseBattleNode(
            global::MapBattleNodeUI previousBattleNode,
            global::MapBattleNodeUI alternateBattleNode)
        {
            if (IsSelectableBattleNode(alternateBattleNode))
            {
                return alternateBattleNode;
            }

            if (IsSelectableBattleNode(previousBattleNode))
            {
                return previousBattleNode;
            }

            if (IsActiveBattleNode(alternateBattleNode))
            {
                return alternateBattleNode;
            }

            if (IsActiveBattleNode(previousBattleNode))
            {
                return previousBattleNode;
            }

            return alternateBattleNode ?? previousBattleNode;
        }

        private static bool IsSelectableBattleNode(global::MapBattleNodeUI battleNode)
        {
            if (!IsActiveBattleNode(battleNode))
            {
                return false;
            }

            GameUISelectableButton button = battleNode.button;
            return button != null &&
                button.gameObject != null &&
                button.gameObject.activeInHierarchy &&
                button.interactable &&
                button.state != GameUISelectableButton.State.Disabled &&
                button.state != GameUISelectableButton.State.Locked;
        }

        private static bool IsActiveBattleNode(global::MapBattleNodeUI battleNode)
        {
            return battleNode != null &&
                battleNode.gameObject != null &&
                battleNode.gameObject.activeInHierarchy;
        }

        private static void AddBattleNodeAliases(List<GameObject> aliases, global::MapBattleNodeUI battleNode)
        {
            if (battleNode == null)
            {
                return;
            }

            if (battleNode.gameObject != null && !aliases.Contains(battleNode.gameObject))
            {
                aliases.Add(battleNode.gameObject);
            }

            GameUISelectableButton button = battleNode.button;
            if (button != null && button.gameObject != null && !aliases.Contains(button.gameObject))
            {
                aliases.Add(button.gameObject);
            }
        }

        private void LogSkippedRegionBattle(int distance, string reason)
        {
            if (!_isRegionRun || distance < _regionMin || distance > _regionMax + 1)
            {
                return;
            }

            Log.Info("[AccessibilityMod] MapHandler: skipped region battle at distance "
                + distance
                + ": "
                + reason);
        }

        private void BuildEdges()
        {
            List<int> distances = _nodes.Select(node => node.Distance).Distinct().OrderBy(value => value).ToList();
            for (int i = 0; i < distances.Count; i++)
            {
                int distance = distances[i];
                List<MapNode> rewardNodes = _nodes
                    .Where(node => node.Point.Kind == MapPoint.PointKind.Reward && node.Distance == distance)
                    .OrderBy(node => node.Branch)
                    .ThenBy(node => node.Point.Index)
                    .ToList();

                MapNode entry = _battleNodesByDistance.TryGetValue(distance, out MapNode entryNode) ? entryNode : null;
                MapNode exit = _battleNodesByDistance.TryGetValue(distance + 1, out MapNode exitNode) ? exitNode : null;

                if (rewardNodes.Count == 0)
                {
                    AddEdge(entry, exit);
                    continue;
                }

                List<MapNode> sharedNodes = rewardNodes
                    .Where(node => node.Branch == global::MapScreen.BranchSelection.NoBranch)
                    .OrderBy(GetWorldY)
                    .ThenBy(node => node.Point.Index)
                    .ToList();
                List<IGrouping<global::MapScreen.BranchSelection, MapNode>> branchGroups = rewardNodes
                    .Where(node => node.Branch != global::MapScreen.BranchSelection.NoBranch)
                    .GroupBy(node => node.Branch)
                    .OrderBy(group => group.Key)
                    .ToList();

                if (branchGroups.Count == 0)
                {
                    WirePath(entry, sharedNodes, exit);
                    continue;
                }

                for (int branchIndex = 0; branchIndex < branchGroups.Count; branchIndex++)
                {
                    List<MapNode> branchNodes = branchGroups[branchIndex]
                        .OrderBy(GetWorldY)
                        .ThenBy(node => node.Point.Index)
                        .ToList();
                    WirePath(entry, branchNodes, sharedNodes.Count > 0 ? sharedNodes[0] : exit);
                }

                if (sharedNodes.Count > 0)
                {
                    WirePath(null, sharedNodes, exit);
                }
            }
        }

        private void WirePath(MapNode entry, List<MapNode> path, MapNode exit)
        {
            if (path == null || path.Count == 0)
            {
                AddEdge(entry, exit);
                return;
            }

            AddEdge(entry, path[0]);
            for (int nodeIndex = 0; nodeIndex < path.Count - 1; nodeIndex++)
            {
                AddEdge(path[nodeIndex], path[nodeIndex + 1]);
            }

            AddEdge(path[path.Count - 1], exit);
        }

        private void AssignCoordinates()
        {
            List<int> distances = _nodes.Select(node => node.Distance).Distinct().OrderBy(value => value).ToList();
            for (int distanceIndex = 0; distanceIndex < distances.Count; distanceIndex++)
            {
                int distance = distances[distanceIndex];
                List<MapNode> row = _nodes
                    .Where(node => node.Distance == distance)
                    .OrderBy(node => GetWorldX(node))
                    .ThenBy(node => node.Branch)
                    .ThenBy(node => node.Point.Index)
                    .ToList();
                for (int i = 0; i < row.Count; i++)
                {
                    row[i].Column = i + 1;
                }
            }
        }

        private static float GetWorldX(MapNode node)
        {
            if (node?.Target == null)
            {
                return 0f;
            }

            return node.Target.transform.position.x;
        }

        private static float GetWorldY(MapNode node)
        {
            if (node?.Target == null)
            {
                return 0f;
            }

            return node.Target.transform.position.y;
        }

        private void AddBranchChoiceAliases(List<global::MapSection> sections)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                global::MapSection section = sections[i];
                if (section == null)
                {
                    continue;
                }

                int distance = i;
                object distanceValue = MapDistanceField.GetValue(section);
                if (distanceValue is int reflectedDistance)
                {
                    distance = reflectedDistance;
                }

                global::BranchChoiceUI branchChoiceUI = BranchChoiceUIField.GetValue(section) as global::BranchChoiceUI;
                List<GameUISelectableButton> buttons = BranchButtonsField.GetValue(branchChoiceUI) as List<GameUISelectableButton>;
                if (buttons == null)
                {
                    continue;
                }

                for (int buttonIndex = 0; buttonIndex < buttons.Count; buttonIndex++)
                {
                    GameUISelectableButton button = buttons[buttonIndex];
                    global::MapScreen.BranchSelection branch = (global::MapScreen.BranchSelection)buttonIndex;
                    MapNode branchNode = _nodes
                        .Where(node => node.Distance == distance && node.Branch == branch)
                        .OrderBy(GetWorldY)
                        .ThenBy(node => node.Point.Index)
                        .FirstOrDefault();
                    if (button != null && branchNode != null)
                    {
                        RegisterAlias(button.gameObject, branchNode);
                    }
                }
            }
        }

        private void AddNode(MapNode node, params GameObject[] targets)
        {
            if (node == null)
            {
                return;
            }

            _nodes.Add(node);
            for (int i = 0; i < targets.Length; i++)
            {
                RegisterAlias(targets[i], node);
            }
        }

        private void RegisterAlias(GameObject target, MapNode node)
        {
            if (target == null || node == null)
            {
                return;
            }

            _nodesByTarget[target] = node;
        }

        private void AddEdge(MapNode from, MapNode to)
        {
            if (from == null || to == null || ReferenceEquals(from, to))
            {
                return;
            }

            for (int i = 0; i < from.ForwardEdges.Count; i++)
            {
                if (ReferenceEquals(from.ForwardEdges[i].To, to))
                {
                    return;
                }
            }

            MapEdge edge = new MapEdge(from, to);
            _edges.Add(edge);
            from.ForwardEdges.Add(edge);
            to.BackwardEdges.Add(edge);
        }

        private static string GetTooltipTitle(GameObject target)
        {
            TooltipProviderComponent provider = target != null ? target.GetComponentInChildren<TooltipProviderComponent>(includeInactive: true) : null;
            return TooltipText.FirstTitle(provider);
        }

        private static string GetTooltipBody(GameObject target)
        {
            TooltipProviderComponent provider = target != null ? target.GetComponentInChildren<TooltipProviderComponent>(includeInactive: true) : null;
            return TooltipText.FirstBody(provider);
        }
    }
}
