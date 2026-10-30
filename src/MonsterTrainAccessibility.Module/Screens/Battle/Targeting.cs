using System;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;

namespace MonsterTrainAccessibility.Module.Screens
{
    // Targeting: while the game holds a card (or an ability) selected, the screen is the selected
    // floor's targets as the game lists them for its keyboard play, one row under the floor's name:
    // a unit by its readout, a place for a new unit as "Spawn point 2" (and the unit standing there,
    // read as a unit is anywhere: "Spawn point 1, Shield Steward, Steward, size 3, ..."), a card
    // that takes the whole floor as "Selected floor". Landing on a target moves the game's own keyboard
    // target there; Up and Down select the floor above or below and land on the game's pick there;
    // Enter plays through the game's own submit; Escape drops the card as the game's cancel does.
    public sealed partial class BattleScreen
    {
        private void BuildTargeting(GraphBuilder b, CommonSelectionBehavior sel)
        {
            int room = SelectedFloor;
            var items = new List<KeyValuePair<ControlId, NodeVtable>>();
            var targets = sel.possibleTargets;
            int start = 0;
            if (sel.activeValidator is RoomTargetValidator || targets.Count == 0)
                items.Add(Item("target:" + room + ":floor", Target(sel, -1, () => Strings.Battle("selected_floor"), null)));
            else
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    var unit = target.GetParamCharacterState();
                    var point = target.GetParamSpawnPointUI();
                    NodeVtable vt;
                    string id;
                    if (point != null && (sel.activeValidator.IsSpawnerCard() || sel.activeValidator.IsTeleportCard()))
                    {
                        var spawn = point.GetParentSpawnPoint();
                        id = "target:" + room + ":point:" + (spawn != null ? spawn.GetIndexInRoom() : i);
                        vt = SpawnTarget(sel, i, spawn);
                    }
                    else if (unit != null)
                    {
                        id = "target:" + room + ":unit:" + unit.GetInstanceID();
                        vt = Target(sel, i, null, unit);
                    }
                    else continue;
                    if (i == sel.keyboardSelectedTargetIndex) start = items.Count;
                    items.Add(new KeyValuePair<ControlId, NodeVtable>(ControlId.Structural(id), vt));
                }
                if (items.Count == 0)
                    items.Add(Item("target:" + room + ":floor", Target(sel, -1, () => Strings.Battle("selected_floor"), null)));
            }
            Row(b, "target", FloorLabel(room), items);
            b.SetStart(items[Math.Min(start, items.Count - 1)].Key);
        }

        // One target: landing moves the game's keyboard target to it; Enter plays there.
        private static NodeVtable Target(CommonSelectionBehavior sel, int index, Func<string> label, CharacterState unit)
        {
            var vt = unit != null ? UnitNodes.Unit(unit, null) : GameNodes.Text(label);
            vt.OnFocus = () =>
            {
                if (index < 0 || !sel.IsCardSelected() || index == sel.keyboardSelectedTargetIndex) return;
                sel.MoveTargetWithKeyboard(index - sel.keyboardSelectedTargetIndex);
            };
            vt.OnActivate = () => Submit(sel);
            return vt;
        }

        // A place for a new unit. By its neighbours (the preference, on by default), the names of the units
        // on either side of it: "Before Shield Steward" at the front, "After Fel" at the back, "Between
        // Shield Steward, Fel" between two, "No friendly units" on a floor with none. Else "Spawn point 2"
        // (1 at the front); where a unit stands, whom the new one goes in front of, the place and then the
        // unit as it reads anywhere (its buffer the unit's).
        private static NodeVtable SpawnTarget(CommonSelectionBehavior sel, int index, SpawnPoint spawn)
        {
            Func<string> place = () => spawn != null ? Strings.Battle("spawn_point", spawn.GetIndexInRoom() + 1) : null;
            if (Preferences.PlacesByNeighbours && spawn != null)
                return Target(sel, index, () => Between(spawn) ?? Strings.Battle("place_alone"), null);
            var occupant = spawn != null ? spawn.GetCharacterState() : null;
            if (occupant == null || occupant.IsDestroyed) return Target(sel, index, place, null);
            var vt = Target(sel, index, null, occupant);
            vt.Announcements = new List<NodeAnnouncement>(vt.Announcements);
            ((List<NodeAnnouncement>)vt.Announcements).Insert(0, GameNodes.LabelPart(place));
            return vt;
        }

        // The place by the units on either side of it, front first: the unit standing on it is the one a
        // new unit goes in front of, the unit on the place before it the one it goes behind. Null on a
        // floor with none of the player's units.
        private static string Between(SpawnPoint spawn)
        {
            var room = spawn.GetRoomOwner();
            int at = spawn.GetIndexInRoom();
            string ahead = at > 0 && room != null ? NameOf(room.GetMonsterPoint(at - 1)) : null;
            string behind = NameOf(spawn);
            if (ahead != null && behind != null) return Strings.Battle("place_between", ahead, behind);
            if (behind != null) return Strings.Battle("place_before", behind);
            if (ahead != null) return Strings.Battle("place_after", ahead);
            return null;
        }

        private static string NameOf(SpawnPoint point)
        {
            var unit = point != null ? point.GetCharacterState() : null;
            return unit != null && !unit.IsDestroyed ? unit.GetName() : null;
        }

        // Play on the game's keyboard target, as its submit does; the target was moved by keyboard, which
        // the game counts as the cursor having left the hand.
        private static void Submit(CommonSelectionBehavior sel)
        {
            if (!sel.IsCardSelected()) return;
            try
            {
                sel.UpdateKeyboardNavSplinePointer(force: true);
                if (sel.HasKeyboardSelectedTarget()) sel.hasMovedTargetingCursorOutOfHand = true;
                sel.HandleSubmit();
            }
            catch (Exception e) { CoreLog.Warning("battle: playing the selected card: " + e.Message); }
        }

        // Up / Down while targeting: the floor above or below, landing on the game's pick there.
        private static void ChangeFloor(int step)
        {
            var rooms = Rooms;
            if (rooms == null) return;
            int room = SelectedFloor + step;
            if (room < 0 || room >= rooms.GetNumRooms()) return;
            SelectFloor(room);
            Later.Frames(1, () => Navigation.AnnounceCurrent(interrupt: true), "battle: target floor");
        }

        // Escape while targeting: the game's own deselect.
        private static void CancelTargeting()
        {
            try { HandUi?.InputDeselect(null, null); }
            catch (Exception e) { CoreLog.Warning("battle: cancelling the selected card: " + e.Message); }
        }

        /// <summary>The game's own message when a card cannot be played (not enough ember, the floor
        /// full), spoken as it is shown.</summary>
        [HarmonyPatch(typeof(HandUI), nameof(HandUI.ShowCardSelectionErrorMessage))]
        internal static class PlayErrors
        {
            /// <summary>How many messages have been spoken, so a caller can tell whether the game said one.</summary>
            public static int Spoken;

            private static void Prefix(HandUI __instance, string errorMessage)
            {
                if (__instance.suppressErrorMessages || string.IsNullOrEmpty(errorMessage)) return;
                var save = AllGameManagers.Instance?.GetSaveManager();
                if (save != null && save.IsInUndoMode) return;
                Spoken++;
                Speech.Say(errorMessage, interrupt: true);
            }
        }
    }
}
