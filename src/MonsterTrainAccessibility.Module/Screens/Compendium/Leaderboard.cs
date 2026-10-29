using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Screens
{
    public sealed partial class CompendiumScreen
    {
        // ---- Progress Record: the friends' leaderboard ----

        // The sort choices (clan, cup, the stat ranked by, each "selected" as the game marks it), then
        // the players, then the game's own page controls and messages.
        private static void Leaderboard(GraphBuilder b, string id, CompendiumStatsLeaderboardPage board)
        {
            var sort = board.statsSortOptionsUI;
            if (sort != null)
            {
                if (sort.classDropdown != null && sort.classDropdown.gameObject.activeInHierarchy)
                    b.AddItem(ControlId.Structural(id + ":clan"), GameWidgets.Dropdown(sort.classDropdown, () => FilterNodes.Label(sort.classDropdown.transform.parent)));
                FilterNodes.Options(b, id + ":clans", sort.classFilterUI);
                FilterNodes.Options(b, id + ":cups", sort.covenantLevelFilterUI);
                foreach (var stat in new[] { sort.ascensionLevelButton, sort.scoreButton, sort.winCountButton, sort.winStreakButton, sort.endlessBattlesButton })
                {
                    if (stat == null || !stat.gameObject.activeInHierarchy) continue;
                    var button = stat;
                    b.AddItem(ControlId.Structural(id + ":stat:" + stat.name), GameWidgets.Button(button, () => PauseMenuScreen.Caption(button), null,
                        GameNodes.SelectedPart(() => button.state == GameUISelectableButton.State.Activated)));
                }
            }
            if (board.rowsRoot != null)
            {
                Transform header = null;
                int r = 0;
                foreach (Transform child in board.rowsRoot.transform)
                {
                    if (!child.gameObject.activeInHierarchy) continue;
                    var row = child.GetComponent<PlayerStatRow>();
                    if (row == null) { header = header ?? child; continue; }
                    if (Cells(row.transform).Count == 0) continue; // an empty slot of the page of ten
                    var h = header;
                    var playerRow = row;
                    b.AddItem(ControlId.Structural(id + ":row:" + r++), GameWidgets.Button(row.button, () => PlayerRow(playerRow.transform, h)));
                }
            }
            if (board.noFriendsMessage != null && board.noFriendsMessage.activeInHierarchy) Walk(b, board.noFriendsMessage.transform, id + ":nofriends");
            if (board.errorLabel != null && Spoken(board.errorLabel))
                b.AddItem(ControlId.Structural(id + ":error"), GameNodes.Text(() => board.errorLabel.Content()));
            PaginationNodes.Add(b, id + ":pages", board.paginationControls);
        }

        // "Oleg P, Rank: 1, Win Streak: 2": the name, then each shown cell under its column's header
        // (the header row's text of the same name).
        private static string PlayerRow(Transform row, Transform header)
        {
            var cells = Cells(row);
            string name = null;
            var parts = new List<string>();
            foreach (var cell in cells)
            {
                if (cell.name == "Name label") { name = cell.Content(); continue; }
                var title = header != null ? header.Find(cell.name)?.GetComponent<TMP_Text>() : null;
                parts.Add(title != null && Spoken(title) ? title.Content() + ": " + cell.Content() : cell.Content());
            }
            if (name != null) parts.Insert(0, name);
            return string.Join(", ", parts);
        }

        // A row's shown texts, in order; a column the game hides (its text component disabled) is not one.
        private static List<TMP_Text> Cells(Transform row)
        {
            var cells = new List<TMP_Text>();
            foreach (Transform child in row)
            {
                var text = child.GetComponent<TMP_Text>();
                if (Spoken(text)) cells.Add(text);
            }
            return cells;
        }
    }
}
