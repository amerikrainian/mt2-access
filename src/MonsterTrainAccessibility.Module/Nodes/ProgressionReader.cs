using System.Collections.Generic;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// What a <see cref="ProgressionObjectiveUI"/> says (the game's unlock or objective panel: a locked
    /// cosmetic's condition, a mastery goal): its title, its description and its count ("2/5"), each
    /// only while the game has it set, in the game's own wording. Read whether or not the panel is on
    /// screen: the game fills it ahead of showing it on hover.
    /// </summary>
    internal static class ProgressionReader
    {
        public static List<string> Lines(ProgressionObjectiveUI objective)
        {
            var lines = new List<string>();
            if (objective == null) return lines;
            if (objective.titleRoot != null && objective.titleRoot.activeSelf) Add(lines, objective.titleLabel);
            if (objective.descriptionRoot != null && objective.descriptionRoot.activeSelf) Add(lines, objective.descriptionLabel);
            if (objective.numericLabel != null && objective.numericLabel.gameObject.activeSelf) Add(lines, objective.numericLabel);
            return lines;
        }

        /// <summary>The same as one line of sentences, or null when the panel says nothing.</summary>
        public static string Line(ProgressionObjectiveUI objective) => UI.GameNodes.Sentences(Lines(objective));

        private static void Add(List<string> lines, TMPro.TMP_Text text)
        {
            if (text != null && !string.IsNullOrWhiteSpace(text.text)) lines.Add(text.text);
        }
    }
}
