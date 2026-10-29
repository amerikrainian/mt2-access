using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// Soul Savior's soul choice (<see cref="SoulSelectionDialog"/>, opened by the run setup's Souls).
    /// Opening it speaks the game's instructions ("Choose 2 Souls to start your run. ..."); then one
    /// Tab-stop, top to bottom: the soul types as a row of tabs (Unit, Spell, Global; landing on one opens
    /// it, and a vertical move into the row lands on the open one); the open type's souls as a row
    /// (<see cref="SoulNodes.Choice"/>: landing hovers one, a toggle "on" while chosen, Enter choosing or
    /// dropping it through the game); "Selected Souls" as a row of the slots the game fills, each by its
    /// tooltip (the soul in it, or what unlocks a locked slot; an open slot draws nothing and is not
    /// listed); Remove All Souls and Randomize Souls (which closes, as in the game); Back. The game's
    /// Preview button, which only moves its own controller focus to the slots, is left out: the slots are
    /// their own row here. Escape is the game's: it closes the choice, keeping it.
    /// </summary>
    public sealed class SoulSelectionScreen : Screen
    {
        public override string Key => "game.soulsavior.run_setup.souls";
        public override int Layer => 5;
        public override bool Exclusive => true;

        private static global::SoulSaviorRunSetupScreen Setup => RunSetupNodes.SoulSavior;

        private static SoulSelectionDialog Dialog
        {
            get
            {
                var screen = Setup;
                var dialog = screen != null ? screen.soulSelectionDialog : null;
                return dialog != null && dialog.IsOpen ? dialog : null;
            }
        }

        public override bool IsActive() => Dialog != null;

        public override void OnPush()
        {
            var dialog = Dialog;
            if (dialog == null) return;
            ScreenIntro.Say(RunSetupNodes.Text(dialog.descriptionLabel));
        }

        public override void Build(GraphBuilder b)
        {
            var screen = Setup;
            var dialog = Dialog;
            if (screen == null || dialog == null) return;
            b.PushContext(null);

            var tabs = new List<SettingsTab>();
            if (dialog.tabs != null)
                foreach (var tab in dialog.tabs)
                    if (tab != null && tab.button != null && tab.button.gameObject.activeInHierarchy) tabs.Add(tab);
            if (tabs.Count > 0)
            {
                b.StartRow(entry: () => System.Math.Max(0, tabs.FindIndex(t => t.SectionRoot != null && t.SectionRoot.activeInHierarchy)));
                foreach (var tab in tabs) b.AddItem(ControlId.Structural("souls:tab:" + tab.gameObject.name), SettingsScreen.Tab(tab));
                b.EndRow();
            }

            var open = tabs.Find(t => t.SectionRoot != null && t.SectionRoot.activeInHierarchy);
            var souls = new List<RunSetupSoulSelectionItemUI>();
            foreach (var item in Souls(dialog))
                if (item != null && item.gameObject.activeInHierarchy && item.SoulData != null) souls.Add(item);
            ControlId first = null;
            if (souls.Count > 0)
            {
                b.PushContext(open != null ? PauseMenuScreen.Caption(open.button) : null, Strings.RoleList);
                b.StartRow();
                foreach (var soul in souls)
                {
                    var id = ControlId.Structural("souls:soul:" + soul.SoulData.GetID());
                    first = first ?? id;
                    b.AddItem(id, SoulNodes.Choice(soul, dialog));
                }
                b.EndRow();
                b.PopContext();
            }

            Selected(b, dialog);

            var footer = new List<GameUISelectableButton>();
            foreach (var button in new[] { dialog.removeAllButton, dialog.randomizeButton })
                if (button != null && button.gameObject.activeInHierarchy) footer.Add(button);
            if (footer.Count > 0)
            {
                b.StartRow();
                foreach (var button in footer)
                {
                    var shown = button;
                    b.AddItem(ControlId.Structural("souls:" + button.gameObject.name), GameWidgets.Button(shown, () => PauseMenuScreen.Caption(shown)));
                }
                b.EndRow();
            }
            var back = dialog.backButton;
            if (back != null && back.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural("souls:back"), GameWidgets.Button(back, () => PauseMenuScreen.Caption(back)));
            b.PopContext();
            if (first != null) b.SetStart(first);
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The slots the game fills ("Selected Souls"): each by its tooltip, the chosen soul or the unlock hint.
        private static void Selected(GraphBuilder b, SoulSelectionDialog dialog)
        {
            var slots = new List<KeyValuePair<int, TooltipProviderComponent>>();
            var tips = new[] { dialog.soulPreviewTooltip1, dialog.soulPreviewTooltip2, dialog.soulPreviewTooltip3 };
            for (int i = 0; i < tips.Length; i++)
                if (tips[i] != null && tips[i].gameObject.activeInHierarchy && TooltipReader.Lines(tips[i]).Count > 0)
                    slots.Add(new KeyValuePair<int, TooltipProviderComponent>(i, tips[i]));
            if (slots.Count == 0) return;
            b.PushContext(ChallengeProgressScreen.HeaderOf(dialog, "Text selected souls"), Strings.RoleList);
            b.StartRow();
            foreach (var slot in slots) b.AddItem(ControlId.Structural("souls:slot:" + slot.Key), TooltipReader.Item(slot.Value));
            b.EndRow();
            b.PopContext();
        }

        // The open type's souls, as the game lists them.
        private static List<RunSetupSoulSelectionItemUI> Souls(SoulSelectionDialog dialog)
        {
            switch (dialog.tabIndex)
            {
                case 0: return dialog.unitSoulItems;
                case 1: return dialog.spellSoulItems;
                case 2: return dialog.artifactSoulItems;
                default: return new List<RunSetupSoulSelectionItemUI>();
            }
        }
    }
}
