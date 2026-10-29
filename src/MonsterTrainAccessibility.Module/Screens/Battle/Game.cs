using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Module.Interop;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Screens
{
    // The live battle, read from the game's own managers on every call: its screen, the hand, the
    // floors and who stands on them, whether the player may act, and the floor the game has selected
    // (the one cards are played to and the camera shows).
    public sealed partial class BattleScreen
    {
        private static AllGameManagers Managers => AllGameManagers.Instance;

        /// <summary>The game's battle screen while a battle is up.</summary>
        private static GameScreen Game => GameScreens.Active<GameScreen>(global::ScreenName.Game);

        private static HandUI HandUi
        {
            get
            {
                var screen = Game;
                return screen != null ? screen.handUI : null;
            }
        }

        private static RoomManager Rooms => Managers != null ? Managers.GetRoomManager() : null;

        /// <summary>The player may act: the game takes input, it is the player's turn and nothing is
        /// resolving.</summary>
        private static bool Ready
        {
            get
            {
                var screen = Game;
                var combat = Managers != null ? Managers.GetCombatManager() : null;
                return screen != null && combat != null && screen.AllowUINavigation()
                    && combat.ShouldShowEndTurnButton() && combat.AllowExternalInput;
            }
        }

        /// <summary>The selection behaviour (card, unit ability, room ability) with something selected,
        /// that is, targeting; null when nothing is.</summary>
        private static CommonSelectionBehavior Targeting
        {
            get
            {
                var hand = HandUi;
                if (hand == null) return null;
                if (hand.selectionBehaviour != null && hand.selectionBehaviour.IsCardSelected()) return hand.selectionBehaviour;
                if (hand.unitAbilitySelectionBehavior != null && hand.unitAbilitySelectionBehavior.IsCardSelected()) return hand.unitAbilitySelectionBehavior;
                if (hand.roomAbilitySelectionBehavior != null && hand.roomAbilitySelectionBehavior.IsCardSelected()) return hand.roomAbilitySelectionBehavior;
                return null;
            }
        }

        private static int SelectedFloor => Rooms != null ? Rooms.GetSelectedRoom() : -1;

        /// <summary>Select a floor as the player's scrolling does (the camera follows); a no-op on the
        /// floor already selected.</summary>
        private static void SelectFloor(int room)
        {
            var rooms = Rooms;
            if (rooms == null || room < 0 || room >= rooms.GetNumRooms() || rooms.GetSelectedRoom() == room) return;
            var ui = rooms.GetRoomUI();
            if (ui != null) ui.StartCoroutine(ui.SetSelectedRoom(room, fromPlayerInput: true));
        }

        /// <summary>The units on a floor, both sides and a boss outside the train, left to right as drawn
        /// (the player's back line to front, then the enemy's front line to back).</summary>
        private static List<CharacterState> Units(int room)
        {
            var units = new List<CharacterState>();
            var state = Rooms?.GetRoom(room);
            if (state == null) return units;
            state.AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
            var boss = Managers.GetHeroManager()?.GetOuterTrainBossCharacterAtRoom(room);
            if (boss != null && !units.Contains(boss)) units.Add(boss);
            units.RemoveAll(u => u == null || u.IsDestroyed || u.PreviewMode);
            units.Sort((a, b) =>
            {
                int c = X(a).CompareTo(X(b));
                return c != 0 ? c : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            return units;
        }

        private static float X(CharacterState unit)
        {
            var ui = unit.GetCharacterUI();
            return ui != null ? ui.transform.position.x : 0f;
        }

        /// <summary>"Floor 2" (1 at the bottom, where the enemy boards).</summary>
        private static string FloorName(int room) => Strings.Battle("floor", room + 1);

        /// <summary>"3/5": the player's room left on the floor, of its capacity.</summary>
        private static string Capacity(RoomState room)
        {
            if (room == null) return null;
            var info = room.GetCapacityInfo(Team.Type.Monsters);
            return Strings.Battle("capacity", Mathf.Max(0, info.max - info.count), info.max);
        }

        /// <summary>The capacity, with the floor's echoes when it holds any ("3/5, 2/4").</summary>
        private static string CapacityWithEchoes(RoomState room)
        {
            string capacity = Capacity(room);
            if (capacity == null || !room.corruptionEnabled || room.GetCurrentCorruption() <= 0) return capacity;
            return Strings.Battle("capacity_echo", capacity, room.GetCurrentCorruption(), room.GetMaxCorruption());
        }

        /// <summary>The floor's room upgrades the game shows, by name.</summary>
        private static List<string> Attachments(RoomState room)
        {
            var names = new List<string>();
            if (room == null) return names;
            var attachments = new List<TrainRoomAttachmentState>();
            room.TryGetTrainRoomAttachments(room.Attachments, isHidden: false, attachments);
            foreach (var attachment in attachments)
            {
                string name = attachment?.CardState?.GetTitle();
                if (string.IsNullOrWhiteSpace(name) && attachment != null)
                    foreach (var upgrade in attachment.UpgradeStates)
                    {
                        name = GameText.Term(upgrade.Item2?.GetUpgradeTitleKey());
                        if (!string.IsNullOrWhiteSpace(name)) break;
                    }
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
            }
            return names;
        }

        /// <summary>"Floor 2, 3/5, Training Ground": the floor, its capacity, its upgrades.</summary>
        private static string FloorLabel(int room)
        {
            var state = Rooms?.GetRoom(room);
            var parts = new List<string> { FloorName(room) };
            string capacity = Capacity(state);
            if (capacity != null) parts.Add(capacity);
            parts.AddRange(Attachments(state));
            return string.Join(", ", parts);
        }
    }
}
