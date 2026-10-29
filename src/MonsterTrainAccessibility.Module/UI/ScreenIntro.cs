using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// What a screen says as it opens: its title and the game's instruction lines ("Herzal's Hoard.
    /// Choose an artifact."), spoken once, queued ahead of the first landing. A line that only
    /// instructs is said, never listed as an item to land on; the screen's list then goes unnamed, so
    /// the first landing reads the control itself. Call it from the screen's <c>OnPush</c>, so it is
    /// not said again when focus comes back from a dialog over the screen.
    /// </summary>
    internal static class ScreenIntro
    {
        public static void Say(params string[] lines)
        {
            if (!Core.UI.Navigation.FocusActive()) return;
            string intro = GameNodes.Sentences(lines);
            if (!string.IsNullOrWhiteSpace(intro)) Speech.Say(intro);
        }
    }
}
