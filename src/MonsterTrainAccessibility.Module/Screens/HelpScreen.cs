using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Buffers;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using Navigation = MonsterTrainAccessibility.Core.UI.Navigation;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    // The game declares its own InputManager and ScreenManager in the global namespace.
    using InputManager = Core.Input.InputManager;
    using ScreenManager = Core.Screens.ScreenManager;

    /// <summary>
    /// "Keys here" (F1 anywhere): the keys that would do something right now, for the screen and the
    /// control the player stood on when they asked (<see cref="KeyHelp"/>: reroll and freeze in the
    /// shop, the hex keys on a board cell, a glance only where its fact is on screen), each row what
    /// the key does and the keys bound to it. The list is taken the moment the help opens, while the
    /// game screen still has the focus: once this overlay is up, the focused control is its own.
    /// <para>Enter on a row runs the action as the key would have: the help closes, focus is back
    /// where it was, the action is dispatched through the module's real input path, and then the
    /// focus is read again, since the action is likely to have changed what stands there. What is
    /// heard, in order: whatever the action itself says, then the focused control; never the action's
    /// name, which the player has just read off the row. A speech hold keeps that order (its first
    /// line interrupts, the rest queue: an action's feedback interrupts, some of it frames later); the
    /// landing a closing overlay brings is taken quiet, the focus being read after the action
    /// instead. The focus is not read again when the action moved it (the move spoke it), when it
    /// opened another screen (which announces itself), or when the action's own feedback already
    /// said it (the shop's reroll).</para>
    /// </summary>
    public sealed class HelpScreen : Screen
    {
        public const string ActionKey = "mod.help";

        // Frames from closing to dispatching: the overlay pops and the game screen rebinds and renders
        // within the frame, one more for good measure. Then from dispatching to reading the focus: past
        // the longest feedback a key defers (the shop's reroll waits twelve for the game to redraw).
        private const int DispatchAfter = 2;
        private const int FocusAfter = 16;

        private static bool s_open;
        private static List<KeyHelpEntry> s_entries = new List<KeyHelpEntry>();

        public static void Toggle()
        {
            if (s_open) { s_open = false; return; }
            // Collected now, against the screen and the control the player is on.
            s_entries = KeyHelp.Collect(action => action.Key == ActionKey);
            s_open = true;
            Core.Speech.Say(Strings.ScreenHelp, interrupt: true);
        }

        public override string Key => "mod.help";
        public override int Layer => 51;
        public override bool Exclusive => true;
        public override bool IsActive() => s_open;

        public override void Build(GraphBuilder b)
        {
            // A context with no words of its own, there for the rows' positions: opening says "Keys
            // here" itself, interrupting whatever was being said, so a label would say it a second
            // time, and "list" tells a player who asked for the keys nothing.
            b.PushContext(null, null);
            if (s_entries.Count == 0)
                b.AddItem(ControlId.Structural("help:none"), GameNodes.Text(() => Strings.HelpNone));
            foreach (var entry in s_entries)
            {
                var e = entry;
                b.AddItem(ControlId.Structural("help:" + e.ActionKey), new NodeVtable
                {
                    Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(() => Strings.HelpRow(e.Label, e.Keys)) },
                    SearchText = () => e.Label,
                    OnActivate = () => Perform(e),
                });
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => s_open = false);
        }

        // Nothing is said for the Enter itself: the player has just read the row. The hold's first
        // line (the action's own, else the focus) cuts the row's readout off.
        private static void Perform(KeyHelpEntry entry)
        {
            Core.Speech.BeginHold();
            Navigation.QuietNextLanding();
            s_open = false;
            Later.Frames(DispatchAfter, () => Dispatch(entry.ActionKey), "help dispatch");
        }

        private static void Dispatch(string actionKey)
        {
            var screen = Navigation.BoundScreen;
            var before = Navigation.FocusedNode != null ? Navigation.FocusedNode.Id : null;
            if (!InputManager.Dispatch(actionKey)) { Core.Speech.EndHold(); return; }
            Later.Frames(FocusAfter, () => ReadFocus(screen, before), "help focus");
        }

        private static void ReadFocus(Screen screen, ControlId before)
        {
            try
            {
                // A real key press since then ended the hold: the player has moved on.
                if (!Core.Speech.Holding) return;
                var node = Navigation.FocusedNode;
                if (node == null || !ReferenceEquals(Navigation.BoundScreen, screen)) return; // another screen: it announces itself
                if (!Equals(node.Id, before)) return;                                          // the action moved focus and spoke it
                string head = null;
                foreach (var line in NodeLines.Lines(node)) { head = line; break; }
                if (head != null && AlreadySaid(head)) return;
                Core.Speech.Say(GraphAnnouncer.LeafText(node));
            }
            finally { Core.Speech.EndHold(); }
        }

        // Whether the action's own feedback read the focused control already (the shop's reroll ends
        // with its line).
        private static bool AlreadySaid(string head)
        {
            string clean = MonsterTrainAccessibility.Contracts.TextFilter.Clean(head);
            if (string.IsNullOrWhiteSpace(clean)) return false;
            foreach (var said in Core.Speech.Held)
                if (MonsterTrainAccessibility.Contracts.TextFilter.Clean(said).IndexOf(clean, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
