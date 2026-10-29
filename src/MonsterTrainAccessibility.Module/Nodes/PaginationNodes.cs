using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A leaderboard's page controls (<see cref="PaginationControls"/>): First Page, Previous Page, Your
    /// Rank, Next Page, Last Page in a row, pressed through the game (disabled at an end), then "Page 2 of 6".
    /// The buttons are drawn as arrows, and their tooltips all carry the Your Rank key, so each is named
    /// by the game's own term for what it does.
    /// </summary>
    internal static class PaginationNodes
    {
        public static void Add(GraphBuilder b, string id, PaginationControls pages)
        {
            if (pages == null || !pages.gameObject.activeInHierarchy) return;
            b.StartRow();
            Button(b, id + ":first", pages.firstButton, "Leaderboard_Navigation_FirstPage");
            Button(b, id + ":prev", pages.prevButton, "Leaderboard_Navigation_PreviousPage");
            Button(b, id + ":player", pages.focusPlayerButton, "Leaderboard_Navigation_PlayerPage");
            Button(b, id + ":next", pages.nextButton, "Leaderboard_Navigation_NextPage");
            Button(b, id + ":last", pages.lastButton, "Leaderboard_Navigation_LastPage");
            b.EndRow();
            if (Page(pages) != null) b.AddItem(ControlId.Structural(id + ":page"), GameNodes.Text(() => Page(pages)));
        }

        /// <summary>"Page 2 of 6", as drawn; null while it is hidden.</summary>
        public static string Page(PaginationControls pages)
        {
            TMP_Text label = pages != null ? pages.pageLabel : null;
            string text = label != null && label.gameObject.activeInHierarchy ? label.Content() : null;
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static void Button(GraphBuilder b, string id, GameUISelectableButton button, string term)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural(id), GameWidgets.Button(button, () => GameText.Term(term)));
        }
    }
}
