using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The Train Depot (<see cref="global::TrainCosmeticsScreen"/>): the train's three rooms, top to
    /// bottom, each a combo box of its look ("Top Room, combo box, Skull Crusher", then the room's
    /// description), then Back. Enter opens the game's room picker, read by
    /// <see cref="TrainRoomPickerScreen"/>; Escape is the game's, back to the Covenant Outpost.
    /// </summary>
    public sealed class TrainCosmeticsScreen : Screen
    {
        public override string Key => "game.train_depot";
        public override bool KeepStateOnPop => true;

        internal static global::TrainCosmeticsScreen Game
            => GameScreens.Active<global::TrainCosmeticsScreen>(global::ScreenName.TrainCosmetics);

        public override bool IsActive()
        {
            var screen = Game;
            return screen != null && !TrainRoomPickerScreen.IsOpen(screen);
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(Header(screen), Strings.RoleList);
            Room(b, "top", screen.topRoomSelectionUI, () => screen.topRoomData);
            Room(b, "middle", screen.midRoomSelectionUI, () => screen.midRoomData);
            Room(b, "bottom", screen.btmRoomSelectionUI, () => screen.btmRoomData);
            var back = screen.backButton;
            if (back != null && back.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("depot:back"), GameWidgets.Button(back, () => PauseMenuScreen.Caption(back)));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A room: its header as the label, its look as the value, its description after.
        private static void Room(GraphBuilder b, string key, TrainCosmeticsSelectionUI room, System.Func<RoomData> data)
        {
            var button = room != null ? room.EditCosmeticButton : null;
            if (button == null || !button.gameObject.activeInHierarchy) return;
            var vt = GameWidgets.Button(button, () => room.HeaderText != null ? room.HeaderText.Content() : null,
                () => data()?.GetDescription(),
                new NodeAnnouncement(() => room.CosmeticName != null ? room.CosmeticName.Content() : null,
                    live: true, kind: AnnouncementKinds.Value));
            vt.ControlType = ControlTypes.ComboBox;
            b.AddItem(ControlId.Structural("depot:" + key), vt);
        }

        // The screen's own heading ("Train Depot"), the "Text header" the game shows top left.
        private static string Header(global::TrainCosmeticsScreen screen)
        {
            foreach (var text in screen.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (text.gameObject.name == "Text header" && !string.IsNullOrWhiteSpace(text.Content())) return text.Content();
            return null;
        }
    }

    /// <summary>
    /// The Train Depot's room picker (<see cref="TrainCosmeticRoomSelectionUI"/>): every look the room can
    /// take, the equipped one marked "selected" and landed on first, a locked one with the game's
    /// "Locked" and its unlock condition and progress, then the look's description. Enter picks
    /// through the game (a locked look stays unpicked, as in the game); Escape closes the picker.
    /// </summary>
    public sealed class TrainRoomPickerScreen : Screen
    {
        public override string Key => "game.train_depot.rooms";
        public override int Layer => 5;
        public override bool Exclusive => true;

        internal static bool IsOpen(global::TrainCosmeticsScreen screen)
            => screen.roomSelectionDialog != null && screen.roomSelectionDialog.gameObject.activeInHierarchy;

        public override bool IsActive()
        {
            var screen = TrainCosmeticsScreen.Game;
            return screen != null && IsOpen(screen);
        }

        public override void Build(GraphBuilder b)
        {
            var screen = TrainCosmeticsScreen.Game;
            if (screen == null || !IsOpen(screen)) return;
            var picker = screen.roomSelectionDialog;
            var editing = screen.selectedSelectionUI;
            string equipped = Equipped(screen, picker.RoomIndex)?.GetID();
            b.PushContext(editing != null && editing.HeaderText != null ? editing.HeaderText.Content() : null, Strings.RoleList);
            foreach (var item in picker.trainCosmeticsRoomSelectionItems)
            {
                var look = item;
                var room = look != null ? look.RoomData : null;
                if (room == null || !look.gameObject.activeInHierarchy || look.button == null) continue;
                var id = ControlId.Structural("rooms:" + room.GetID());
                var vt = GameWidgets.Button(look.button, () => room.GetName(), () => Description(look, room),
                    GameNodes.SelectedPart(() => room.GetID() == Equipped(screen, picker.RoomIndex)?.GetID()),
                    new NodeAnnouncement(() => look.lockedRoot != null && look.lockedRoot.activeSelf ? GameText.Term("Locked") : null,
                        kind: AnnouncementKinds.Enabled));
                b.AddItem(id, vt);
                if (room.GetID() == equipped) b.SetStart(id);
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A locked look's unlock condition and progress first (what the player needs), then the
        // look's description.
        private static string Description(TrainCosmeticRoomSelectionItemUI look, RoomData room)
        {
            var parts = new List<string>();
            if (look.lockedRoot != null && look.lockedRoot.activeSelf)
            {
                string unlock = ProgressionReader.Line(look.progressionObjectiveUI);
                if (unlock != null) parts.Add(unlock);
            }
            parts.Add(room.GetDescription());
            return GameNodes.Sentences(parts);
        }

        // The look the room being edited wears now (the game's room indices: 2 top, 1 middle, 0 bottom).
        private static RoomData Equipped(global::TrainCosmeticsScreen screen, int roomIndex)
        {
            switch (roomIndex)
            {
                case 2: return screen.topRoomData;
                case 1: return screen.midRoomData;
                case 0: return screen.btmRoomData;
                default: return null;
            }
        }
    }
}
