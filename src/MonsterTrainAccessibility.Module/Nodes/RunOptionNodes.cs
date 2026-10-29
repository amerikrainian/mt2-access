using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Module.UI;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A run dialog's run types (<see cref="GameModeOption"/>: Depart's over the Covenant Outpost, the
    /// Rift Station's over the Soul Savior screen): each its panel's title ("Resume Run", "Daily
    /// Challenge") on the panel's play button; and the saved run as Resume Run's panel draws it: the mode
    /// label when there is one ("Endless Mode", "Soul Savior"), then each stat the game gives a tooltip
    /// (clans, Covenant rank, difficulty, the bosses, mutators), in the panel's order, read by its own
    /// tooltips, the Covenant's effects in the buffer.
    /// </summary>
    internal static class RunOptionNodes
    {
        public static ControlId Option(GraphBuilder b, string key, GameModeOption option)
        {
            if (option == null || !option.gameObject.activeInHierarchy || option.playButton == null) return null;
            var button = option.playButton.Button;
            if (button == null) return null;
            var id = ControlId.Structural(key);
            b.AddItem(id, GameWidgets.Button(button, () => Screens.ChallengeProgressScreen.HeaderOf(option, "Title label")));
            return id;
        }

        public static void SavedRun(GraphBuilder b, string key, ContinueGameOption option)
        {
            ModeLabel(b, key + ":mode:endless", option.endlessModeLabel);
            ModeLabel(b, key + ":mode:soulsavior", option.soulSaviorModeLabel);
            int i = 0;
            foreach (var provider in option.GetComponentsInChildren<ITooltipProvider>(false))
            {
                var widget = provider as Component;
                if (widget == null || TooltipReader.Lines(widget).Count == 0) continue;
                var id = ControlId.Structural(key + ":run:" + widget.gameObject.name + ":" + i++);
                var covenant = widget.GetComponent<ChallengeCovenantUI>();
                b.AddItem(id, covenant != null ? StartingConditionsNodes.Covenant(covenant) : TooltipReader.Item(widget));
            }
        }

        private static void ModeLabel(GraphBuilder b, string key, Transform label)
        {
            if (label == null || !label.gameObject.activeInHierarchy) return;
            var text = label.GetComponentInChildren<TMP_Text>(false);
            if (text == null || string.IsNullOrWhiteSpace(text.Content())) return;
            b.AddItem(ControlId.Structural(key), GameNodes.Text(() => text.Content()));
        }
    }
}
