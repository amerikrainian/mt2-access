using System;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A mastery card frame choice (<see cref="CardFrameOptionUI"/>, drawn as a picture of the frame):
    /// a button by the frame's name, "selected" on the frame in use, the game's "Locked" with its
    /// unlock goal and count (the game's own progression panel) while locked. Landing hovers it, as the
    /// pointer does: the game shows the goal on hover, in one panel it moves onto the hovered frame.
    /// Enter chooses it through the game.
    /// </summary>
    internal static class CardFrameNodes
    {
        public static NodeVtable Option(CardFrameOptionUI option)
        {
            Func<string> name = () => Name(option.FrameType);
            var vt = GameWidgets.Button(option.Button, name, () => Goal(option),
                GameNodes.SelectedPart(() => option.Button.state == ShinyShoe.GameUISelectableButton.State.Activated));
            vt.OnFocus = () => GameInput.Hover(option.Button);
            var activate = vt.OnActivate;
            vt.OnActivate = () => { if (!option.locked) activate(); };
            return vt;
        }

        // A locked frame's goal, from the game's one progression panel, which hover moves onto the
        // hovered frame and fills with its goal: read only while this frame holds it.
        private static string Goal(CardFrameOptionUI option)
        {
            var panel = option.progressionObjectiveUI;
            if (!option.locked || panel == null || panel.transform.parent != option.transform) return null;
            return ProgressionReader.Line(panel);
        }

        /// <summary>The frame's name: the game's own where it has one (the Pyre Crystal frame, which a
        /// reward names), ours otherwise.</summary>
        public static string Name(MasteryFrameType frame)
        {
            switch (frame)
            {
                case MasteryFrameType.Default: return Strings.FrameName(Strings.Frame("default"));
                case MasteryFrameType.Metallic: return Strings.FrameName(Strings.Frame("metallic"));
                case MasteryFrameType.Pyre: return GameText.Term("MasteryFrameReward_Title");
                case MasteryFrameType.HeavenThrone: return Strings.FrameName(Strings.Frame("heaven_throne"));
                case MasteryFrameType.Prismatic: return Strings.FrameName(Strings.Frame("prismatic"));
                default: return frame.ToString();
            }
        }
    }
}
