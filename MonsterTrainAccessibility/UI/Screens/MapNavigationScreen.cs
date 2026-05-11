using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Map;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class MapNavigationScreen : GameScreen
    {
        private static readonly System.Reflection.FieldInfo SectionsField = AccessTools.Field(typeof(global::MapScreen), "sections")!;

        private readonly global::MapScreen _screen;
        private MapHandler _handler;
        private TreeMapViewer _viewer;
        private HudNavigationScreen _hudNavigationScreen;
        private readonly Dictionary<MapNode, ProxyMapPoint> _elementsByNode = new Dictionary<MapNode, ProxyMapPoint>();
        private MapNode _choicePrefixNode;
        private bool _allowFocusAnnouncements;

        public static MapNavigationScreen Current { get; private set; }

        public MapNavigationScreen(global::MapScreen screen)
        {
            _screen = screen;
            ClaimAction("ui_up");
            ClaimAction("ui_down");
            ClaimAction("ui_left");
            ClaimAction("ui_right");
            ClaimAction("buffer_prev_item");
            ClaimAction("buffer_next_item");
            ClaimAction("buffer_prev");
            ClaimAction("buffer_next");
        }

        public override void OnPush()
        {
            Current = this;
            base.OnPush();
            _allowFocusAnnouncements = false;
            SelectCurrentNode();
        }

        public override void OnPop()
        {
            if (Current == this)
            {
                Current = null;
            }

            base.OnPop();
        }

        public override void OnUpdate()
        {
            SyncHudNavigation();
            if (_viewer == null)
            {
                Rebuild();
            }

            EnsureInitialSelection();
        }

        public override bool ShouldAnnounceFocus(UIElement element) => _allowFocusAnnouncements;

        public override bool ShouldRestoreNavigationFocus() => false;

        public override UIElement GetElement(GameObject go)
        {
            UIElement element = base.GetElement(go);
            if (element != null)
            {
                return element;
            }

            MapNode node = _handler?.GetNodeForTarget(go);
            if (node != null && _elementsByNode.TryGetValue(node, out ProxyMapPoint proxy))
            {
                return proxy;
            }

            return null;
        }

        public void UpdateStartPoint(MapNode node)
        {
            if (node == null || _viewer == null || ReferenceEquals(_viewer.CurrentNode, node))
            {
                return;
            }

            _viewer.SetStartNode(node);
        }

        public Message DescribePoint(MapNode node, bool includeChoicePrefix)
        {
            if (node == null)
            {
                return null;
            }

            bool shouldPrefixChoice = includeChoicePrefix || ReferenceEquals(_choicePrefixNode, node);
            if (ReferenceEquals(_choicePrefixNode, node))
            {
                _choicePrefixNode = null;
            }
            return MapNodeAnnouncementFormatter.DescribeNode(node, new[] { node }, shouldPrefixChoice);
        }

        public bool ActivateNode(MapNode node)
        {
            if (node == null || _handler?.SaveManager == null)
            {
                return false;
            }

            if (!node.CanBeReached(_handler.SaveManager))
            {
                Core.Log.Info("[AccessibilityMod] MapNavigation blocked activation of unreachable node: "
                    + node.GetDisplayName()
                    + " distance=" + node.Distance
                    + " branch=" + node.Branch
                    + " visited=" + node.IsVisited(_handler.SaveManager));
                return false;
            }

            if (_handler.SaveManager.GetGameSequence() == global::SaveData.GameSequence.LeavingBattle &&
                node.Branch != global::MapScreen.BranchSelection.NoBranch)
            {
                _handler.TryHighlightBranch(node);
                _screen.ChooseBranch(node.Branch);
                return true;
            }

            return false;
        }

        public bool CanSubmitNode(MapNode node)
        {
            return node != null && _handler?.SaveManager != null && node.CanBeReached(_handler.SaveManager) && node.CanBeSelected();
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            if (_viewer == null && !Rebuild())
            {
                return true;
            }

            Message result = null;
            bool selectChoice = false;
            MapNode previousChoiceNode = _viewer.CurrentNode;
            switch (action.Key)
            {
                case "ui_right":
                    if (_viewer.UsesCurrentRingList)
                    {
                        return true;
                    }

                    result = _viewer.CycleChoice(1);
                    selectChoice = true;
                    break;
                case "ui_left":
                    if (_viewer.UsesCurrentRingList)
                    {
                        return true;
                    }

                    result = _viewer.CycleChoice(-1);
                    selectChoice = true;
                    break;
                case "ui_up":
                    if (_viewer.UsesCurrentRingList)
                    {
                        result = _viewer.CycleChoice(1);
                        selectChoice = true;
                        break;
                    }
                    return true;
                case "ui_down":
                    if (_viewer.UsesCurrentRingList)
                    {
                        result = _viewer.CycleChoice(-1);
                        selectChoice = true;
                        break;
                    }

                    _viewer.SnapToCurrentOrigin();
                    _choicePrefixNode = null;
                    if (SelectCurrentNode())
                    {
                        _allowFocusAnnouncements = true;
                        AnnounceCurrentNode();
                    }
                    return true;
                case "buffer_prev_item":
                    result = _viewer.MoveVirtualCursor(0, 1);
                    break;
                case "buffer_next_item":
                    result = _viewer.MoveVirtualCursor(0, -1);
                    break;
                case "buffer_prev":
                    result = _viewer.MoveVirtualCursor(-1, 0);
                    break;
                case "buffer_next":
                    result = _viewer.MoveVirtualCursor(1, 0);
                    break;
                default:
                    return false;
            }

            if (selectChoice)
            {
                _choicePrefixNode = _viewer.LastIncludedChoicePrefix ? _viewer.CurrentNode : null;
                if (_viewer.IsCurrentNode(previousChoiceNode))
                {
                    _choicePrefixNode = null;
                    return true;
                }

                if (!SelectCurrentNode())
                {
                    SpeechManager.Output(result);
                    _choicePrefixNode = null;
                }
                else
                {
                    _allowFocusAnnouncements = true;
                    AnnounceCurrentNode();
                }
                return true;
            }

            _choicePrefixNode = null;
            SpeechManager.Output(result);

            return true;
        }

        private void AnnounceCurrentNode()
        {
            MapNode currentNode = _viewer?.CurrentNode;
            UIElement currentElement = currentNode != null ? GetElement(currentNode.Target) : null;
            if (currentNode?.Target == null || currentElement == null)
            {
                return;
            }

            UIManager.SetFocusedControl(currentNode.Target, currentElement);
            UIManager.ForceReannounceCurrentFocus();
        }

        protected override void BuildRegistry()
        {
            RootElement = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = false
            };

            Rebuild();
        }

        private bool Rebuild()
        {
            ClearRegistry();
            RootElement?.Clear();
            _elementsByNode.Clear();
            _handler = new MapHandler();
            if (!_handler.Build(_screen))
            {
                _viewer = null;
                return false;
            }

            _viewer = new TreeMapViewer(_handler);
            MapNode selected = _handler.GetNodeForTarget(EventSystem.current?.currentSelectedGameObject);
            MapNode start = GetStartNode(selected);
            if (start != null)
            {
                _viewer.SetStartNode(start);
            }

            RegisterNodes();
            return true;
        }

        private MapNode GetStartNode(MapNode selected)
        {
            global::SaveManager saveManager = _handler?.SaveManager;
            if (saveManager == null)
            {
                return selected ?? _handler?.GetDefaultNode();
            }

            if (saveManager.GetGameSequence() != global::SaveData.GameSequence.DestinationReached &&
                selected != null &&
                selected.CanBeReached(saveManager) &&
                selected.CanBeSelected())
            {
                return selected;
            }

            return _handler.GetDefaultNode();
        }

        private void RegisterNodes()
        {
            if (_handler == null)
            {
                return;
            }

            foreach (MapNode node in _handler.Nodes)
            {
                if (node == null || node.Target == null)
                {
                    continue;
                }

                ProxyMapPoint element = new ProxyMapPoint(node);
                _elementsByNode[node] = element;
                RootElement.Add(element);
                Register(node.Target, element);
                if (node.Point.RewardNode != null)
                {
                    Register(node.Point.RewardNode.gameObject, element);
                }
                if (node.Point.BattleNode != null)
                {
                    Register(node.Point.BattleNode.gameObject, element);
                }
            }
        }

        private bool SelectCurrentNode()
        {
            MapNode current = _viewer?.CurrentNode;
            if (current == null ||
                current.Target == null ||
                !current.Target.activeInHierarchy ||
                !current.CanBeReached(_handler?.SaveManager))
            {
                return false;
            }

            bool branchPreview = _handler?.SaveManager?.GetGameSequence() == global::SaveData.GameSequence.LeavingBattle &&
                current.Branch != global::MapScreen.BranchSelection.NoBranch;
            if (branchPreview)
            {
                _handler.TryHighlightBranch(current);
            }

            if (!current.CanBeSelected())
            {
                return false;
            }

            global::ShinyShoe.IGameUIComponent component = current.Target.GetComponent<global::ShinyShoe.IGameUIComponent>();
            if (component != null && global::InputManager.Inst != null)
            {
                bool selected = global::InputManager.Inst.SelectGameUIComponent(component, allowClearingSelection: false);
                if (selected)
                {
                    _handler?.TryHighlightBranch(current);
                }

                return selected;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            eventSystem.SetSelectedGameObject(current.Target);
            _handler?.TryHighlightBranch(current);
            return true;
        }

        private void EnsureInitialSelection()
        {
            if (_allowFocusAnnouncements || _viewer == null || !IsReadyForInitialFocusAnnouncement())
            {
                return;
            }

            if (!SelectCurrentNode())
            {
                return;
            }

            MapNode current = _viewer.CurrentNode;
            UIElement element = current != null ? GetElement(current.Target) : null;
            if (current?.Target != null && element != null)
            {
                UIManager.SetFocusedControl(current.Target, element);
                _allowFocusAnnouncements = true;
            }
        }

        private bool IsReadyForInitialFocusAnnouncement()
        {
            if (_screen == null || !_screen.gameObject.activeInHierarchy || !_screen.AllowUINavigation())
            {
                return false;
            }

            global::SaveManager saveManager = _handler?.SaveManager;
            if (saveManager == null || _handler.Nodes.Count == 0)
            {
                return false;
            }

            switch (saveManager.GetGameSequence())
            {
                case global::SaveData.GameSequence.Initial:
                case global::SaveData.GameSequence.LeavingBattle:
                case global::SaveData.GameSequence.DestinationReached:
                    return !IsActiveSectionScrolling(saveManager);
                case global::SaveData.GameSequence.TransitioningIntoSection:
                    return saveManager.GetCurrentDistance() == 0;
                default:
                    return false;
            }
        }

        private bool IsActiveSectionScrolling(global::SaveManager saveManager)
        {
            if (saveManager == null)
            {
                return false;
            }

            List<global::MapSection> sections = SectionsField.GetValue(_screen) as List<global::MapSection>;
            int index = saveManager.GetCurrentDistance();
            if (sections == null || index < 0 || index >= sections.Count)
            {
                return false;
            }

            global::MapSection section = sections[index];
            return section != null && section.Scrolling;
        }

        private void SyncHudNavigation()
        {
            global::Hud hud = GameManagers.GetScreenManager()?.GetScreen(global::ScreenName.Hud) as global::Hud;
            bool active = hud != null && hud.IsHudNavigationEnabled();
            if (active)
            {
                if (_hudNavigationScreen == null || _hudNavigationScreen.Parent == null)
                {
                    _hudNavigationScreen = new HudNavigationScreen(hud);
                    PushChild(_hudNavigationScreen);
                }
                return;
            }

            if (_hudNavigationScreen != null && _hudNavigationScreen.Parent != null)
            {
                RemoveChild(_hudNavigationScreen);
            }

            _hudNavigationScreen = null;
        }
    }
}
