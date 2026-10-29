using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
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
    /// The Dragon's Hoard (<see cref="global::DragonsHoardScreen"/>), under its title: the game's
    /// question, the current loot level as drawn ("Current Loot Level: 0 / 8", the hoard and its
    /// maximum), the rewards looting now would give when there are any (each by its own tooltip), then
    /// View Loot Levels (<see cref="DragonsHoardLevelsScreen"/>), Loot and Save, all pressed through the
    /// game. Focus lands on the loot level, not on Loot. Escape is the game's: it leaves the hoard.
    /// </summary>
    public sealed class DragonsHoardScreen : Screen
    {
        private static readonly InputCategory[] Categories = { InputCategory.Game, InputCategory.UI };

        public override string Key => "game.dragons_hoard";
        public override int Layer => 10;
        public override bool Exclusive => true;
        public override IReadOnlyList<InputCategory> InputCategories => Categories;

        internal static global::DragonsHoardScreen Game
        {
            get
            {
                var screen = GameScreens.Active<global::DragonsHoardScreen>(global::ScreenName.DragonsHoard);
                return screen != null && screen.IsInteractable() ? screen : null;
            }
        }

        public override bool IsActive()
        {
            var screen = Game;
            return screen != null && !DragonsHoardLevelsScreen.IsOpen(screen);
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null) return;
            b.PushContext(Title(screen), Strings.RoleList);
            Text(b, "question", ChallengeProgressScreen.HeaderOf(screen, "Instructions"));
            var level = screen.currentDragonsHoardLootLabel;
            if (level != null && level.gameObject.activeInHierarchy)
            {
                var id = ControlId.Structural("hoard:level");
                b.AddItem(id, GameNodes.Text(() => GameNodes.Sentences(
                    ChallengeProgressScreen.HeaderOf(screen, "Text Current Loot Level"), Lines(level))));
                b.SetStart(id);
            }
            if (screen.rewardsContainer != null && screen.rewardsContainer.gameObject.activeInHierarchy)
            {
                int i = 0;
                foreach (var reward in screen.rewardsContainer.GetComponentsInChildren<RewardItemUI>(false))
                    if (TooltipReader.Lines(reward).Count > 0) b.AddItem(ControlId.Structural("hoard:reward:" + i++), TooltipReader.Item(reward));
            }
            Button(b, "levels", screen.lootLevelsButton);
            Button(b, "loot", screen.confirmButton);
            Button(b, "save", screen.cancelButton);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The hoard's title, its icon dropped (the icon is the hoard's own: it would say the name twice).
        internal static string Title(Component screen)
            => GameSprites.WithoutEchoedIcons(ChallengeProgressScreen.HeaderOf(screen, "Title"))?.Trim();

        // A two-line count as one line ("0", "8" read "0 / 8").
        private static string Lines(TMP_Text label)
        {
            string text = label.Content();
            if (string.IsNullOrWhiteSpace(text)) return null;
            var lines = new List<string>();
            foreach (var line in System.Text.RegularExpressions.Regex.Split(text, @"<br\s*/?>|\n"))
                if (!string.IsNullOrWhiteSpace(line)) lines.Add(line.Trim());
            return string.Join(" / ", lines);
        }

        private static void Text(GraphBuilder b, string key, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            b.AddItem(ControlId.Structural("hoard:" + key), GameNodes.Text(() => text));
        }

        private static void Button(GraphBuilder b, string key, GameUISelectableButton button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || PauseMenuScreen.Caption(button) == null) return;
            b.AddItem(ControlId.Structural("hoard:" + key), GameWidgets.Button(button, () => PauseMenuScreen.Caption(button)));
        }
    }

    /// <summary>
    /// The Dragon's Hoard's loot levels (<see cref="DragonsHoardInfoDialog"/>), under the game's "Loot
    /// Levels": its current loot level line, then every level in the game's order, "Level 3, current,
    /// Gain 25 Coin. A Random Minor Pyreborne Artifact" (each reward by its own tooltip), the game's
    /// "Locked" for a level above the hoard's maximum, with the game's note on unlocking them where it
    /// shows it. Landing on a level shows it in the dialog's panel, as the pointer does. Escape closes
    /// the dialog, as in the game.
    /// </summary>
    public sealed class DragonsHoardLevelsScreen : Screen
    {
        public override string Key => "game.dragons_hoard.levels";
        public override int Layer => 11;
        public override bool Exclusive => true;

        internal static bool IsOpen(global::DragonsHoardScreen screen)
            => screen.dragonsHoardInfoDialog != null && screen.dragonsHoardInfoDialog.gameObject.activeInHierarchy;

        public override bool IsActive()
        {
            var screen = DragonsHoardScreen.Game;
            return screen != null && IsOpen(screen);
        }

        public override void Build(GraphBuilder b)
        {
            var screen = DragonsHoardScreen.Game;
            if (screen == null || !IsOpen(screen)) return;
            var dialog = screen.dragonsHoardInfoDialog;
            // The header shares its object name with the current-level line: found under its own root.
            var header = FindDeep(dialog.transform, "Loot Level Header");
            b.PushContext(header != null ? ChallengeProgressScreen.HeaderOf(header, "Text Loot Level Info") : null, Strings.RoleList);
            var current = dialog.currentLootLevelText;
            if (current != null && current.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("levels:current"), GameNodes.Text(() => current.Content()));
            var save = AllGameManagers.Instance.GetSaveManager();
            var first = dialog.dragonsHoardRewardSelectionItems != null && dialog.dragonsHoardRewardSelectionItems.Count > 0
                ? dialog.dragonsHoardRewardSelectionItems[0] : null;
            if (first != null)
            {
                // The levels in the dialog's order; the game's note on unlocking (laid out on its own)
                // goes before the first locked level, which it is about.
                TMP_Text note = null;
                var items = new List<DragonsHoardRewardSelectionItem>();
                foreach (Transform child in first.transform.parent)
                {
                    if (!child.gameObject.activeInHierarchy) continue;
                    var item = child.GetComponent<DragonsHoardRewardSelectionItem>();
                    if (item != null && item.RewardNodeData != null) items.Add(item);
                    else if (item == null) note = note ?? child.GetComponentInChildren<TMP_Text>(false);
                }
                bool noted = note == null || string.IsNullOrWhiteSpace(note.Content());
                for (int i = 0; i < items.Count; i++)
                {
                    int number = i + 1;
                    if (!noted && save != null && number > save.GetDragonsHoardCap())
                    {
                        var shown = note;
                        b.AddItem(ControlId.Structural("levels:note"), GameNodes.Text(() => shown.Content()));
                        noted = true;
                    }
                    b.AddItem(ControlId.Structural("levels:" + number), Level(items[i], number, save));
                }
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A level: its number, "current" when the hoard is at it, the game's "Locked" above the hoard's
        // maximum (its own lock mark clears while hovered, so the maximum decides), then its rewards.
        private static NodeVtable Level(DragonsHoardRewardSelectionItem item, int number, SaveManager save)
        {
            System.Func<string> label = () => Strings.HoardLevel(number);
            return new NodeVtable
            {
                ControlType = ControlTypes.Item,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(() => item.currentLootLevelHighlight != null && item.currentLootLevelHighlight.activeSelf ? Strings.StateCurrent : null,
                        kind: AnnouncementKinds.Value),
                    new NodeAnnouncement(() => save != null && number > save.GetDragonsHoardCap() ? GameText.Term("Locked") : null,
                        kind: AnnouncementKinds.Enabled),
                    GameNodes.TooltipPart(() => GameNodes.Sentences(Rewards(item))),
                },
                SearchText = label,
                OnFocus = () => GameInput.Hover(item.GetDefaultGameUISelectable()),
                Details = () => Rewards(item),
            };
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(false))
                if (child.name == name) return child;
            return null;
        }

        private static List<string> Rewards(DragonsHoardRewardSelectionItem item)
        {
            var lines = new List<string>();
            foreach (var reward in item.GetComponentsInChildren<RewardItemUI>(false))
                lines.AddRange(TooltipReader.Lines(reward));
            return lines;
        }
    }
}
