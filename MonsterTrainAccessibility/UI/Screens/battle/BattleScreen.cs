using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Events;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class BattleScreen : GameScreen
    {
        private static readonly FieldInfo HandUIField = AccessTools.Field(typeof(global::GameScreen), "handUI")!;
        private static readonly FieldInfo HandSelectionUIField = AccessTools.Field(typeof(global::HandUI), "handSelectionUI")!;
        private static readonly FieldInfo RoomUIField = AccessTools.Field(typeof(global::GameScreen), "roomUI")!;
        private static readonly FieldInfo BattleHudField = AccessTools.Field(typeof(global::GameScreen), "battleHud")!;
        private static readonly FieldInfo VictoryUIField = AccessTools.Field(typeof(global::GameScreen), "victoryUI")!;
        private static readonly FieldInfo HandSelectionInstructionsDescriptionLabelField = AccessTools.Field(typeof(global::HandSelectionUI), "instructionsDescriptionLabel")!;
        private static readonly FieldInfo HandSelectionHideButtonField = AccessTools.Field(typeof(global::HandSelectionUI), "hideButton")!;
        private static readonly FieldInfo HandSelectionConfirmButtonLabelField = AccessTools.Field(typeof(global::HandSelectionUI), "confirmButtonLabel")!;
        private static readonly FieldInfo HandSelectionCancelButtonLabelField = AccessTools.Field(typeof(global::HandSelectionUI), "cancelButtonLabel")!;
        private static readonly FieldInfo HandSelectionHideButtonLabelField = AccessTools.Field(typeof(global::HandSelectionUI), "hideButtonLabel")!;
        private static readonly FieldInfo UnitAbilityHandUIField = AccessTools.Field(typeof(global::UnitAbilitySelectionBehaviour), "unitAbilityHandUI")!;
        private static readonly FieldInfo RoomAbilityHandUIField = AccessTools.Field(typeof(global::RoomAbilitySelectionBehaviour), "roomAbilityHandUI")!;
        private static readonly FieldInfo RoomManagerField = AccessTools.Field(typeof(global::RoomUI), "roomManager")!;
        private static readonly FieldInfo RoomUISelectedRoomField = AccessTools.Field(typeof(global::RoomUI), "selectedRoom")!;
        private static readonly FieldInfo EnergyUIField = AccessTools.Field(typeof(global::BattleHud), "energyUI")!;
        private static readonly FieldInfo AbilityCounterUIField = AccessTools.Field(typeof(global::BattleHud), "abilityCounterUI")!;
        private static readonly FieldInfo AbilityCounterCountLabelField = AccessTools.Field(typeof(global::AbilityCounterUI), "countLabel")!;
        private static readonly FieldInfo EnergySelectableField = AccessTools.Field(typeof(global::EnergyUI), "gameUISelectable")!;
        private static readonly FieldInfo PossibleTargetsField = AccessTools.Field(typeof(global::CommonSelectionBehavior), "possibleTargets")!;
        private static readonly FieldInfo ActiveValidatorField = AccessTools.Field(typeof(global::CommonSelectionBehavior), "activeValidator")!;
        private static readonly PropertyInfo HasMovedTargetingCursorOutOfHandProperty = AccessTools.Property(typeof(global::CommonSelectionBehavior), "hasMovedTargetingCursorOutOfHand")!;
        private static readonly FieldInfo VictoryScoreHeaderField = AccessTools.Field(typeof(global::VictoryUI), "scoreSectionHeaderLabel")!;
        private static readonly FieldInfo VictoryTotalScoreLabelField = AccessTools.Field(typeof(global::VictoryUI), "totalScoreLabel")!;
        private static readonly FieldInfo VictoryTotalScoreAmountLabelField = AccessTools.Field(typeof(global::VictoryUI), "totalScoreAmountLabel")!;
        private static readonly FieldInfo VictoryScoreEntryContainerField = AccessTools.Field(typeof(global::VictoryUI), "scoreEntryContainer")!;
        private static readonly FieldInfo HudLunaCovenUIField = AccessTools.Field(typeof(global::Hud), "lunaCovenUI")!;
        private static readonly FieldInfo HudBattleTurnCounterField = AccessTools.Field(typeof(global::Hud), "battleTurnCounter")!;
        private static readonly FieldInfo HudBossTargetUisField = AccessTools.Field(typeof(global::Hud), "bossTargetUis")!;
        private static readonly FieldInfo HudSoulSaviorBossTargetUisField = AccessTools.Field(typeof(global::Hud), "soulSaviorBossTargetUis")!;
        private static readonly FieldInfo HudForgePointsUIField = AccessTools.Field(typeof(global::Hud), "forgePointsUI")!;
        private static readonly MethodInfo RoomUIGetTrainRoomAttachmentDisplayMethod = AccessTools.Method(typeof(global::RoomUI), "GetTrainRoomAttachmentDisplay")!;
        private static readonly MethodInfo RoomAbilitySetCurrentRoomAttachmentMethod = AccessTools.Method(typeof(global::RoomAbilitySelectionBehaviour), "SetCurrentRoomAttachment")!;

        private readonly global::GameScreen _screen;
        private readonly List<BattleLayer> _layers = new List<BattleLayer>();
        private readonly Dictionary<int, UIElement> _cardElements = new Dictionary<int, UIElement>();
        private readonly Dictionary<CharacterState, UIElement> _creatureElements = new Dictionary<CharacterState, UIElement>();
        private readonly Dictionary<int, UIElement> _floorTargetElements = new Dictionary<int, UIElement>();
        private readonly Dictionary<int, UIElement> _roomAbilityElements = new Dictionary<int, UIElement>();
        private readonly Dictionary<SpawnPoint, UIElement> _spawnPointElements = new Dictionary<SpawnPoint, UIElement>();
        private readonly BattleRoomNavigation _roomNavigation;
        private Dictionary<string, ProxyRoomAbility> _roomAbilityReuse;
        private CombatEventMonitor _eventMonitor;
        private CharacterVitalsSignalTracker _vitalsTracker;
        private int _layerIndex;
        private string _signature;
        private string _targetSignature;
        private UIElement _focusedElement;
        private UIElement _unitAbilityCardElement;
        private UIElement _roomAbilityCardElement;
        private ListContainer _victoryRoot;
        private bool _hasHandSelectionLayer;
        private bool _initialFocusApplied;
        private int _lastEnabledSelectedRoom = -1;

        public BattleScreen(global::GameScreen screen)
        {
            _screen = screen;
            _roomNavigation = new BattleRoomNavigation(this);
            ClaimAction("ui_up");
            ClaimAction("ui_down");
            ClaimAction("ui_scroll_up");
            ClaimAction("ui_scroll_down");
            ClaimAction("ui_left");
            ClaimAction("ui_right");
            ClaimAction("ui_accept");
            ClaimAction("ui_select");
            ClaimAction("read_ember");
            ClaimAction("read_forge_points");
            ClaimAction("read_unit_outcome");
            ClaimAction("read_floor_outcomes");
            ClaimAction("read_all_floor_outcomes");
            ClaimAction("read_floor_capacity");
            ClaimAction("read_all_floor_capacity");
            ClaimAction("jump_to_hand");
        }

        internal global::HandUI HandUI => Get<global::HandUI>(_screen, HandUIField);
        private global::HandSelectionUI HandSelectionUI => Get<global::HandSelectionUI>(HandUI, HandSelectionUIField);
        internal global::CardSelectionBehaviour SelectionBehaviour => HandUI?.GetSelectionBehaviour();
        internal global::UnitAbilitySelectionBehaviour UnitAbilitySelectionBehavior => HandUI?.GetUnitAbilitySelectionBehavior();
        internal global::UnitAbilityHandUI UnitAbilityHandUI => Get<global::UnitAbilityHandUI>(UnitAbilitySelectionBehavior, UnitAbilityHandUIField);
        internal global::RoomAbilitySelectionBehaviour RoomAbilitySelectionBehavior => HandUI?.GetRoomAbilitySelectionBehavior();
        internal global::RoomAbilityHandUI RoomAbilityHandUI => Get<global::RoomAbilityHandUI>(RoomAbilitySelectionBehavior, RoomAbilityHandUIField);
        internal global::RoomUI RoomUI => Get<global::RoomUI>(_screen, RoomUIField);
        private global::BattleHud BattleHud => Get<global::BattleHud>(_screen, BattleHudField);
        private global::VictoryUI VictoryUI => Get<global::VictoryUI>(_screen, VictoryUIField);
        private global::Hud Hud => Core.GameManagers.GetScreenManager()?.GetScreen(global::ScreenName.Hud) as global::Hud;
        private CombatManager CombatManager => AllGameManagers.Instance.OrNull()?.GetCombatManager();
        private MonsterManager MonsterManager => AllGameManagers.Instance.OrNull()?.GetMonsterManager();
        private RoomManager RoomManager => Get<RoomManager>(RoomUI, RoomManagerField);
        internal bool IsTargeting => GetActiveTargetingBehavior() != null;
        private bool IsVictoryActive => VictoryUI != null && VictoryUI.gameObject.activeInHierarchy;
        private bool AllowsNativeNavigation => _screen != null && _screen.AllowUINavigation();
        private bool AllowsBattleFocus => AllowsNativeNavigation &&
            CombatManager?.ShouldShowEndTurnButton() == true &&
            CombatManager.AllowExternalInput;
        private int FloorLayerCount => RoomManager?.GetNumRooms() ?? 3;
        private int HandLayerIndex => _hasHandSelectionLayer ? 1 : 0;
        private int FirstFloorLayerIndex => HandLayerIndex + 1;
        private int LastFloorLayerIndex => FirstFloorLayerIndex + FloorLayerCount - 1;
        private BattleLayer CurrentLayer => _layerIndex >= 0 && _layerIndex < _layers.Count ? _layers[_layerIndex] : null;
        private UIElement CurrentElement
        {
            get
            {
                BattleLayer layer = CurrentLayer;
                return layer != null && layer.FocusIndex >= 0 && layer.FocusIndex < layer.Items.Count ? layer.Items[layer.FocusIndex] : null;
            }
        }

        public override void OnPush()
        {
            base.OnPush();
            _vitalsTracker = new CharacterVitalsSignalTracker(AllGameManagers.Instance);
            _vitalsTracker.Start();
            _vitalsTracker.Prime();
            _eventMonitor = new CombatEventMonitor(AllGameManagers.Instance, _vitalsTracker);
            _eventMonitor.Start();
            _layerIndex = HandLayerIndex;
            if (ReplayAccessibilityState.IsSuppressed)
            {
                ClearFocusWhileBattleIsNotReady();
                return;
            }

            if (AllowsBattleFocus)
            {
                FocusCurrent();
                _initialFocusApplied = true;
                return;
            }

            ClearFocusWhileBattleIsNotReady();
        }

        public override void OnPop()
        {
            if (_focusedElement != null && _focusedElement.IsFocused)
            {
                _focusedElement.Unfocus();
            }

            _focusedElement = null;
            _eventMonitor?.Stop();
            _eventMonitor = null;
            _vitalsTracker?.Stop();
            _vitalsTracker = null;
            base.OnPop();
        }

        public override void OnUpdate()
        {
            if (ReplayAccessibilityState.IsSuppressed)
            {
                ClearFocusWhileBattleIsNotReady();
                _vitalsTracker?.FlushPending();
                return;
            }

            RememberCurrentEnabledRoom();
            string nextSignature = BuildBattleSignature();
            if (IsVictoryActive)
            {
                if (_victoryRoot == null || !string.Equals(nextSignature, _signature, StringComparison.Ordinal))
                {
                    BuildRegistry();
                    _victoryRoot?.FocusFirst();
                }

                _vitalsTracker?.FlushPending();
                return;
            }

            if (_victoryRoot != null)
            {
                BuildRegistry();
                FocusCurrent();
            }

            if (!AllowsBattleFocus)
            {
                ClearFocusWhileBattleIsNotReady();
                _vitalsTracker?.FlushPending();
                return;
            }

            if (!string.Equals(nextSignature, _signature, StringComparison.Ordinal))
            {
                RebuildPreservingFocus();
            }
            else if (!_initialFocusApplied)
            {
                FocusCurrent();
            }

            _initialFocusApplied = true;

            SyncTargetingFocus(force: false);
            _vitalsTracker?.FlushPending();
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            if (IsVictoryActive && _victoryRoot != null)
            {
                return _victoryRoot.HandleAction(action);
            }

            if (action.Key == "jump_to_hand")
            {
                return JumpToHand();
            }

            if (IsTargeting)
            {
                return HandleTargetingAction(action);
            }

            switch (action.Key)
            {
                case "read_ember":
                    return ReadEmber();
                case "read_forge_points":
                    return ReadForgePoints();
                case "read_unit_outcome":
                    return ReadFocusedUnitOutcome();
                case "read_floor_outcomes":
                    return ReadCurrentFloorOutcomes();
                case "read_all_floor_outcomes":
                    return ReadAllFloorOutcomes();
                case "read_floor_capacity":
                    return ReadCurrentFloorCapacity();
                case "read_all_floor_capacity":
                    return ReadAllFloorCapacity();
            }

            if (!AllowsBattleFocus)
            {
                return IsBattleControlAction(action.Key);
            }

            switch (action.Key)
            {
                case "ui_left":
                    return MoveHorizontal(-1);
                case "ui_right":
                    return MoveHorizontal(1);
                case "ui_up":
                case "ui_scroll_up":
                    return MoveVertical(1);
                case "ui_down":
                case "ui_scroll_down":
                    return MoveVertical(-1);
                case "ui_accept":
                case "ui_select":
                    return ActivateCurrent();
                default:
                    return false;
            }
        }

        public override bool ShouldAcceptGameSelection()
        {
            return !ReplayAccessibilityState.IsSuppressed;
        }

        public override bool ShouldRestoreNavigationFocus()
        {
            if (ReplayAccessibilityState.IsSuppressed)
            {
                return false;
            }

            if (IsWaitingForHandSelectionConfirmFocus())
            {
                return false;
            }

            return base.ShouldRestoreNavigationFocus();
        }

        public override bool ShouldAnnounceFocus(UIElement element)
        {
            if (ReplayAccessibilityState.IsSuppressed)
            {
                return false;
            }

            if (!IsVictoryActive && !IsTargeting && !AllowsBattleFocus)
            {
                return false;
            }

            return base.ShouldAnnounceFocus(element);
        }

        public override bool ShouldPropagate(string actionKey)
        {
            if (IsBattleControlAction(actionKey) && ShouldYieldBattleControlsToOverlay())
            {
                return true;
            }

            return base.ShouldPropagate(actionKey);
        }

        public override bool BlocksGameInput(InputAction action)
        {
            if (IsTargeting && (action?.Key == "ui_left" || action?.Key == "ui_right"))
            {
                return false;
            }

            if (IsBattleControlAction(action?.Key) && ShouldYieldBattleControlsToOverlay())
            {
                return false;
            }

            if (!AllowsBattleFocus && IsBattleMovementAction(action?.Key))
            {
                return true;
            }

            if (IsRoomAbilitySubmit(action))
            {
                return true;
            }

            return base.BlocksGameInput(action);
        }

        private bool IsRoomAbilitySubmit(InputAction action)
        {
            if (action == null || IsTargeting || !AllowsBattleFocus)
            {
                return false;
            }

            if (action.Key != "ui_accept" && action.Key != "ui_select")
            {
                return false;
            }

            return CurrentElement is ProxyRoomAbility;
        }

        private bool IsWaitingForHandSelectionConfirmFocus()
        {
            global::HandSelectionUI selection = HandSelectionUI;
            return selection != null &&
                selection.Active &&
                selection.CurrentlySelectedCardUI != null;
        }

        private bool ShouldYieldBattleControlsToOverlay()
        {
            bool sawOverlayAboveBattle = false;
            foreach (Screen screen in ScreenManager.WalkScreensDeepestFirst())
            {
                if (ReferenceEquals(screen, this))
                {
                    return sawOverlayAboveBattle;
                }

                if (screen is RewardScreen ||
                    screen is DraftScreen ||
                    screen is ChampionUpgradeScreen ||
                    screen is DeckScreen)
                {
                    sawOverlayAboveBattle = true;
                }
            }

            return false;
        }

        private static bool IsBattleControlAction(string actionKey)
        {
            switch (actionKey)
            {
                case "ui_left":
                case "ui_right":
                case "ui_up":
                case "ui_down":
                case "ui_scroll_up":
                case "ui_scroll_down":
                case "ui_accept":
                case "ui_select":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBattleMovementAction(string actionKey)
        {
            switch (actionKey)
            {
                case "ui_left":
                case "ui_right":
                case "ui_up":
                case "ui_down":
                case "ui_scroll_up":
                case "ui_scroll_down":
                    return true;
                default:
                    return false;
            }
        }

        private static bool ReadEmber()
        {
            PlayerManager playerManager = Core.GameManagers.GetPlayerManager();
            if (playerManager == null)
            {
                return false;
            }

            SpeechManager.Output(Message.Localized("ui", "SHORTCUT.EMBER", new
            {
                count = playerManager.GetEnergy()
            }));
            return true;
        }

        private static bool ReadForgePoints()
        {
            SaveManager saveManager = Core.GameManagers.GetSaveManager();
            if (saveManager == null)
            {
                return false;
            }

            SpeechManager.Output(Message.Localized("ui", "SHORTCUT.FORGE_POINTS", new
            {
                count = saveManager.GetForgePoints()
            }));
            return true;
        }

        private bool ReadFocusedUnitOutcome()
        {
            if (CurrentElement is ProxyCombatCreature creature)
            {
                SpeechManager.Output(CombatOutcomeDescriber.Describe(creature.Character, includeName: false));
            }

            return true;
        }

        private bool ReadCurrentFloorOutcomes()
        {
            if (!TryGetCurrentFloorLayer(out BattleLayer layer, out int _))
            {
                return true;
            }

            Message message = BuildFloorOutcomeMessage(layer, includeFloor: false);
            SpeechManager.Output(message ?? Message.Localized("combat", "COMBAT_OUTCOME.NO_CHANGES"));
            return true;
        }

        private bool ReadAllFloorOutcomes()
        {
            List<Message> floorMessages = new List<Message>();
            int roomCount = RoomManager?.GetNumRooms() ?? FloorLayerCount;
            for (int roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                int layerIndex = GetFloorLayerIndex(roomIndex);
                if (layerIndex < 0 || layerIndex >= _layers.Count)
                {
                    continue;
                }

                Message message = BuildFloorOutcomeMessage(_layers[layerIndex], includeFloor: true, roomIndex: roomIndex);
                if (message != null)
                {
                    floorMessages.Add(message);
                }
            }

            SpeechManager.Output(floorMessages.Count > 0
                ? Message.Join(". ", floorMessages)
                : Message.Localized("combat", "COMBAT_OUTCOME.NO_CHANGES"));
            return true;
        }

        private bool ReadCurrentFloorCapacity()
        {
            RoomState room = TryGetCurrentFloorRoom();
            if (room != null)
            {
                SpeechManager.Output(GetFloorCapacityEchoLabel(room));
            }

            return true;
        }

        private bool ReadAllFloorCapacity()
        {
            RoomManager roomManager = RoomManager;
            if (roomManager == null)
            {
                return true;
            }

            List<Message> floorMessages = new List<Message>();
            int roomCount = roomManager.GetNumRooms();
            for (int roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                RoomState room = roomManager.GetRoom(roomIndex);
                Message summary = GetFloorCapacityEchoLabel(room);
                if (summary == null)
                {
                    continue;
                }

                Message floor = Message.Localized("combat", "FLOOR", new { floor = roomIndex + 1 });
                floorMessages.Add(Message.Localized("combat", "FLOOR.CAPACITY_ECHO.FLOOR", new
                {
                    floor = floor.Resolve(),
                    summary = summary.Resolve()
                }));
            }

            SpeechManager.Output(floorMessages.Count > 0
                ? Message.Join(", ", floorMessages)
                : Message.Localized("combat", "FLOOR.NO_FOCUS"));
            return true;
        }

        private RoomState TryGetCurrentFloorRoom()
        {
            if (!TryGetCurrentFloorLayer(out BattleLayer _, out int roomIndex))
            {
                return null;
            }

            return TryGetRoom(roomIndex);
        }

        private bool TryGetCurrentFloorLayer(out BattleLayer layer, out int roomIndex)
        {
            layer = null;
            roomIndex = -1;
            int roomCount = RoomManager?.GetNumRooms() ?? FloorLayerCount;
            int firstFloor = FirstFloorLayerIndex;
            if (_layerIndex < firstFloor || _layerIndex >= firstFloor + roomCount || _layerIndex >= _layers.Count)
            {
                return false;
            }

            roomIndex = _layerIndex - firstFloor;
            layer = _layers[_layerIndex];
            return true;
        }

        private static Message BuildFloorOutcomeMessage(BattleLayer layer, bool includeFloor, int roomIndex = -1)
        {
            List<Message> outcomes = new List<Message>();
            if (layer != null)
            {
                for (int i = 0; i < layer.Items.Count; i++)
                {
                    if (!(layer.Items[i] is ProxyCombatCreature creature) || !creature.IsVisible)
                    {
                        continue;
                    }

                    if (!CombatOutcomeDescriber.HasChange(creature.Character))
                    {
                        continue;
                    }

                    Message outcome = CombatOutcomeDescriber.Describe(creature.Character, includeName: true);
                    if (outcome != null)
                    {
                        outcomes.Add(outcome);
                    }
                }
            }

            if (outcomes.Count == 0)
            {
                return null;
            }

            Message body = Message.Join(". ", outcomes);
            if (!includeFloor)
            {
                return body;
            }

            Message floor = Message.Localized("combat", "FLOOR", new { floor = roomIndex + 1 });
            return Message.Join(": ", floor, body);
        }

        private bool MoveHorizontal(int direction)
        {
            BattleLayer layer = CurrentLayer;
            if (layer == null || layer.VisibleCount <= 1)
            {
                return true;
            }

            int currentVisible = layer.GetVisibleOrdinal(layer.FocusIndex);
            if (currentVisible < 0)
            {
                currentVisible = 0;
            }

            int nextVisible = (currentVisible + direction + layer.VisibleCount) % layer.VisibleCount;
            layer.FocusIndex = layer.GetIndexForVisibleOrdinal(nextVisible);
            FocusCurrent();
            return true;
        }

        private bool MoveVertical(int direction)
        {
            if (direction < 0 && _layerIndex == 0)
            {
                return true;
            }

            if (direction > 0 && _layerIndex >= _layers.Count - 1)
            {
                return true;
            }

            _layerIndex = Mathf.Clamp(_layerIndex + direction, 0, _layers.Count - 1);
            FocusCurrent();
            return true;
        }

        private bool ActivateCurrent()
        {
            UIElement element = CurrentElement;
            IActivatableElement activatable = element as IActivatableElement;
            return activatable != null && activatable.Activate();
        }

        private bool JumpToHand()
        {
            if (HandLayerIndex < 0 || HandLayerIndex >= _layers.Count)
            {
                return false;
            }

            _layerIndex = HandLayerIndex;
            if (IsTargeting)
            {
                FocusSelectedCard(GetActiveTargetingBehavior());
                return true;
            }

            FocusCurrent();
            return true;
        }

        private bool TriggerHud(IGameUIComponent component)
        {
            global::Hud hud = Hud;
            if (hud == null || component == null)
            {
                return false;
            }

            if (global::InputManager.Inst != null)
            {
                global::InputManager.Inst.SelectGameUIComponent(component, allowClearingSelection: false);
            }

            CoreInputControlMapping mapping = new CoreInputControlMapping(global::InputManager.Controls.Submit, InputType.None, fake: true);
            return hud.ApplyScreenInput(mapping, component, global::InputManager.Controls.Submit);
        }

        internal bool RouteToNearestAbility()
        {
            CharacterState character = FindNearestAbilityCharacter();
            TrainRoomAttachmentState attachment = FindNearestAvailableRoomAbility(out int roomIndex);
            if (character == null && attachment == null)
            {
                Core.Log.Info("[AccessibilityMod] Ability counter: no available ability target.");
                return false;
            }

            int currentRoom = RoomManager != null ? RoomManager.CurrentSelectedRoom : 0;
            if (attachment != null && (character == null || Mathf.Abs(roomIndex - currentRoom) < Mathf.Abs(character.GetCurrentRoomIndex() - currentRoom)))
            {
                Core.Log.Info("[AccessibilityMod] Ability counter: routing focus to room ability in room " + roomIndex + ".");
                bool focusedRoom = FocusRoomAbility(roomIndex);
                Core.Log.Info("[AccessibilityMod] Ability counter: route " + (focusedRoom ? "succeeded." : "failed."));
                return focusedRoom;
            }

            Core.Log.Info("[AccessibilityMod] Ability counter: routing focus to " + character.GetName() + " in room " + character.GetCurrentRoomIndex() + ".");
            bool focusedCharacter = FocusCharacter(character);
            Core.Log.Info("[AccessibilityMod] Ability counter: route " + (focusedCharacter ? "succeeded." : "failed."));
            return focusedCharacter;
        }

        internal int CountAvailableAbilities()
        {
            MonsterManager monsterManager = AllGameManagers.Instance.OrNull()?.GetMonsterManager();
            int count = monsterManager?.GetNumCharactersWithAvailableAbility(includePreviewCharacters: false, includePyres: false) ?? 0;
            return count + CountAvailableRoomAbilities();
        }

        internal bool ActivateRoomAbility(int roomIndex, TrainRoomAttachmentState attachment)
        {
            if (attachment == null || !attachment.HasRoomAbility)
            {
                return false;
            }

            global::RoomAbilitySelectionBehaviour behavior = RoomAbilitySelectionBehavior;
            RoomState room = TryGetRoom(roomIndex);
            if (behavior == null || room == null)
            {
                Core.Log.Info("[AccessibilityMod] Room ability activation failed: missing behavior or room.");
                return false;
            }

            try
            {
                ClearOtherAbilitySelection();

                if (SelectionBehaviour?.IsInViewCharactersMode() == true)
                {
                    SelectionBehaviour.ExitViewCharactersMode();
                }

                TrainRoomAttachmentDisplay display = PrepareRoomAbilityDisplay(roomIndex, room, attachment);
                if (display == null)
                {
                    Core.Log.Info("[AccessibilityMod] Room ability activation failed: missing attachment display.");
                    return false;
                }

                RoomAbilitySetCurrentRoomAttachmentMethod.Invoke(behavior, new object[] { display });
                bool handled = behavior.HandleSubmit();
                if (!handled && behavior.IsCardFocusedOrSelected())
                {
                    handled = true;
                }

                _targetSignature = null;
                SyncTargetingFocus(force: true);
                return handled;
            }
            catch (Exception ex)
            {
                Core.Log.Info("[AccessibilityMod] Room ability activation failed: " + ex);
                return false;
            }
        }

        private TrainRoomAttachmentDisplay PrepareRoomAbilityDisplay(int roomIndex, RoomState room, TrainRoomAttachmentState attachment)
        {
            global::RoomUI roomUI = RoomUI;
            SaveManager saveManager = AllGameManagers.Instance.OrNull()?.GetSaveManager();
            if (roomUI == null || saveManager == null)
            {
                return null;
            }

            roomUI.StartCoroutine(roomUI.SetSelectedRoom(roomIndex, fromPlayerInput: true));
            roomUI.UpdateTrainRoomAttachmentDisplay(room);
            TrainRoomAttachmentDisplay display = RoomUIGetTrainRoomAttachmentDisplayMethod.Invoke(roomUI, null) as TrainRoomAttachmentDisplay;
            if (display == null)
            {
                return null;
            }

            display.Set(attachment, saveManager);
            display.gameObject.SetActive(display.HasIconSet);
            display.RefreshDisplay();
            return display;
        }

        private bool FocusCharacter(CharacterState character)
        {
            if (character == null || !_creatureElements.TryGetValue(character, out UIElement element))
            {
                Core.Log.Info("[AccessibilityMod] FocusCharacter failed: creature element missing.");
                return false;
            }

            int layerIndex = GetFloorLayerIndex(character.GetCurrentRoomIndex());
            if (layerIndex < 0 || layerIndex >= _layers.Count)
            {
                Core.Log.Info("[AccessibilityMod] FocusCharacter failed: layer " + layerIndex + " out of range.");
                return false;
            }

            _layerIndex = layerIndex;
            BattleLayer layer = CurrentLayer;
            int itemIndex = layer?.IndexOf(element) ?? -1;
            if (itemIndex < 0)
            {
                Core.Log.Info("[AccessibilityMod] FocusCharacter failed: element not in layer " + layerIndex + ".");
                return false;
            }

            Core.Log.Info("[AccessibilityMod] FocusCharacter: " + character.GetName() + " layer=" + layerIndex + " index=" + itemIndex + ".");
            layer.FocusIndex = itemIndex;
            FocusElement(element, selectForNavigation: true);
            Core.UIManager.ForceReannounceCurrentFocus();
            return true;
        }

        private bool FocusRoomAbility(int roomIndex)
        {
            if (!_roomAbilityElements.TryGetValue(roomIndex, out UIElement element))
            {
                Core.Log.Info("[AccessibilityMod] FocusRoomAbility failed: room ability element missing.");
                return false;
            }

            int layerIndex = GetFloorLayerIndex(roomIndex);
            if (layerIndex < 0 || layerIndex >= _layers.Count)
            {
                Core.Log.Info("[AccessibilityMod] FocusRoomAbility failed: layer " + layerIndex + " out of range.");
                return false;
            }

            _layerIndex = layerIndex;
            BattleLayer layer = CurrentLayer;
            int itemIndex = layer?.IndexOf(element) ?? -1;
            if (itemIndex < 0)
            {
                Core.Log.Info("[AccessibilityMod] FocusRoomAbility failed: element not in layer " + layerIndex + ".");
                return false;
            }

            layer.FocusIndex = itemIndex;
            Core.Log.Info("[AccessibilityMod] FocusRoomAbility: room=" + roomIndex +
                ", layer=" + layerIndex +
                ", index=" + itemIndex +
                ", nativeBefore=" + DescribeNativeSelection() + ".");
            FocusElement(element, selectForNavigation: true);
            Core.Log.Info("[AccessibilityMod] FocusRoomAbility complete: room=" + roomIndex +
                ", focused=" + DescribeElement(_focusedElement) +
                ", nativeAfter=" + DescribeNativeSelection() +
                ", targeting=" + IsTargeting +
                ", roomAbilityFocusedOrSelected=" + (RoomAbilitySelectionBehavior?.IsCardFocusedOrSelected() == true) + ".");
            Core.UIManager.ForceReannounceCurrentFocus();
            return true;
        }

        private void ClearOtherAbilitySelection()
        {
            try
            {
                SelectionBehaviour?.UnFocusCard(true);
                UnitAbilitySelectionBehavior?.UnFocusCard(true);
            }
            catch (Exception ex)
            {
                Core.Log.Info("[AccessibilityMod] Room ability activation selection cleanup failed: " + ex);
            }
        }

        private void FocusCurrent()
        {
            BattleLayer layer = CurrentLayer;
            if (layer == null)
            {
                return;
            }

            int index = layer.GetNearestVisibleIndex(layer.FocusIndex);
            if (index < 0)
            {
                return;
            }

            layer.FocusIndex = index;
            UIElement next = layer.Items[index];
            FocusElement(next, selectForNavigation: true);
        }

        private void ClearFocusWhileBattleIsNotReady()
        {
            if (_focusedElement != null && _focusedElement.IsFocused)
            {
                _focusedElement.Unfocus();
            }

            _focusedElement = null;
            Core.UIManager.SetFocusedElement(null);
        }

        private void FocusElement(UIElement next, bool selectForNavigation)
        {
            PrepareHandCardFocus(next);
            PrepareRoomAbilityFocus(next);
            if (ReferenceEquals(_focusedElement, next))
            {
                if (selectForNavigation)
                {
                    INavigationTargetElement currentTarget = _focusedElement as INavigationTargetElement;
                    currentTarget?.SelectForNavigation();
                }
                return;
            }

            if (_focusedElement != null && _focusedElement.IsFocused)
            {
                _focusedElement.Unfocus();
            }

            _focusedElement = next;
            _focusedElement.Focus();
            if (selectForNavigation)
            {
                INavigationTargetElement target = _focusedElement as INavigationTargetElement;
                target?.SelectForNavigation();
            }

            if (_focusedElement is ProxyRoomAbility)
            {
                ParkSelectionOnTower();
                Core.UIManager.SetFocusedElement(_focusedElement);
                return;
            }

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.activeInHierarchy)
            {
                Core.UIManager.SetFocusedControl(selected, _focusedElement);
            }
            else
            {
                Core.UIManager.SetFocusedElement(_focusedElement);
            }
        }

        private bool SyncToElement(UIElement element, string reason)
        {
            if (element == null || IsTargeting)
            {
                return false;
            }

            for (int layerIndex = 0; layerIndex < _layers.Count; layerIndex++)
            {
                BattleLayer layer = _layers[layerIndex];
                int itemIndex = layer.IndexOf(element);
                if (itemIndex < 0)
                {
                    continue;
                }

                _layerIndex = layerIndex;
                layer.FocusIndex = itemIndex;
                PrepareHandCardFocus(element);
                if (!ReferenceEquals(_focusedElement, element))
                {
                    if (_focusedElement != null && _focusedElement.IsFocused)
                    {
                        _focusedElement.Unfocus();
                    }

                    _focusedElement = element;
                    _focusedElement.Focus();
                }

                GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (selected != null && selected.activeInHierarchy)
                {
                    Core.UIManager.SetFocusedControl(selected, _focusedElement);
                }
                else
                {
                    Core.UIManager.SetFocusedElement(_focusedElement);
                }

                return true;
            }

            return false;
        }

        private void PrepareHandCardFocus(UIElement element)
        {
            if (!(element is ProxyCombatCard) || IsTargeting)
            {
                return;
            }

            global::CardSelectionBehaviour selectionBehavior = SelectionBehaviour;
            if (selectionBehavior?.IsInViewCharactersMode() == true)
            {
                selectionBehavior.ExitViewCharactersMode();
            }
        }

        private void PrepareRoomAbilityFocus(UIElement element)
        {
            if (!(element is ProxyRoomAbility) || IsTargeting)
            {
                return;
            }

            ParkSelectionOnTower();
        }

        internal bool PrepareFocusedCardForNativeSubmit(global::HandUI handUI)
        {
            if (handUI == null || !ReferenceEquals(handUI, HandUI) || IsTargeting)
            {
                return true;
            }

            if (!(CurrentElement is ProxyCombatCard card))
            {
                return true;
            }

            PrepareHandCardFocus(card);
            if (card.PrepareForNativeSubmit())
            {
                return PreselectPlayableRoomForCardSubmit(card);
            }

            return true;
        }

        internal void PrepareTargetingForNativeSubmit(global::HandUI handUI)
        {
            if (handUI == null || !ReferenceEquals(handUI, HandUI) || !IsTargeting)
            {
                return;
            }

            global::CommonSelectionBehavior targeting = GetActiveTargetingBehavior();
            if (targeting == null || !targeting.HasKeyboardSelectedTarget())
            {
                return;
            }

            targeting.UpdateKeyboardNavSplinePointer(force: true);
            HasMovedTargetingCursorOutOfHandProperty.SetValue(targeting, true, null);
        }

        private void RememberCurrentEnabledRoom()
        {
            global::RoomManager roomManager = RoomManager;
            int selectedRoom = roomManager != null ? roomManager.GetSelectedRoom() : -1;
            if (IsEnabledRoom(roomManager, selectedRoom))
            {
                _lastEnabledSelectedRoom = selectedRoom;
            }
        }

        private void RestoreEnabledRoomForCardSubmit()
        {
            global::RoomManager roomManager = RoomManager;
            int selectedRoom = roomManager != null ? roomManager.GetSelectedRoom() : -1;
            if (IsEnabledRoom(roomManager, selectedRoom))
            {
                _lastEnabledSelectedRoom = selectedRoom;
                return;
            }

            int fallbackRoom = FindEnabledRoom(roomManager, selectedRoom);
            if (fallbackRoom < 0)
            {
                return;
            }

            _roomNavigation.SelectRoom(fallbackRoom);
            _lastEnabledSelectedRoom = fallbackRoom;
            Core.Log.Info("[AccessibilityMod] Restored enabled battle room for card submit: " + selectedRoom + " -> " + fallbackRoom + ".");
        }

        private bool PreselectPlayableRoomForCardSubmit(ProxyCombatCard card)
        {
            global::RoomManager roomManager = RoomManager;
            CardManager cardManager = AllGameManagers.Instance.OrNull()?.GetCardManager();
            CardState cardState = card?.Card;
            if (roomManager == null || cardManager == null || cardState == null)
            {
                return true;
            }

            int selectedRoom = roomManager.GetSelectedRoom();
            if (CanPlayCardInRoom(cardManager, roomManager, cardState, selectedRoom))
            {
                RememberEnabledRoomForSubmit(roomManager, selectedRoom);
                return true;
            }

            int roomIndex = FindPlayableRoomForCard(cardManager, roomManager, cardState, selectedRoom);
            if (roomIndex < 0)
            {
                Core.Log.Info("[AccessibilityMod] No playable room for card submit; allowing native invalid feedback: " + Message.RawCleaned(cardState.GetTitle())?.Resolve() + ".");
                return true;
            }

            _roomNavigation.SelectRoom(roomIndex);
            RememberEnabledRoomForSubmit(roomManager, roomIndex);
            Core.Log.Info("[AccessibilityMod] Preselected playable room for card submit: " + selectedRoom + " -> " + roomIndex + ".");
            return true;
        }

        private int FindPlayableRoomForCard(CardManager cardManager, global::RoomManager roomManager, CardState card, int selectedRoom)
        {
            int roomCount = roomManager != null ? roomManager.GetNumRooms() : 0;
            if (roomCount <= 0)
            {
                return -1;
            }

            int lastRoom = IsPlayableRoomIndex(roomManager, _lastEnabledSelectedRoom) ? _lastEnabledSelectedRoom : -1;
            if (lastRoom >= 0 && CanPlayCardInRoom(cardManager, roomManager, card, lastRoom))
            {
                return lastRoom;
            }

            for (int offset = 1; offset < roomCount; offset++)
            {
                int up = selectedRoom + offset;
                if (CanPlayCardInRoom(cardManager, roomManager, card, up))
                {
                    return up;
                }

                int down = selectedRoom - offset;
                if (CanPlayCardInRoom(cardManager, roomManager, card, down))
                {
                    return down;
                }
            }

            for (int roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                if (CanPlayCardInRoom(cardManager, roomManager, card, roomIndex))
                {
                    return roomIndex;
                }
            }

            return -1;
        }

        private static bool CanPlayCardInRoom(CardManager cardManager, global::RoomManager roomManager, CardState card, int roomIndex)
        {
            if (!IsPlayableRoomIndex(roomManager, roomIndex))
            {
                return false;
            }

            return CanPlayCardInRoomScoped(cardManager, roomManager, card, roomIndex);
        }

        private static bool CanPlayCardInRoomScoped(CardManager cardManager, global::RoomManager roomManager, CardState card, int roomIndex)
        {
            global::RoomUI roomUI = roomManager?.GetRoomUI();
            if (roomUI == null)
            {
                return false;
            }

            int previousRoom = roomManager.GetSelectedRoom();
            bool changed = previousRoom != roomIndex;
            try
            {
                if (changed)
                {
                    RoomUISelectedRoomField.SetValue(roomUI, roomIndex);
                }

                global::CommonSelectionBehavior.SelectionError selectionError;
                bool canPlay = cardManager.CanPlayHandCard(card, roomIndex, null, null, null, out selectionError);
                if (!canPlay && changed)
                {
                    Core.Log.Info("[AccessibilityMod] Card room probe failed: room " + roomIndex + ", error " + selectionError + ".");
                }
                return canPlay;
            }
            finally
            {
                if (changed)
                {
                    RoomUISelectedRoomField.SetValue(roomUI, previousRoom);
                }
            }
        }

        private void RememberEnabledRoomForSubmit(global::RoomManager roomManager, int roomIndex)
        {
            if (IsEnabledRoom(roomManager, roomIndex))
            {
                _lastEnabledSelectedRoom = roomIndex;
            }
        }

        private void PreselectCorruptionRoomForCardSubmit(ProxyCombatCard card)
        {
            global::CardSelectionBehaviour selectionBehavior = SelectionBehaviour;
            global::RoomManager roomManager = RoomManager;
            CardState cardState = card?.Card;
            if (selectionBehavior == null || roomManager == null || cardState == null)
            {
                return;
            }

            selectionBehavior.CanSelectCard(out global::CommonSelectionBehavior.SelectionError selectionError);
            if (selectionError != global::CommonSelectionBehavior.SelectionError.InsufficientCorruption)
            {
                return;
            }

            int roomIndex = FindCorruptionRoomForCard(roomManager, cardState);
            if (roomIndex < 0)
            {
                return;
            }

            int selectedRoom = roomManager.GetSelectedRoom();
            if (selectedRoom == roomIndex)
            {
                return;
            }

            _roomNavigation.SelectRoom(roomIndex);
            _lastEnabledSelectedRoom = roomIndex;
            Core.Log.Info("[AccessibilityMod] Preselected charged echo room for card submit: " + selectedRoom + " -> " + roomIndex + ".");
        }

        private static int FindCorruptionRoomForCard(global::RoomManager roomManager, CardState card)
        {
            if (roomManager == null || card == null)
            {
                return -1;
            }

            int roomCount = roomManager.GetNumRooms();
            for (int roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                global::RoomState room = roomManager.GetRoom(roomIndex);
                if (room == null || !room.IsRoomEnabled() || room.GetIsPyreRoom())
                {
                    continue;
                }

                if (HasRequiredCorruptionInRoom(card, room))
                {
                    return roomIndex;
                }
            }

            return -1;
        }

        private static bool HasRequiredCorruptionInRoom(CardState card, global::RoomState room)
        {
            if (card == null || room == null)
            {
                return false;
            }

            int corruption = room.GetCurrentCorruption();
            if (card.HasTrait(typeof(CardTraitCorruptState)))
            {
                corruption++;
            }

            List<CardTraitState> traits = card.GetTraitStates();
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i] is CardTraitCorruptRestricted corruptRestricted &&
                    corruption < corruptRestricted.GetParamInt())
                {
                    return false;
                }
            }

            return true;
        }

        private int FindEnabledRoom(global::RoomManager roomManager, int selectedRoom)
        {
            if (roomManager == null)
            {
                return -1;
            }

            if (IsEnabledRoom(roomManager, _lastEnabledSelectedRoom))
            {
                return _lastEnabledSelectedRoom;
            }

            int roomCount = roomManager.GetNumRooms();
            for (int offset = 1; offset < roomCount; offset++)
            {
                int up = selectedRoom + offset;
                if (IsEnabledRoom(roomManager, up))
                {
                    return up;
                }

                int down = selectedRoom - offset;
                if (IsEnabledRoom(roomManager, down))
                {
                    return down;
                }
            }

            for (int roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                if (IsEnabledRoom(roomManager, roomIndex))
                {
                    return roomIndex;
                }
            }

            return -1;
        }

        private static bool IsEnabledRoom(global::RoomManager roomManager, int roomIndex)
        {
            global::RoomState room = roomIndex >= 0 && roomManager != null ? roomManager.GetRoom(roomIndex) : null;
            return room != null && room.IsRoomEnabled();
        }

        private static bool IsPlayableRoomIndex(global::RoomManager roomManager, int roomIndex)
        {
            return IsEnabledRoom(roomManager, roomIndex);
        }

        private bool HandleTargetingAction(InputAction action)
        {
            global::CommonSelectionBehavior selectionBehavior = GetActiveTargetingBehavior();
            if (selectionBehavior == null)
            {
                return true;
            }

            switch (action.Key)
            {
                case "ui_left":
                case "ui_right":
                    return true;
                case "ui_up":
                case "ui_scroll_up":
                    return MoveTargetingVertical(1);
                case "ui_down":
                case "ui_scroll_down":
                    return MoveTargetingVertical(-1);
                case "ui_accept":
                case "ui_select":
                    selectionBehavior.HandleSubmit();
                    _targetSignature = null;
                    SyncTargetingFocus(force: true);
                    return true;
                default:
                    return false;
            }
        }

        private void SyncTargetingFocus(bool force)
        {
            if (!IsTargeting)
            {
                _targetSignature = null;
                return;
            }

            string signature = BuildTargetSignature();
            if (!force && string.Equals(signature, _targetSignature, StringComparison.Ordinal))
            {
                return;
            }

            _targetSignature = signature;
            if (!TryResolveCurrentTarget(out UIElement element, out int layerIndex))
            {
                return;
            }

            _layerIndex = layerIndex;
            BattleLayer layer = CurrentLayer;
            int index = layer != null ? layer.IndexOf(element) : -1;
            if (index >= 0)
            {
                layer.FocusIndex = index;
            }

            FocusElement(element, selectForNavigation: false);
            if (force)
            {
                Core.UIManager.ForceReannounceCurrentFocus();
            }
        }

        private bool MoveTargetingVertical(int direction)
        {
            int nextLayer;
            if (direction > 0)
            {
                if (_layerIndex < HandLayerIndex)
                {
                    nextLayer = HandLayerIndex;
                }
                else if (_layerIndex < FirstFloorLayerIndex)
                {
                    nextLayer = FirstFloorLayerIndex;
                }
                else
                {
                    nextLayer = Mathf.Min(_layerIndex + 1, LastFloorLayerIndex);
                }
            }
            else
            {
                if (_layerIndex <= HandLayerIndex)
                {
                    return true;
                }
                nextLayer = _layerIndex - 1;
            }

            _layerIndex = nextLayer;
            if (_layerIndex == HandLayerIndex)
            {
                FocusSelectedCard(GetActiveTargetingBehavior());
                return true;
            }

            int roomIndex = _layerIndex - FirstFloorLayerIndex;
            _roomNavigation.SelectRoom(roomIndex);
            FocusFloorTarget(roomIndex);
            _targetSignature = null;
            return true;
        }

        private void FocusSelectedCard(global::CommonSelectionBehavior selectionBehavior)
        {
            if (selectionBehavior == null)
            {
                return;
            }

            if (ReferenceEquals(selectionBehavior, UnitAbilitySelectionBehavior) && _unitAbilityCardElement != null)
            {
                FocusElement(_unitAbilityCardElement, selectForNavigation: false);
                return;
            }

            if (ReferenceEquals(selectionBehavior, RoomAbilitySelectionBehavior) && _roomAbilityCardElement != null)
            {
                FocusElement(_roomAbilityCardElement, selectForNavigation: false);
                return;
            }

            int cardIndex = selectionBehavior.GetFocusedOrSelectedCardIndex();
            if (ReferenceEquals(selectionBehavior, SelectionBehaviour) && _cardElements.TryGetValue(cardIndex, out UIElement element))
            {
                FocusElement(element, selectForNavigation: false);
            }
        }

        private void FocusFloorTarget(int roomIndex)
        {
            if (_floorTargetElements.TryGetValue(roomIndex, out UIElement element))
            {
                FocusElement(element, selectForNavigation: false);
            }
        }

        private string BuildTargetSignature()
        {
            global::CommonSelectionBehavior selectionBehavior = GetActiveTargetingBehavior();
            if (selectionBehavior == null)
            {
                return string.Empty;
            }

            int roomIndex = RoomManager != null ? RoomManager.GetSelectedRoom() : -1;
            int targetIndex = selectionBehavior.GetKeyboardSelectedTarget();
            int cardIndex = selectionBehavior.GetFocusedOrSelectedCardIndex();
            return selectionBehavior.GetType().Name + ":" + roomIndex + ":" + targetIndex + ":" + cardIndex + ":" + selectionBehavior.IsCardSelected();
        }

        private bool TryResolveCurrentTarget(out UIElement element, out int layerIndex)
        {
            element = null;
            layerIndex = _layerIndex;

            global::CommonSelectionBehavior selectionBehavior = GetActiveTargetingBehavior();
            RoomManager roomManager = RoomManager;
            if (selectionBehavior == null || roomManager == null)
            {
                return false;
            }

            int selectedRoom = roomManager.GetSelectedRoom();
            if (selectedRoom < 0)
            {
                return false;
            }

            layerIndex = GetFloorLayerIndex(Mathf.Clamp(selectedRoom, 0, FloorLayerCount - 1));

            TargetValidator activeValidator = Get<TargetValidator>(selectionBehavior, ActiveValidatorField);
            if (activeValidator is RoomTargetValidator)
            {
                return _floorTargetElements.TryGetValue(selectedRoom, out element);
            }

            List<TargetValidator.TargetWrapper> targets = Get<List<TargetValidator.TargetWrapper>>(selectionBehavior, PossibleTargetsField);
            int targetIndex = selectionBehavior.GetKeyboardSelectedTarget();
            if (targets != null && targetIndex >= 0 && targetIndex < targets.Count)
            {
                TargetValidator.TargetWrapper target = targets[targetIndex];
                if (TryResolveTargetWrapper(target, selectedRoom, out element, out layerIndex))
                {
                    return true;
                }
            }

            return _floorTargetElements.TryGetValue(selectedRoom, out element);
        }

        private bool TryResolveTargetWrapper(TargetValidator.TargetWrapper target, int fallbackRoomIndex, out UIElement element, out int layerIndex)
        {
            element = null;
            layerIndex = GetFloorLayerIndex(Mathf.Clamp(fallbackRoomIndex, 0, FloorLayerCount - 1));
            if (target == null)
            {
                return false;
            }

            CharacterState character = target.GetParamCharacterState();
            if (character != null && _creatureElements.TryGetValue(character, out element))
            {
                layerIndex = GetFloorLayerIndex(Mathf.Clamp(character.GetCurrentRoomIndex(), 0, FloorLayerCount - 1));
                return true;
            }

            SpawnPoint spawnPoint = target.GetSpawnPoint();
            if (spawnPoint != null)
            {
                if (_spawnPointElements.TryGetValue(spawnPoint, out element))
                {
                    RoomState room = spawnPoint.GetRoomOwner();
                    if (room != null)
                    {
                        layerIndex = GetFloorLayerIndex(Mathf.Clamp(room.GetRoomIndex(), 0, FloorLayerCount - 1));
                    }
                    return true;
                }

                CharacterState spawnCharacter = spawnPoint.GetCharacterState();
                if (spawnCharacter != null && _creatureElements.TryGetValue(spawnCharacter, out element))
                {
                    layerIndex = GetFloorLayerIndex(Mathf.Clamp(spawnCharacter.GetCurrentRoomIndex(), 0, FloorLayerCount - 1));
                    return true;
                }
            }

            return _floorTargetElements.TryGetValue(fallbackRoomIndex, out element);
        }

        internal bool OnGameAbilityCounterInvoked()
        {
            bool focused = RouteToNearestAbility();
            if (focused)
            {
                ParkSelectionOnTower();
            }

            Core.Log.Info("[AccessibilityMod] Ability counter game hook: route " + (focused ? "succeeded." : "failed."));
            return focused;
        }

        public override UIElement GetElement(GameObject go)
        {
            if (!IsVictoryActive && !AllowsBattleFocus)
            {
                return null;
            }

            UIElement element = base.GetElement(go);
            if (IsVictoryActive)
            {
                UIElement focusedVictoryElement = _victoryRoot?.FocusedChild;
                if (focusedVictoryElement != null && element != null && !ReferenceEquals(element, focusedVictoryElement))
                {
                    return focusedVictoryElement;
                }

                return element;
            }

            if (IsHandSelectionLayerElement(element))
            {
                SyncToElement(element, go != null ? go.name : "native hand selection");
                return element;
            }

            BattleLayer layer = CurrentLayer;
            if (ShouldSyncSelectedElement(element, layer))
            {
                SyncToElement(element, go != null ? go.name : "selected object");
            }

            if (layer == null || _focusedElement == null)
            {
                return element;
            }

            if (IsTargeting)
            {
                return _focusedElement;
            }

            if (_layerIndex == HandLayerIndex)
            {
                return element;
            }

            if (element != null && layer.Contains(element))
            {
                return element;
            }

            return _focusedElement;
        }

        private bool ShouldSyncSelectedElement(UIElement element, BattleLayer layer)
        {
            if (!AllowsBattleFocus)
            {
                return false;
            }

            if (element == null || IsTargeting)
            {
                return false;
            }

            if (_focusedElement == null || layer == null)
            {
                return true;
            }

            if (_focusedElement is ProxyRoomAbility)
            {
                return ReferenceEquals(element, _focusedElement);
            }

            return layer.Contains(element);
        }

        protected override void BuildRegistry()
        {
            ClearDynamicState();

            if (IsVictoryActive)
            {
                BuildVictoryRegistry();
                RootElement = _victoryRoot;
                _signature = BuildBattleSignature();
                return;
            }

            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = false
            };
            RootElement = root;

            _hasHandSelectionLayer = ShouldAddHandSelectionLayer();
            if (_hasHandSelectionLayer)
            {
                AddHandSelectionLayer(root);
            }
            AddHandLayer(root);
            AddFloorLayers(root);
            AddTopPanelLayer(root);

            _signature = BuildBattleSignature();
        }

        internal void ClearBattleRegistry()
        {
            ClearRegistry();
        }

        private void ClearDynamicState()
        {
            _layers.Clear();
            _cardElements.Clear();
            _creatureElements.Clear();
            _floorTargetElements.Clear();
            _roomAbilityElements.Clear();
            _spawnPointElements.Clear();
            _unitAbilityCardElement = null;
            _roomAbilityCardElement = null;
            _victoryRoot = null;
            _hasHandSelectionLayer = false;
            ClearBattleRegistry();
        }

        internal void RegisterBattle(GameObject go, UIElement element)
        {
            Register(go, element);
        }

        internal void RegisterBattle(UIElement element, params GameObject[] targets)
        {
            Register(element, targets);
        }

        private RoomState TryGetRoom(int roomIndex)
        {
            RoomManager roomManager = RoomManager;
            if (roomManager == null || roomIndex < 0 || roomIndex >= roomManager.GetNumRooms())
            {
                return null;
            }

            return roomManager.GetRoom(roomIndex);
        }

        private GameObject GetRoomTarget(RoomState room)
        {
            CameraAnchor anchor = room?.GetCameraAnchor();
            return anchor != null ? anchor.gameObject : RoomUI != null ? RoomUI.gameObject : null;
        }

        private int GetFloorLayerIndex(int roomIndex)
        {
            return FirstFloorLayerIndex + roomIndex;
        }

        private BattleLayer AddLayer(Container root, string label, bool announceName = true, bool announcePosition = true)
        {
            BattleListContainer container = new BattleListContainer
            {
                ContainerLabel = label,
                AnnounceName = announceName,
                AnnouncePosition = announcePosition
            };
            root.Add(container);

            BattleLayer layer = new BattleLayer(container);
            _layers.Add(layer);
            return layer;
        }

        private void RebuildPreservingFocus()
        {
            int oldLayer = _layerIndex;
            int oldIndex = CurrentLayer?.FocusIndex ?? 0;
            bool hadHandSelectionLayer = _hasHandSelectionLayer;
            _roomAbilityReuse = BuildRoomAbilityReuseMap();
            try
            {
                BuildRegistry();
            }
            finally
            {
                _roomAbilityReuse = null;
            }
            UIElement nativeSelected = ResolveNativeSelectedElement();
            if (IsHandSelectionLayerElement(nativeSelected) && SyncToElement(nativeSelected, "native hand selection"))
            {
                return;
            }

            _layerIndex = Mathf.Clamp(RemapLayerIndex(oldLayer, hadHandSelectionLayer), 0, _layers.Count - 1);
            if (CurrentLayer != null)
            {
                CurrentLayer.FocusIndex = oldIndex;
            }
            FocusCurrent();
        }

        private Dictionary<string, ProxyRoomAbility> BuildRoomAbilityReuseMap()
        {
            if (_roomAbilityElements.Count == 0)
            {
                return null;
            }

            Dictionary<string, ProxyRoomAbility> reuse = new Dictionary<string, ProxyRoomAbility>();
            foreach (UIElement element in _roomAbilityElements.Values)
            {
                if (element is ProxyRoomAbility roomAbility)
                {
                    reuse[roomAbility.StableFocusKey] = roomAbility;
                }
            }

            return reuse.Count > 0 ? reuse : null;
        }

        private UIElement ResolveNativeSelectedElement()
        {
            IGameUIComponent selected = global::InputManager.Inst != null
                ? global::InputManager.Inst.GetSelectedGameUIComponent()
                : null;
            GameObject go = selected?.component != null ? selected.component.gameObject : null;
            return go != null ? base.GetElement(go) : null;
        }

        private bool IsHandSelectionLayerElement(UIElement element)
        {
            return _hasHandSelectionLayer && element != null && _layers.Count > 0 && _layers[0].Contains(element);
        }

        private int RemapLayerIndex(int oldLayer, bool hadHandSelectionLayer)
        {
            int oldHandLayer = hadHandSelectionLayer ? 1 : 0;
            int oldFirstFloor = oldHandLayer + 1;
            int oldLastFloor = oldFirstFloor + FloorLayerCount - 1;

            if (oldLayer == oldHandLayer)
            {
                return HandLayerIndex;
            }

            if (hadHandSelectionLayer && oldLayer == 0)
            {
                return _hasHandSelectionLayer ? 0 : HandLayerIndex;
            }

            if (oldLayer >= oldFirstFloor && oldLayer <= oldLastFloor)
            {
                return GetFloorLayerIndex(oldLayer - oldFirstFloor);
            }

            return _layers.Count - 1;
        }

        private void AddHandLayer(Container root)
        {
            BattleLayer layer = AddLayer(root, Message.Localized("combat", "HAND").Resolve());
            global::HandUI handUI = HandUI;
            List<global::CardUI> cards = handUI?.GetCardUIList();
            if (cards != null)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    AddCard(layer, cards[i], i);
                }
            }

            AddUnitAbilityCard(layer);
            AddRoomAbilityCard(layer);

            if (layer.Items.Count == 0)
            {
                GameObject target = handUI != null ? handUI.gameObject : null;
                ProxyEmptyHand empty = new ProxyEmptyHand(target);
                layer.Add(empty);
                RegisterBattle(target, empty);
            }
        }

        private bool ShouldAddHandSelectionLayer()
        {
            global::HandSelectionUI selection = HandSelectionUI;
            if (selection == null || !selection.Active)
            {
                return false;
            }

            return IsVisible(selection.GetConfirmButton())
                || IsVisible(selection.GetCancelButton())
                || IsVisible(Get<global::ShinyShoe.GameUISelectableButton>(selection, HandSelectionHideButtonField));
        }

        private void AddHandSelectionLayer(Container root)
        {
            BattleLayer layer = AddLayer(root, null, announceName: false);
            global::HandSelectionUI selection = HandSelectionUI;
            if (selection == null || !selection.Active)
            {
                return;
            }

            AddHandSelectionButton(
                layer,
                selection.GetConfirmButton(),
                selection,
                () => HandSelectionButtonLabel(selection, HandSelectionConfirmButtonLabelField, "HAND_SELECTION.CONFIRM"));
            AddHandSelectionButton(
                layer,
                selection.GetCancelButton(),
                selection,
                () => HandSelectionButtonLabel(selection, HandSelectionCancelButtonLabelField, "HAND_SELECTION.CANCEL"));
            AddHandSelectionButton(
                layer,
                Get<global::ShinyShoe.GameUISelectableButton>(selection, HandSelectionHideButtonField),
                selection,
                () => HandSelectionButtonLabel(selection, HandSelectionHideButtonLabelField, selection.Hidden ? "HAND_SELECTION.SHOW" : "HAND_SELECTION.HIDE"));
        }

        private void AddHandSelectionButton(
            BattleLayer layer,
            global::ShinyShoe.GameUISelectableButton button,
            global::HandSelectionUI selection,
            Func<Message> label)
        {
            if (!IsVisible(button))
            {
                return;
            }

            LabeledButton element = new LabeledButton(
                button,
                label,
                () => IsVisible(button) && selection != null && selection.Active,
                () => HandSelectionPrompt(selection));
            layer.Add(element);
            RegisterBattle(button.gameObject, element);
        }

        private Message HandSelectionButtonLabel(global::HandSelectionUI selection, FieldInfo labelField, string fallbackKey)
        {
            TMP_Text text = Get<TMP_Text>(selection, labelField);
            return Message.FromText(AccessibilityText.ReadLocalizedText(text)) ?? Message.Localized("ui", fallbackKey);
        }

        private Message HandSelectionPrompt(global::HandSelectionUI selection)
        {
            TMP_Text text = Get<TMP_Text>(selection, HandSelectionInstructionsDescriptionLabelField);
            return text != null && text.gameObject.activeInHierarchy
                ? Message.FromText(AccessibilityText.ReadLocalizedText(text))
                : null;
        }

        private static bool IsVisible(Component component)
        {
            return component != null && component.gameObject.activeInHierarchy;
        }

        private void AppendHandSignature(StringBuilder sb)
        {
            global::HandUI handUI = HandUI;
            List<global::CardUI> cards = handUI?.GetCardUIList();
            sb.Append("hand:");
            AppendHandSelectionSignature(sb);
            if (cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                global::CardUI card = cards[i];
                if (card == null || !card.gameObject.activeInHierarchy)
                {
                    continue;
                }

                sb.Append(card.GetInstanceID()).Append(':').Append(card.GetCardState()?.GetID()).Append('|');
            }

            global::UnitAbilityCardUI unitAbilityCard = UnitAbilityHandUI?.GetFocusedCardUI();
            if (unitAbilityCard != null && unitAbilityCard.gameObject.activeInHierarchy && unitAbilityCard.GetCardState() != null)
            {
                sb.Append("ability:")
                    .Append(unitAbilityCard.GetInstanceID())
                    .Append(':')
                    .Append(unitAbilityCard.GetCardState()?.GetID())
                    .Append('|');
            }

            global::RoomAbilityCardUI roomAbilityCard = RoomAbilityHandUI?.GetFocusedCardUI();
            if (roomAbilityCard != null && roomAbilityCard.gameObject.activeInHierarchy && roomAbilityCard.GetCardState() != null)
            {
                sb.Append("roomAbility:")
                    .Append(roomAbilityCard.GetInstanceID())
                    .Append(':')
                    .Append(roomAbilityCard.GetCardState()?.GetID())
                    .Append('|');
            }
        }

        private void AddCard(BattleLayer layer, global::CardUI cardUI, int index)
        {
            if (cardUI == null)
            {
                return;
            }

            IGameUIComponent selectable = cardUI.SelectableUI;
            if (selectable == null)
            {
                return;
            }

            ProxyCombatCard element = new ProxyCombatCard(
                cardUI,
                selectable,
                SelectionBehaviour,
                isSelected: IsHandSelectionCardSelected);
            layer.Add(element);
            _cardElements[index] = element;
            RegisterBattle(cardUI.gameObject, element);
            RegisterBattle(selectable.component != null ? selectable.component.gameObject : null, element);
        }

        private bool IsHandSelectionCardSelected(global::CardUI cardUI)
        {
            global::HandSelectionUI selection = HandSelectionUI;
            return cardUI != null &&
                selection != null &&
                selection.Active &&
                ReferenceEquals(selection.CurrentlySelectedCardUI, cardUI);
        }

        private void AddUnitAbilityCard(BattleLayer layer)
        {
            global::UnitAbilityCardUI cardUI = UnitAbilityHandUI?.GetFocusedCardUI();
            if (cardUI == null || cardUI.GetCardState() == null)
            {
                return;
            }

            IGameUIComponent selectable = cardUI.SelectableUI;
            if (selectable == null)
            {
                return;
            }

            ProxyUnitAbilityCard element = new ProxyUnitAbilityCard(cardUI, selectable);
            layer.Add(element);
            _unitAbilityCardElement = element;
            RegisterBattle(cardUI.gameObject, element);
            RegisterBattle(selectable.component != null ? selectable.component.gameObject : null, element);
        }

        private void AddRoomAbilityCard(BattleLayer layer)
        {
            global::RoomAbilityCardUI cardUI = RoomAbilityHandUI?.GetFocusedCardUI();
            if (cardUI == null || cardUI.GetCardState() == null)
            {
                return;
            }

            IGameUIComponent selectable = cardUI.SelectableUI;
            if (selectable == null)
            {
                return;
            }

            ProxyRoomAbilityCard element = new ProxyRoomAbilityCard(cardUI, selectable);
            layer.Add(element);
            _roomAbilityCardElement = element;
            RegisterBattle(cardUI.gameObject, element);
            RegisterBattle(selectable.component != null ? selectable.component.gameObject : null, element);
        }

        private void AddFloorLayers(Container root)
        {
            int roomCount = RoomManager?.GetNumRooms() ?? 3;
            for (int roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                AddFloorLayer(root, roomIndex);
            }
        }

        private void AppendFloorSignature(StringBuilder sb)
        {
            RoomManager roomManager = RoomManager;
            sb.Append(";rooms:");
            if (roomManager == null)
            {
                return;
            }

            for (int roomIndex = 0; roomIndex < roomManager.GetNumRooms(); roomIndex++)
            {
                RoomState room = TryGetRoom(roomIndex);
                sb.Append(roomIndex).Append(':');
                if (room == null)
                {
                    continue;
                }

                CapacityInfo capacity = room.GetCapacityInfo(Team.Type.Monsters);
                sb.Append("capacity=")
                    .Append(capacity.count)
                    .Append('/')
                    .Append(capacity.max)
                    .Append('|');

                AppendRoomAbilitySignature(sb, room);

                List<CharacterState> characters = new List<CharacterState>();
                room.AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters);
                characters.Sort(CompareCharactersForNavigation);
                for (int i = 0; i < characters.Count; i++)
                {
                    CharacterState character = characters[i];
                    sb.Append(character.GetInstanceID()).Append('|');
                }
            }
        }

        private void AddFloorLayer(Container root, int roomIndex)
        {
            RoomState room = TryGetRoom(roomIndex);
            BattleLayer layer = AddLayer(root, GetFloorLayerLabel(roomIndex, room));
            BattleRoomNavigation roomNavigation = _roomNavigation;
            AddFloorTarget(layer, roomIndex, roomNavigation);
            AddRoomAbility(layer, roomIndex, room, roomNavigation);
            if (room != null)
            {
                List<CharacterState> characters = new List<CharacterState>();
                room.AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters);
                characters.Sort(CompareCharactersForNavigation);
                for (int i = 0; i < characters.Count; i++)
                {
                    AddCreature(layer, characters[i], roomIndex);
                }

                AddSpawnPointTargets(layer, room, roomIndex);
            }

            if (layer.Items.Count == 0)
            {
                ProxyCombatFloor empty = new ProxyCombatFloor(
                    roomIndex,
                    "FLOOR.EMPTY",
                    true,
                    roomNavigation: roomNavigation);
                layer.Add(empty);
                GameObject target = GetRoomTarget(room);
                RegisterBattle(target, empty);
            }
        }

        private void AppendRoomAbilitySignature(StringBuilder sb, RoomState room)
        {
            TrainRoomAttachmentState attachment = FindRoomAbilityAttachment(room);
            if (attachment == null)
            {
                return;
            }

            CardState ability = ProxyRoomAbility.ResolveAbilityCard(attachment);
            sb.Append("roomAbility=")
                .Append(attachment.Guid)
                .Append(':')
                .Append(attachment.CurrentAbilityCooldown)
                .Append(':')
                .Append(ability?.GetID())
                .Append('|');
        }

        private void AddRoomAbility(BattleLayer layer, int roomIndex, RoomState room, BattleRoomNavigation roomNavigation)
        {
            TrainRoomAttachmentState attachment = FindRoomAbilityAttachment(room);
            if (attachment == null)
            {
                return;
            }

            string stableKey = ProxyRoomAbility.GetStableFocusKey(roomIndex, attachment);
            ProxyRoomAbility element = null;
            if (_roomAbilityReuse != null &&
                _roomAbilityReuse.TryGetValue(stableKey, out ProxyRoomAbility reusable) &&
                ReferenceEquals(reusable.Attachment, attachment))
            {
                element = reusable;
            }

            if (element == null)
            {
                element = new ProxyRoomAbility(roomIndex, attachment, roomNavigation, this);
            }

            if (!element.IsVisible)
            {
                return;
            }

            layer.Add(element);
            _roomAbilityElements[roomIndex] = element;
        }

        private void AppendHandSelectionSignature(StringBuilder sb)
        {
            global::HandSelectionUI selection = HandSelectionUI;
            sb.Append("selection:");
            if (selection == null)
            {
                sb.Append("null;");
                return;
            }

            sb.Append(selection.Active ? '1' : '0').Append(':');
            sb.Append(selection.Hidden ? '1' : '0').Append(':');
            AppendHandSelectionButtonSignature(sb, selection.GetConfirmButton());
            AppendHandSelectionButtonSignature(sb, selection.GetCancelButton());
            AppendHandSelectionButtonSignature(sb, Get<global::ShinyShoe.GameUISelectableButton>(selection, HandSelectionHideButtonField));
            sb.Append(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(selection, HandSelectionConfirmButtonLabelField))).Append(':');
            sb.Append(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(selection, HandSelectionCancelButtonLabelField))).Append(':');
            sb.Append(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(selection, HandSelectionHideButtonLabelField))).Append(':');
            TMP_Text prompt = Get<TMP_Text>(selection, HandSelectionInstructionsDescriptionLabelField);
            sb.Append(AccessibilityText.ReadLocalizedText(prompt)).Append(':');
            sb.Append(';');
        }

        private static void AppendHandSelectionButtonSignature(StringBuilder sb, global::ShinyShoe.GameUISelectableButton button)
        {
            sb.Append(IsVisible(button) ? '1' : '0').Append('[');
            sb.Append(AccessibleScreenText.ReadButtonLabel(button)).Append(']');
        }

        private static string GetFloorLayerLabel(int roomIndex, RoomState room)
        {
            Message floor = Message.Localized("combat", "FLOOR", new { floor = roomIndex + 1 });
            Message capacity = GetFloorCapacityLabel(room);
            return capacity != null
                ? Message.Join(", ", floor, capacity).Resolve()
                : floor.Resolve();
        }

        private static Message GetFloorCapacityLabel(RoomState room)
        {
            if (room == null)
            {
                return null;
            }

            CapacityInfo capacity = room.GetCapacityInfo(Team.Type.Monsters);
            int free = Math.Max(0, capacity.max - capacity.count);
            return Message.Localized("combat", "FLOOR.CAPACITY", new { free, total = capacity.max });
        }

        private static Message GetFloorCapacityEchoLabel(RoomState room)
        {
            Message capacity = GetFloorCapacityLabel(room);
            int echoes = room?.GetCurrentCorruption() ?? 0;
            if (capacity == null || room == null || !room.corruptionEnabled || echoes <= 0)
            {
                return capacity;
            }

            return Message.Localized("combat", "FLOOR.CAPACITY_ECHO", new
            {
                capacity = capacity.Resolve(),
                echoes,
                totalEchoes = room.GetMaxCorruption()
            });
        }

        private void AddFloorTarget(BattleLayer layer, int roomIndex, BattleRoomNavigation roomNavigation)
        {
            ProxyCombatFloor target = new ProxyCombatFloor(
                roomIndex,
                "FLOOR.TARGET",
                false,
                roomNavigation: roomNavigation);
            layer.AddHidden(target);
            _floorTargetElements[roomIndex] = target;
        }

        private void AddSpawnPointTargets(BattleLayer layer, RoomState room, int roomIndex)
        {
            room.GetAllSpawnInformation(out List<SpawnPoint> spawnPoints, out List<SpawnPointUI> _);
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                SpawnPoint spawnPoint = spawnPoints[i];
                if (spawnPoint == null || _spawnPointElements.ContainsKey(spawnPoint))
                {
                    continue;
                }

                ProxyCombatSpawnPoint element = new ProxyCombatSpawnPoint(spawnPoint, roomIndex, _roomNavigation);
                layer.AddHidden(element);
                _spawnPointElements[spawnPoint] = element;
            }
        }

        private void AddCreature(BattleLayer layer, CharacterState character, int roomIndex)
        {
            if (character == null)
            {
                return;
            }

            ProxyCombatCreature element = new ProxyCombatCreature(
                character,
                roomIndex,
                _roomNavigation);
            layer.Add(element);
            _creatureElements[character] = element;
            CharacterUI characterUI = character.GetCharacterUI();
            GameObject target = characterUI != null ? characterUI.gameObject : null;
            RegisterBattle(target, element);
        }

        private string BuildBattleSignature()
        {
            StringBuilder sb = new StringBuilder();
            if (IsVictoryActive)
            {
                AppendVictorySignature(sb);
                return sb.ToString();
            }

            AppendHandSignature(sb);
            AppendFloorSignature(sb);
            AppendTopPanelSignature(sb);
            return sb.ToString();
        }

        private void BuildVictoryRegistry()
        {
            global::VictoryUI victory = VictoryUI;
            ListContainer root = new ListContainer
            {
                AnnounceName = false,
                AnnouncePosition = false,
                NavigationAxis = NavigationAxis.Vertical
            };
            _victoryRoot = root;

            AddVictoryText(root, Get<TMP_Text>(victory, VictoryScoreHeaderField));
            AddVictoryText(
                root,
                Get<TMP_Text>(victory, VictoryTotalScoreLabelField),
                Get<TMP_Text>(victory, VictoryTotalScoreAmountLabelField));
            AddVictoryScoreEntries(root, Get<UnityEngine.UI.LayoutGroup>(victory, VictoryScoreEntryContainerField));

            List<RewardItemUI> rewards = ProxyRewardItem.VictoryRewards(victory);
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    AddVictoryReward(root, rewards[i]);
                }
            }

            AddVictoryButton(root, ProxyVictoryButton.Create(victory));
        }

        private void AppendVictorySignature(StringBuilder sb)
        {
            global::VictoryUI victory = VictoryUI;
            sb.Append("victory:");
            UnityEngine.UI.LayoutGroup scoreEntries = Get<UnityEngine.UI.LayoutGroup>(victory, VictoryScoreEntryContainerField);
            if (scoreEntries != null)
            {
                global::ScoreEntryUI[] entryUIs = scoreEntries.GetComponentsInChildren<global::ScoreEntryUI>(includeInactive: true);
                for (int i = 0; i < entryUIs.Length; i++)
                {
                    global::ScoreEntryUI entry = entryUIs[i];
                    if (entry == null || !entry.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    ProxyVictoryScoreEntry.AppendSignature(sb, entry);
                }
            }

            ProxyRewardItem.AppendVictorySignature(sb, victory);
            ProxyVictoryButton.AppendSignature(sb, victory);
        }

        private void AddVictoryScoreEntries(ListContainer root, UnityEngine.UI.LayoutGroup container)
        {
            if (container == null)
            {
                return;
            }

            global::ScoreEntryUI[] entries = container.GetComponentsInChildren<global::ScoreEntryUI>(includeInactive: true);
            for (int i = 0; i < entries.Length; i++)
            {
                AddVictoryScoreEntry(root, entries[i]);
            }
        }

        private void AddVictoryScoreEntry(ListContainer root, global::ScoreEntryUI entry)
        {
            if (entry == null || !entry.gameObject.activeInHierarchy)
            {
                return;
            }

            ProxyVictoryScoreEntry element = new ProxyVictoryScoreEntry(entry);
            if (!element.HasContent)
            {
                return;
            }

            root.Add(element);
            RegisterBattle(entry.gameObject, element);
            RegisterBattle(element.LabelTarget, element);
            RegisterBattle(element.ValueTarget, element);
            RegisterBattle(element.TooltipTarget, element);
        }

        private void AddVictoryText(ListContainer root, TMP_Text label, TMP_Text amount = null)
        {
            if (label == null)
            {
                return;
            }

            ProxyVictoryText element = new ProxyVictoryText(label, amount);
            root.Add(element);
            RegisterBattle(label.gameObject, element);
            if (amount != null)
            {
                RegisterBattle(amount.gameObject, element);
            }
        }

        private void AddVictoryReward(ListContainer root, RewardItemUI rewardUI)
        {
            if (rewardUI == null || !rewardUI.gameObject.activeInHierarchy || rewardUI.rewardState?.RewardData == null)
            {
                return;
            }

            ProxyRewardItem element = new ProxyRewardItem(rewardUI);
            root.Add(element);
            RegisterBattle(element, rewardUI.gameObject, element.TitleTarget, element.QuantityTarget);
        }

        private void AddVictoryButton(ListContainer root, ProxyVictoryButton element)
        {
            global::ShinyShoe.GameUISelectableButton button = element?.Button;
            if (button == null)
            {
                return;
            }

            root.Add(element);
            RegisterBattle(button.gameObject, element);
        }

        private void AppendTopPanelSignature(StringBuilder sb)
        {
            global::BattleHud hud = BattleHud;
            if (hud == null)
            {
                return;
            }

            sb.Append(";top:");
            AppendAbilityCounterSignature(sb, Get<global::AbilityCounterUI>(hud, AbilityCounterUIField));
            AppendForgePointsSignature(sb);
            AppendMoonPhaseSignature(sb);
            AppendTurnCounterSignature(sb);
            HashSet<string> bossTargetsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AppendBossTargetSignature(sb, Get<List<global::BossTargetUI>>(Hud, HudBossTargetUisField), bossTargetsSeen);
            AppendBossTargetSignature(sb, Get<List<global::BossTargetUI>>(Hud, HudSoulSaviorBossTargetUisField), bossTargetsSeen);
        }

        private void AddTopPanelLayer(Container root)
        {
            BattleLayer layer = AddLayer(root, Message.Localized("combat", "TOP_PANEL").Resolve(), announcePosition: false);
            global::BattleHud hud = BattleHud;
            if (hud == null)
            {
                return;
            }

            AddEnergy(layer, Get<global::EnergyUI>(hud, EnergyUIField));
            AddForgePoints(layer);
            AddPile(layer, ProxyCardPile.Draw(hud));
            AddPile(layer, ProxyCardPile.Discard(hud));
            AddPile(layer, ProxyCardPile.Consume(hud));
            AddPile(layer, ProxyCardPile.Eaten(hud));
            AddAbilityCounter(layer, Get<global::AbilityCounterUI>(hud, AbilityCounterUIField));
            AddMoonPhase(layer);
            AddTurnCounter(layer);
            AddBossTargets(layer);
            AddHudButton(layer, hud.GetUndoTurnButton(), "BUTTON.UNDO_TURN");
            AddHudButton(layer, hud.GetEndTurnButton(), "BUTTON.END_TURN");
        }

        private void AddEnergy(BattleLayer layer, global::EnergyUI energyUI)
        {
            if (energyUI == null)
            {
                return;
            }

            IGameUIComponent selectable = Get<IGameUIComponent>(energyUI, EnergySelectableField);
            if (selectable == null)
            {
                return;
            }

            ProxyEnergyUI element = new ProxyEnergyUI(energyUI, selectable);
            layer.Add(element);
            RegisterBattle(energyUI.gameObject, element);
            RegisterBattle(selectable.component != null ? selectable.component.gameObject : null, element);
        }

        private void AddForgePoints(BattleLayer layer)
        {
            global::ForgePointsUI forge = Get<global::ForgePointsUI>(Hud, HudForgePointsUIField);
            if (forge == null || !forge.gameObject.activeInHierarchy)
            {
                return;
            }

            GameUISelectableButton button = forge.GetForgingToggleButton();
            if (button == null)
            {
                return;
            }

            ProxyForgePoints element = new ProxyForgePoints(forge, button, () => TriggerHud(button));
            layer.Add(element);
            RegisterBattle(forge.gameObject, element);
            RegisterBattle(button.gameObject, element);
        }

        private void AddPile(BattleLayer layer, ProxyCardPile element)
        {
            global::CardPileCountUI pile = element?.Pile;
            if (pile == null || pile.Button == null)
            {
                return;
            }

            layer.Add(element);
            RegisterBattle(pile.gameObject, element);
            RegisterBattle(pile.Button.gameObject, element);
        }

        private void AddAbilityCounter(BattleLayer layer, global::AbilityCounterUI abilityCounter)
        {
            if (abilityCounter == null || abilityCounter.Button == null)
            {
                return;
            }

            ProxyAbilityCounter element = new ProxyAbilityCounter(
                abilityCounter,
                this);
            layer.Add(element);
            RegisterBattle(abilityCounter.gameObject, element);
            RegisterBattle(abilityCounter.Button.gameObject, element);
        }

        private void AddHudButton(BattleLayer layer, global::ShinyShoe.GameUISelectableButton button, string labelKey)
        {
            if (button == null)
            {
                return;
            }

            ProxyHudButton element = new ProxyHudButton(button, "combat", labelKey);
            layer.Add(element);
            RegisterBattle(button.gameObject, element);
        }

        private void AddMoonPhase(BattleLayer layer)
        {
            global::LunaCovenUI luna = Get<global::LunaCovenUI>(Hud, HudLunaCovenUIField);
            if (luna == null)
            {
                return;
            }

            ProxyLunaCoven element = new ProxyLunaCoven(luna);
            layer.Add(element);
            RegisterBattle(luna.gameObject, element);
        }

        private void AddTurnCounter(BattleLayer layer)
        {
            global::BattleTurnCounter counter = Get<global::BattleTurnCounter>(Hud, HudBattleTurnCounterField);
            if (counter == null)
            {
                return;
            }

            ProxyBattleTurnCounter element = new ProxyBattleTurnCounter(counter);
            layer.Add(element);
            RegisterBattle(counter.gameObject, element);
        }

        private void AddBossTargets(BattleLayer layer)
        {
            HashSet<string> seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddBossTargets(layer, Get<List<global::BossTargetUI>>(Hud, HudBossTargetUisField), seenTitles);
            AddBossTargets(layer, Get<List<global::BossTargetUI>>(Hud, HudSoulSaviorBossTargetUisField), seenTitles);
        }

        private void AddBossTargets(BattleLayer layer, List<global::BossTargetUI> targets, HashSet<string> seenTitles)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                global::BossTargetUI target = targets[i];
                if (target == null || !target.gameObject.activeInHierarchy || !ProxyBossTarget.IsCurrentBossTarget(target))
                {
                    continue;
                }

                string title = ProxyBossTarget.Title(target);
                if (!string.IsNullOrWhiteSpace(title) && seenTitles != null && !seenTitles.Add(title))
                {
                    continue;
                }

                ProxyBossTarget element = new ProxyBossTarget(target, currentOnly: true);
                layer.Add(element);
                RegisterBattle(target.gameObject, element);
            }
        }

        private static void AppendAbilityCounterSignature(StringBuilder sb, global::AbilityCounterUI abilityCounter)
        {
            sb.Append("ability:");
            if (abilityCounter == null)
            {
                sb.Append("null;");
                return;
            }

            sb.Append(abilityCounter.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            sb.Append(AccessibilityText.ReadLocalizedText(Get<TMP_Text>(abilityCounter, AbilityCounterCountLabelField))).Append(';');
        }

        private void AppendForgePointsSignature(StringBuilder sb)
        {
            global::ForgePointsUI forge = Get<global::ForgePointsUI>(Hud, HudForgePointsUIField);
            sb.Append("forge:");
            if (forge == null)
            {
                sb.Append("null;");
                return;
            }

            sb.Append(forge.gameObject.activeInHierarchy ? '1' : '0').Append(';');
        }

        private void AppendMoonPhaseSignature(StringBuilder sb)
        {
            global::LunaCovenUI luna = Get<global::LunaCovenUI>(Hud, HudLunaCovenUIField);
            sb.Append("moon:");
            if (luna == null)
            {
                sb.Append("null;");
                return;
            }

            sb.Append(luna.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            sb.Append(ProxyLunaCoven.SignatureText(luna)).Append(';');
        }

        private void AppendTurnCounterSignature(StringBuilder sb)
        {
            global::BattleTurnCounter counter = Get<global::BattleTurnCounter>(Hud, HudBattleTurnCounterField);
            sb.Append("turn:");
            if (counter == null)
            {
                sb.Append("null;");
                return;
            }

            sb.Append(counter.gameObject.activeInHierarchy ? '1' : '0').Append(':');
            sb.Append(ProxyBattleTurnCounter.SignatureText(counter)).Append(':');
            sb.Append(ProxyBattleTurnCounter.IsTooltipEnabled(counter) ? '1' : '0').Append(';');
        }

        private static void AppendBossTargetSignature(StringBuilder sb, List<global::BossTargetUI> targets, HashSet<string> seenTitles)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                global::BossTargetUI target = targets[i];
                if (target == null || !target.gameObject.activeInHierarchy || !ProxyBossTarget.IsCurrentBossTarget(target))
                {
                    continue;
                }

                string title = ProxyBossTarget.Title(target);
                if (!string.IsNullOrWhiteSpace(title) && seenTitles != null && !seenTitles.Add(title))
                {
                    continue;
                }

                sb.Append("boss:");
                sb.Append(title).Append(';');
            }
        }

        private CharacterState FindNearestAbilityCharacter()
        {
            MonsterManager monsterManager = MonsterManager;
            RoomManager roomManager = RoomManager;
            if (monsterManager == null || roomManager == null)
            {
                return null;
            }

            List<CharacterState> characters = new List<CharacterState>();
            monsterManager.CollectCharactersWithAvailableAbility(characters, includePreviewCharacters: false, includePyres: false);
            int currentRoom = roomManager.CurrentSelectedRoom;
            CharacterState nearest = null;
            int nearestRoom = -1;
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterState character = characters[i];
                if (character == null)
                {
                    continue;
                }

                int roomIndex = character.GetCurrentRoomIndex();
                if (roomIndex < 0)
                {
                    continue;
                }

                if (nearestRoom < 0 || Mathf.Abs(roomIndex - currentRoom) < Mathf.Abs(nearestRoom - currentRoom))
                {
                    nearest = character;
                    nearestRoom = roomIndex;
                }
            }

            return nearest;
        }

        private int CountAvailableRoomAbilities()
        {
            if (CombatManager?.IsPlacementPhase == true)
            {
                return 0;
            }

            RoomManager roomManager = RoomManager;
            if (roomManager == null)
            {
                return 0;
            }

            int count = 0;
            int roomCount = roomManager.GetNumRooms();
            for (int i = 0; i < roomCount; i++)
            {
                TrainRoomAttachmentState attachment = FindRoomAbilityAttachment(roomManager.GetRoom(i));
                if (attachment != null && attachment.CurrentAbilityCooldown <= 0)
                {
                    count++;
                }
            }

            return count;
        }

        private TrainRoomAttachmentState FindNearestAvailableRoomAbility(out int roomIndex)
        {
            roomIndex = -1;
            if (CombatManager?.IsPlacementPhase == true)
            {
                return null;
            }

            RoomManager roomManager = RoomManager;
            if (roomManager == null)
            {
                return null;
            }

            int currentRoom = roomManager.CurrentSelectedRoom;
            TrainRoomAttachmentState nearest = null;
            int roomCount = roomManager.GetNumRooms();
            for (int i = 0; i < roomCount; i++)
            {
                TrainRoomAttachmentState attachment = FindRoomAbilityAttachment(roomManager.GetRoom(i));
                if (attachment == null || attachment.CurrentAbilityCooldown > 0)
                {
                    continue;
                }

                if (nearest == null || Mathf.Abs(i - currentRoom) < Mathf.Abs(roomIndex - currentRoom))
                {
                    nearest = attachment;
                    roomIndex = i;
                }
            }

            return nearest;
        }

        private static SpawnPointUI GetSpawnPointUI(CharacterState character)
        {
            SpawnPoint spawnPoint = character?.GetSpawnPoint();
            RoomState room = character?.GetCurrentRoom();
            if (spawnPoint == null || room == null)
            {
                return null;
            }

            return room.GetSpawnPointUIFromSpawnPoint(spawnPoint);
        }

        private global::CommonSelectionBehavior GetActiveTargetingBehavior()
        {
            if (SelectionBehaviour?.IsCardSelected() == true)
            {
                return SelectionBehaviour;
            }

            if (UnitAbilitySelectionBehavior?.IsCardSelected() == true)
            {
                return UnitAbilitySelectionBehavior;
            }

            if (HandUI?.GetRoomAbilitySelectionBehavior()?.IsCardSelected() == true)
            {
                return HandUI.GetRoomAbilitySelectionBehavior();
            }

            return null;
        }

        internal bool ParkSelectionOnTower()
        {
            global::CommonSelectionBehavior selectionBehavior = SelectionBehaviour;
            if (selectionBehavior?.TowerSelectable == null || global::InputManager.Inst == null)
            {
                return false;
            }

            bool selected = global::InputManager.Inst.SelectGameUIComponent(selectionBehavior.TowerSelectable, allowClearingSelection: false);
            return selected;
        }

        private static string DescribeElement(UIElement element)
        {
            return element != null ? element.GetType().Name : "null";
        }

        private static string DescribeGameObject(GameObject go)
        {
            return go != null ? go.name : "null";
        }

        private static string DescribeNativeSelection()
        {
            IGameUIComponent selected = global::InputManager.Inst != null
                ? global::InputManager.Inst.GetSelectedGameUIComponent()
                : null;
            GameObject go = selected?.component != null ? selected.component.gameObject : null;
            string componentName = selected != null ? selected.GetType().Name : "null";
            return componentName + ":" + DescribeGameObject(go);
        }

        private static int CompareCharactersForNavigation(CharacterState left, CharacterState right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }
            if (left == null)
            {
                return 1;
            }
            if (right == null)
            {
                return -1;
            }

            CharacterUI leftUI = left.GetCharacterUI();
            CharacterUI rightUI = right.GetCharacterUI();
            if (leftUI != null && rightUI != null)
            {
                int byX = leftUI.transform.position.x.CompareTo(rightUI.transform.position.x);
                if (byX != 0)
                {
                    return byX;
                }
            }

            return left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        private static TrainRoomAttachmentState FindRoomAbilityAttachment(RoomState room)
        {
            if (room == null)
            {
                return null;
            }

            List<TrainRoomAttachmentState> attachments = new List<TrainRoomAttachmentState>();
            room.TryGetTrainRoomAttachments(room.Attachments, isHidden: false, attachments);
            for (int i = 0; i < attachments.Count; i++)
            {
                TrainRoomAttachmentState attachment = attachments[i];
                if (attachment != null && attachment.HasRoomAbility)
                {
                    return attachment;
                }
            }

            return null;
        }

        private static string FormatNumber(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}

