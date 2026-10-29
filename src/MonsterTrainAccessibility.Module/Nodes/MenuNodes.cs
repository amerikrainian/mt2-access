using System;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// The game's menu places (<see cref="MainMenuButton"/>: the Covenant Outpost's and the Rift
    /// Station's) and the signed-in account beside them. A place reads its caption (or a label given for
    /// a captionless one, Settings), then "Locked" or "disabled" when the game says so, then the line the
    /// game shows on hover ("Start a Run") and any note. Enter submits through the game's input signal.
    /// </summary>
    internal static class MenuNodes
    {
        public static void Place(GraphBuilder b, string key, MainMenuButton place, Func<string> note = null, Func<string> label = null)
        {
            var button = place != null ? place.Button : null;
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural(key), GameWidgets.Button(button,
                label ?? (() => Caption(place)),
                () => GameNodes.Sentences(HoverLine(place), note?.Invoke())));
        }

        public static string Caption(MainMenuButton place)
            => place.label != null && !string.IsNullOrWhiteSpace(place.label.Content()) ? place.label.Content() : null;

        // Every other text the place carries: the line the game fades in on hover ("Start a Run").
        private static string HoverLine(MainMenuButton place)
        {
            foreach (var text in place.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null || text == place.label || string.IsNullOrWhiteSpace(text.Content())) continue;
                return text.Content();
            }
            return null;
        }

        /// <summary>A hub's progress panel ("Next Dialogue. Unlock All Souls. 11/33") while the game shows
        /// it, as one line (<see cref="ProgressionReader"/>).</summary>
        public static void Progress(GraphBuilder b, string key, ProgressionObjectiveUI panel)
        {
            if (panel == null || !panel.gameObject.activeInHierarchy || ProgressionReader.Line(panel) == null) return;
            b.AddItem(ControlId.Structural(key), GameNodes.Text(() => ProgressionReader.Line(panel)));
        }

        /// <summary>The signed-in account, where the platform has one: a button when the player may switch
        /// it, else the name alone.</summary>
        public static void Account(GraphBuilder b, string key, GameUISelectableButton change, TMP_Text changeName,
            GameUISelectableButton fixedUser, TMP_Text fixedName)
        {
            if (change != null && change.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural(key), GameWidgets.Button(change, () => changeName != null ? changeName.Content() : null));
            if (fixedUser != null && fixedUser.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural(key), GameNodes.Text(() => fixedName != null ? fixedName.Content() : null));
        }
    }
}
