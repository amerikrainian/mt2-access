using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Input;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// A battle (<see cref="GameScreen"/>), laid out as the train is, top to bottom, one row each:
    /// <list type="bullet">
    /// <item>the top panel: ember, the piles, the units' abilities, the battle's line and the moon,
    /// undo and end turn, each by the game's own name for it;</item>
    /// <item>each floor from the pyre's down, named on entering ("Floor 2, 3/5, Training Ground"): its
    /// units left to right as drawn (<see cref="UnitNodes"/>), "No creatures" when it has none;</item>
    /// <item>the hand, its cards in order (<see cref="CardNodes"/>), "No cards" when empty.</item>
    /// </list>
    /// Up and Down move between rows, each row landing where it was last left; Left and Right within a
    /// row. Landing on a floor's row selects the floor in the game, as scrolling does, while the player
    /// may act. Enter on a card selects it through the game, on a floor that takes it, and targeting
    /// (<see cref="BattleTargetingScreen"/>) takes over. The game's own keys for undo, the piles, end
    /// turn and the card numbers reach the game (<see cref="IGameKeys"/>). Typeahead stands down.
    /// </summary>
    public sealed partial class BattleScreen : Screen, IGameKeys
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.battle";
        public override int Layer => 3;
        public override bool AllowsTypeahead => false;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        private static readonly global::InputManager.Controls[] PassedControls =
        {
            global::InputManager.Controls.Undo,
            global::InputManager.Controls.DrawPile,
            global::InputManager.Controls.DiscardPile,
            global::InputManager.Controls.ExhaustPile,
            global::InputManager.Controls.EatenPile,
            global::InputManager.Controls.Deck,
            global::InputManager.Controls.ContextAction,
            global::InputManager.Controls.GameSpeedToggle,
            global::InputManager.Controls.Minimap,
            global::InputManager.Controls.DragonsHoard,
            global::InputManager.Controls.ShowCardPreview,
            global::InputManager.Controls.Card01, global::InputManager.Controls.Card02,
            global::InputManager.Controls.Card03, global::InputManager.Controls.Card04,
            global::InputManager.Controls.Card05, global::InputManager.Controls.Card06,
            global::InputManager.Controls.Card07, global::InputManager.Controls.Card08,
            global::InputManager.Controls.Card09, global::InputManager.Controls.Card10,
        };

        IEnumerable<global::InputManager.Controls> IGameKeys.GameControls => PassedControls;

        // Where each row was last left, so coming back lands there (the rows are rebuilt every frame).
        private readonly Dictionary<string, ControlId> _lastInRow = new Dictionary<string, ControlId>();

        // Where a landing on each row goes this render: where it was last left, else its first item.
        private readonly Dictionary<string, ControlId> _rowLanding = new Dictionary<string, ControlId>();

        // A battle over (won: its victory panel, then the way out) is not the battle's to read. Nor one
        // the player is leaving (the Menu's Back to the Covenant Outpost or the Rift Station): the game
        // closes the Menu and loads the next screen with the battle still up and no longer interactable,
        // and the battle would be read again on the way out. A load while the battle is interactable is
        // the battle's own and leaves it be.
        public override bool IsActive()
        {
            if (Game == null || HandUi == null) return false;
            if (ShinyShoe.Loading.LoadingScreen.IsWorking() && !Game.IsInteractable()) return false;
            var combat = Managers.GetCombatManager();
            return combat == null || combat.GetCombatPhase() != CombatManager.Phase.EndOfCombat;
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            var rooms = Rooms;
            if (screen == null || rooms == null || Dealing) return;
            var selection = Selection;
            if (selection == null || !selection.Active) _selectionPrompted = false;
            else if (!selection.Hidden)
            {
                BuildHandSelection(b, selection);
                return;
            }
            var targeting = Targeting;
            if (targeting != null)
            {
                _targeted = true;
                // A card's targeting (not an ability's): where it sat in the hand, to come back to.
                _targetedCard = targeting == HandUi.selectionBehaviour ? targeting.GetFocusedOrSelectedCardIndex() : -1;
                BuildTargeting(b, targeting);
                return;
            }
            // Targeting over: a card played or dropped lands back in the hand, where the card was (the
            // card itself if dropped, its neighbour if played); an ability's, on the floor it was used on.
            if (_targeted)
            {
                if (_targetedCard >= 0) _landHand = _targetedCard;
                else _landFloor = SelectedFloor;
            }
            _targeted = false;
            TopPanel(b, screen.battleHud);
            ControlId land = null;
            for (int room = rooms.GetNumRooms() - 1; room >= 0; room--)
            {
                var first = Floor(b, room);
                if (room == _landFloor) land = first;
            }
            var card = Hand(b, _landHand);
            if (_landHand >= 0) land = card;
            if (land != null) b.SetStart(land);
        }

        private int _targetedCard = -1;
        private int _landHand = -1;

        // A turn opening (the battle setting up, its pre-combat, the draw) until the player has control
        // and the dealt cards have settled: nothing is declared, so the first landing is on the hand
        // as dealt, not on an empty hand or a card face half redrawn.
        // Once the screen is back and focus restored (a few frames), read the focus if it is where the
        // turn's opening found it.
        private static void AnnounceIfStill(ControlId before, int tries)
        {
            Later.Frames(1, () =>
            {
                var now = Navigation.FocusedNode?.Id;
                if (now == null && tries > 0) AnnounceIfStill(before, tries - 1);
                else if (Equals(now, before)) Navigation.AnnounceCurrent();
            }, "battle: turn start");
        }


        private static bool s_opening;
        private static ControlId s_focusAtOpening;

        /// <summary>A turn opening (its pre-combat, the battle's start), told by the game's phase signal:
        /// the screen may not rebuild during so short a phase.</summary>
        internal static void TurnOpening()
        {
            if (!s_opening) s_focusAtOpening = Navigation.FocusedNode?.Id;
            s_opening = true;
        }

        private static bool Dealing
        {
            get
            {
                var combat = Managers != null ? Managers.GetCombatManager() : null;
                if (combat == null || Combat.CombatEvents.Reloading) return true;
                // The game asking for a card from the dealt hand (Judge's Scales): the hand is ready.
                var selection = Selection;
                if (selection != null && selection.Active)
                {
                    s_opening = false;
                    return false;
                }
                switch (combat.GetCombatPhase())
                {
                    case CombatManager.Phase.Start:
                    case CombatManager.Phase.PreCombat:
                    case CombatManager.Phase.BossActionPreCombat:
                        TurnOpening();
                        return true;
                }
                if (s_opening)
                {
                    var hand = HandUi;
                    var cards = Managers.GetCardManager();
                    bool settled = Ready && hand != null && !hand.AreAnyCardsAnimating() && cards != null && !cards.isAddingCardsToHand;
                    // Control can come back elsewhere than the player's turn (a battle won): not dealing.
                    if (settled || combat.GetCombatPhase() != CombatManager.Phase.MonsterTurn)
                    {
                        s_opening = false;
                        // Control back where focus was left (End Turn, a floor): said again, as the turn
                        // starts; a focus that moved to a card dealt is read by the landing itself.
                        if (s_focusAtOpening != null) AnnounceIfStill(s_focusAtOpening, 10);
                    }
                }
                return s_opening;
            }
        }

        private bool _targeted;
        private int _landFloor = -1;

        // ---- rows ----

        // A row of items under a context, landing where it was last left.
        private void Row(GraphBuilder b, string key, string label, List<KeyValuePair<ControlId, NodeVtable>> items, Action enter = null, bool positions = true)
        {
            if (items.Count == 0) return;
            var ids = new List<ControlId>();
            foreach (var item in items) ids.Add(item.Key);
            _rowLanding[key] = _lastInRow.TryGetValue(key, out var left) && ids.Contains(left) ? left : ids[0];
            b.PushContext(label, null, positions);
            // Each row its own line: Home and End go to its first and last item.
            b.StartRow(null, () => _lastInRow.TryGetValue(key, out var last) ? ids.IndexOf(last) : 0, edges: true);
            foreach (var item in items)
            {
                var id = item.Key;
                var vt = item.Value;
                var focus = vt.OnFocus;
                vt.OnFocus = () =>
                {
                    _landFloor = -1;
                    _landHand = -1;
                    _lastInRow[key] = id;
                    enter?.Invoke();
                    focus?.Invoke();
                };
                b.AddItem(id, vt);
            }
            b.EndRow();
            b.PopContext();
        }

        private static KeyValuePair<ControlId, NodeVtable> Item(string id, NodeVtable vt)
            => new KeyValuePair<ControlId, NodeVtable>(ControlId.Structural(id), vt);

        // The floor's row; returns the item a landing on it goes to.
        private ControlId Floor(GraphBuilder b, int room)
        {
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            // The floor's ability first (a room upgrade's), then its units.
            var state = Rooms.GetRoom(room);
            if (state != null)
            {
                var attachments = new List<TrainRoomAttachmentState>();
                state.TryGetTrainRoomAttachments(state.Attachments, isHidden: false, attachments);
                foreach (var attachment in attachments)
                {
                    var ability = RoomAbility(attachment);
                    if (ability != null) items.Add(Item("floor:" + room + ":ability:" + attachment.Guid, RoomAbilityNode(room, attachment, ability)));
                }
            }
            foreach (var unit in Units(room))
            {
                var u = unit;
                var vt = UnitNodes.Unit(u, null);
                if (u.HasUnitAbility()) vt.OnActivate = () => UseAbility(u);
                items.Add(Item("unit:" + u.GetInstanceID(), vt));
            }
            if (items.Count == 0) items.Add(Item("floor:" + room + ":empty", GameNodes.Text(() => Strings.Battle("floor_empty"))));
            Row(b, "floor:" + room, FloorLabel(room), items, () =>
            {
                _focusFloor = room;
                if (Ready) SelectFloor(room);
            });
            var key = "floor:" + room;
            if (_lastInRow.TryGetValue(key, out var last) && items.Exists(i => Equals(i.Key, last))) return last;
            return items[0].Key;
        }

        // The hand's row; returns the card at `at` (the last when the hand is shorter now), or the row's
        // only item when it is empty.
        private ControlId Hand(GraphBuilder b, int at = -1)
        {
            var hand = HandUi;
            var cards = Managers.GetCardManager();
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            var uis = hand.GetCardUIList();
            int count = cards != null ? cards.GetNumCardsInHand() : 0;
            for (int i = 0; i < count && i < uis.Count; i++)
            {
                var ui = uis[i];
                var state = cards.GetHandCard(i);
                // A card being dealt is declared once its face is drawn.
                if (ui == null || state == null || !CardNodes.Drawn(ui)) continue;
                int index = i;
                var vt = CardNodes.Card(ui, CardTooltipContainer.LoreTooltipSetting.Preferences, () => FocusCard(index));
                vt.OnActivate = () => PlayCard(index);
                if (Selection != null && Selection.Active) vt.Announcements = new List<NodeAnnouncement>(vt.Announcements) { GameNodes.SelectedPart(() => ChosenInSelection(ui)) };
                items.Add(Item(CardKey(state), vt));
            }
            if (items.Count == 0) items.Add(Item("hand:empty", GameNodes.Text(() => Strings.Battle("hand_empty"))));
            Row(b, "hand", Strings.Battle("hand"), items, () => _focusFloor = -1);
            b.SetStart(items[0].Key);
            return items[System.Math.Max(0, System.Math.Min(at, items.Count - 1))].Key;
        }

        // ---- the hand ----

        // A card in hand by the card itself: copies of one card share their id.
        private static string CardKey(CardState card) => "card:" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(card);

        // The card shown raised in the game, as the keyboard's focus shows it, while the player may act.
        private static void FocusCard(int index)
        {
            var sel = HandUi?.selectionBehaviour;
            if (sel == null || !(Ready || Selecting) || sel.IsCardSelected()) return;
            try { sel.FocusCard(index, snapFocus: true, refresh: true); }
            catch (Exception e) { CoreLog.Warning("battle: focusing card " + index + ": " + e.Message); }
        }

        // Enter on a card: select it through the game, on the selected floor if it takes the card, else
        // on the nearest floor that does (a full floor cannot take a unit). The game says why it cannot.
        private static void PlayCard(int index)
        {
            var hand = HandUi;
            var sel = hand?.selectionBehaviour;
            var cards = Managers.GetCardManager();
            if (sel == null || cards == null) return;
            // The game asking for a card from the hand: choosing it is the game's own choosing.
            if (Selecting)
            {
                if (sel.SelectCard(index, displayError: true, usingKeyboard: true)) LandOnConfirm();
                return;
            }
            if (!Ready)
            {
                Speech.Say(Strings.StateDisabled, interrupt: true);
                return;
            }
            var card = cards.GetHandCard(index);
            int floor = PlayableFloor(card);
            if (floor >= 0 && floor != SelectedFloor) SelectFloor(floor);
            hand.unitAbilitySelectionBehavior?.UnFocusCard(true);
            hand.roomAbilitySelectionBehavior?.UnFocusCard(true);
            if (hand.IsInViewCharactersMode()) hand.RequestExitViewCharactersMode();
            // A frame for the floor change to land; the game shows (and the mod speaks) why a card
            // cannot be selected.
            Later.Frames(1, () =>
            {
                if (!sel.IsFocusedOrSelectedCard(index)) sel.FocusCard(index, snapFocus: true, refresh: true);
                // The game names only some refusals; the rest it leaves to the card's look: said here.
                sel.CanSelectCard(out var error);
                int spoken = PlayErrors.Spoken;
                if (!sel.SelectCard(index, displayError: true, usingKeyboard: true) && PlayErrors.Spoken == spoken)
                {
                    string message = CommonSelectionBehavior.GetErrorMessage(error);
                    Speech.Say(string.IsNullOrWhiteSpace(message) ? Strings.StateDisabled : message, interrupt: true);
                }
            }, "battle: select card");
        }

        // The floor to play a card to: the selected one when it takes the card, else the nearest that
        // does, alternating up and down; -1 when none does (the game then says why).
        private static int PlayableFloor(CardState card)
        {
            var rooms = Rooms;
            var cards = Managers.GetCardManager();
            if (rooms == null || cards == null || card == null) return -1;
            int selected = rooms.GetSelectedRoom();
            int n = rooms.GetNumRooms();
            for (int d = 0; d < n; d++)
                foreach (int room in d == 0 ? new[] { selected } : new[] { selected + d, selected - d })
                {
                    if (room < 0 || room >= n) continue;
                    var state = rooms.GetRoom(room);
                    if (state == null || !state.IsRoomEnabled()) continue;
                    if (cards.CanPlayHandCard(card, room, null, null, null, out _)) return room;
                }
            return -1;
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            // Escape: while targeting, the game's cancel; otherwise the game's own Escape (the Menu).
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ =>
            {
                if (Targeting != null) CancelTargeting();
                else GameInput.Send(global::InputManager.Controls.Escape);
            });
            // Up and Down while targeting: the floor above, the floor below.
            yield return new ElementAction(UiActions.Up, Strings.Get("bind.ui.up"), _ => { if (Targeting != null) ChangeFloor(1); });
            yield return new ElementAction(UiActions.Down, Strings.Get("bind.ui.down"), _ => { if (Targeting != null) ChangeFloor(-1); });
        }

        // ---- the top panel ----

        private void TopPanel(GraphBuilder b, BattleHud hud)
        {
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            if (hud != null)
            {
                Counter(items, "ember", hud.energyUI, hud.energyUI != null ? hud.energyUI.count : null, null, hud.energyIncreaseUI);
                Pile(items, "draw", hud.deckUI, hud.cardIncreaseUI);
                Pile(items, "discard", hud.discardUI, null);
                Pile(items, "consume", hud.exhaustedUI, null);
                Pile(items, "eaten", hud.eatenUI, null);
                var abilities = hud.abilityCounterUI;
                if (Shown(abilities)) Counter(items, "abilities", abilities, abilities.countLabel, abilities.Button, null);
            }
            var top = GameScreens.Active<global::Hud>(global::ScreenName.Hud);
            if (top != null)
            {
                Turn(items, top.battleTurnCounter);
                Line(items, "moon", top.lunaCovenUI);
            }
            // A hand prompt put aside to look at the train: the game's button to bring it back.
            var prompt = Selection;
            if (prompt != null && prompt.Active && prompt.Hidden && Shown(prompt.hideButton))
                items.Add(Item("top:selection", GameWidgets.Button(prompt.hideButton, () => Shown(prompt.hideButtonLabel))));
            // An elixir being mixed, put aside to look at the train: the game's button to bring it back.
            var elixir = GameScreens.Active<ElixirDraftScreen>(global::ScreenName.Elixir);
            if (elixir != null && !DraftScreen.ElixirShown(elixir) && Shown(elixir.showButton))
                items.Add(Item("top:elixir", GameWidgets.Button(elixir.showButton, () => PauseMenuScreen.Caption(elixir.showButton) ?? TooltipReader.FirstTitle(elixir.showButton))));
            if (hud != null)
            {
                var undo = hud.undoTurnButton;
                if (Shown(undo))
                {
                    Func<string> label = () => TooltipReader.FirstTitle(undo);
                    items.Add(Item("top:undo", new NodeVtable
                    {
                        ControlType = ControlTypes.Button,
                        Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label), GameNodes.DisabledPart(() => CanUndo(hud)) },
                        SearchText = label,
                        OnActivate = () => Undo(hud),
                        Details = () => GameNodes.Lines(TooltipReader.Lines(undo)),
                    }));
                }
                var end = hud.endTurnButton;
                if (Shown(end))
                {
                    var button = end.GetComponent<GameUISelectableButton>();
                    var vt = GameWidgets.Button(button, () => TooltipReader.FirstTitle(end));
                    vt.Details = () => GameNodes.Lines(TooltipReader.Lines(end));
                    items.Add(Item("top:end", vt));
                }
            }
            Row(b, "top", Strings.Battle("top_panel"), items, () => _focusFloor = -1, positions: false);
        }

        // "Ember, 7": the game's name for a counter (its tooltip's title) and the number it draws; its
        // tooltips in the buffer, with the "next turn" note beside it when the game shows one.
        private static void Counter(List<KeyValuePair<ControlId, NodeVtable>> items, string key, Component widget, TMP_Text number, GameUISelectableButton press, Component next)
        {
            if (!Shown(widget)) return;
            Func<string> label = () => TooltipReader.FirstTitle(widget);
            var value = new NodeAnnouncement(() => number != null ? number.Content()?.Trim() : null, kind: AnnouncementKinds.Value);
            NodeVtable vt = press != null
                ? GameWidgets.Button(press, label, null, value)
                : new NodeVtable
                {
                    ControlType = ControlTypes.Text,
                    Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label), value },
                    SearchText = label,
                };
            vt.Details = () =>
            {
                var lines = TooltipReader.Lines(widget);
                if (Shown(next)) lines.AddRange(TooltipReader.Lines(next));
                return GameNodes.Lines(lines);
            };
            items.Add(Item("top:" + key, vt));
        }

        private static void Pile(List<KeyValuePair<ControlId, NodeVtable>> items, string key, CardPileCountUI pile, Component next)
        {
            if (!Shown(pile)) return;
            Counter(items, key, pile, pile.label, pile.Button, next);
        }

        // "Deployment Phase", "Turn 3 / 8": the battle's turn as the game heads it; its tooltip in the buffer.
        private static void Turn(List<KeyValuePair<ControlId, NodeVtable>> items, BattleTurnCounter counter)
        {
            if (!Shown(counter)) return;
            Func<string> text = () =>
            {
                var parts = new List<string>();
                foreach (var label in new[] { counter.textLabel, counter.countLabel })
                {
                    string content = label != null && label.gameObject.activeInHierarchy ? label.Content()?.Trim() : null;
                    if (!string.IsNullOrEmpty(content)) parts.Add(content);
                }
                return parts.Count > 0 ? string.Join(" ", parts) : null;
            };
            if (text() == null) return;
            var vt = GameNodes.Text(text);
            vt.Details = () => GameNodes.Lines(TooltipReader.Lines(counter));
            items.Add(Item("top:turn", vt));
        }

        private static void Line(List<KeyValuePair<ControlId, NodeVtable>> items, string key, Component widget)
        {
            if (Shown(widget) && TooltipReader.Lines(widget).Count > 0) items.Add(Item("top:" + key, TooltipReader.Item(widget)));
        }

        // Undo, as the game's held undo key ends: the game's own undo, when it offers one.
        private static void Undo(BattleHud hud)
        {
            if (!CanUndo(hud))
            {
                Speech.Say(Strings.StateDisabled, interrupt: true);
                return;
            }
            hud.soundManager?.PlaySfx("Undo_Complete");
            hud.UndoTurn();
        }

        private static bool CanUndo(BattleHud hud)
        {
            var save = Managers.GetSaveManager();
            var undo = hud.undoTurnButton;
            return save != null && !save.IsUndoDisabledByMutator() && undo != null && undo.Active && undo.HasUndos();
        }

    }
}
