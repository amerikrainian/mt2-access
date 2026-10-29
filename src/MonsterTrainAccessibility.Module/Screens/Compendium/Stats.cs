using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.Module.Screens
{
    public sealed partial class CompendiumScreen
    {
        // ---- Progress Record ----

        // The record pages as a row of tabs (the game disables the open one's button), then the open
        // page as drawn.
        private static void Stats(GraphBuilder b, CompendiumSectionStats stats)
        {
            var pages = new List<GameUISelectableButton>();
            foreach (var button in new[] { stats.runStatsButton, stats.endlessRunStatsButton, stats.soulSaviorStatsButton, stats.runTypesButton, stats.statsLeaderboardButton })
                if (button != null && button.gameObject.activeInHierarchy) pages.Add(button);
            b.StartRow(entry: () => System.Math.Max(0, pages.FindIndex(p => !p.interactable)));
            foreach (var page in pages)
                b.AddItem(ControlId.Structural("logbook:stats:page:" + page.name), RecordTab(page));
            b.EndRow();
            Walk(b, stats.transform, "logbook:Stats", pages.Count > 0 ? pages[0].transform.parent : null);
        }

        private static NodeVtable RecordTab(GameUISelectableButton button)
        {
            System.Func<string> name = () => PauseMenuScreen.Caption(button);
            System.Action open = () => { if (button.interactable) GameInput.Submit(button); };
            return new NodeVtable
            {
                ControlType = ControlTypes.Tab,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(name) },
                SearchText = name,
                OnFocus = open,
                OnActivate = open,
            };
        }
    }
}
