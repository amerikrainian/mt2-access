using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// A run setup's clan picker (<see cref="RunSetupClanSelectionUI"/>, the base setup's or Soul
    /// Savior's, <see cref="RunSetupNodes.ClanPicker"/>), headed by the clan being chosen ("Primary Clan"): every clan the game offers, then its Random choices, each through
    /// <see cref="ClanNodes.RunSetupOption"/>, landing on the one in use. The description is the one the
    /// picker shows for the option: the clan's, or the Random choice's explanation for this slot. Enter
    /// picks through the game, which closes the picker; Escape closes it unchanged.
    /// </summary>
    public sealed class RunSetupClanPickerScreen : Screen
    {
        public override string Key => "game.run_setup.clans";
        public override int Layer => 5;
        public override bool Exclusive => true;

        private static RunSetupClanSelectionUI Picker(out RunSetupClassLevelInfoUI editing)
        {
            var picker = RunSetupNodes.ClanPicker(out editing);
            return picker != null && picker.IsOpen ? picker : null;
        }

        public override bool IsActive() => Picker(out _) != null;

        public override void Build(GraphBuilder b)
        {
            var picker = Picker(out var editing);
            if (picker == null || picker.classSelectionIconItem == null) return;
            b.PushContext(editing != null ? StartingConditionsNodes.HeaderOf(editing) : null, Strings.RoleList);
            foreach (var item in picker.classSelectionIconItem.ClassIcons)
            {
                var option = item;
                if (option == null || !option.gameObject.activeInHierarchy || option.ClassOptionData == null || option.Button == null) continue;
                var id = ControlId.Structural("clans:" + option.ClassOptionData.GetID());
                b.AddItem(id, ClanNodes.RunSetupOption(option, () => Description(picker, option.ClassOptionData)));
                if ((option.selectedIndicator != null && option.selectedIndicator.activeSelf)
                    || (option.randomSelectedIndicator != null && option.randomSelectedIndicator.activeSelf)) b.SetStart(id);
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // The description the picker shows for an option (RunSetupClanSelectionUI.SetClanState).
        private static string Description(RunSetupClanSelectionUI picker, RunSetupClanSelectionLayoutUI.ClanOptionData option)
        {
            if (option.IsRandom)
            {
                string key = picker.isMainClass ? picker.randomExplanationMainClassKey : picker.randomExplanationSublassKey;
                if (option.randomId == RunSetupClanSelectionLayoutUI.RandomIdLegacyEraOnly)
                    key = picker.isMainClass ? picker.randomExplanationLegacyEraMainClassKey : picker.randomExplanationLegacyEraSublassKey;
                else if (option.randomId == RunSetupClanSelectionLayoutUI.RandomIdYoungEraOnly)
                    key = picker.isMainClass ? picker.randomExplanationYoungEraMainClassKey : picker.randomExplanationYoungEraSublassKey;
                return GameText.Term(key);
            }
            return option.clanData != null ? GameText.Term(option.clanData.GetDescriptionKey()) : null;
        }
    }

    /// <summary>
    /// A run setup's pyre heart picker (<see cref="RunSetupPyreHeartSelectionUI"/>, the base setup's or
    /// Soul Savior's, <see cref="RunSetupNodes.PyreHeartPicker"/>), headed "Pyre Heart": every heart, then Random, each through <see cref="StartingConditionsNodes.PyreHeartOption"/>,
    /// landing on the one in use. Enter picks through the game, which closes the picker; Escape
    /// closes it unchanged.
    /// </summary>
    public sealed class RunSetupPyreHeartPickerScreen : Screen
    {
        public override string Key => "game.run_setup.pyre_hearts";
        public override int Layer => 5;
        public override bool Exclusive => true;

        private static RunSetupPyreHeartSelectionUI Picker(out PyreHeartInfoUI pyre)
        {
            var picker = RunSetupNodes.PyreHeartPicker(out pyre);
            return picker != null && picker.IsOpen ? picker : null;
        }

        public override bool IsActive() => Picker(out _) != null;

        public override void Build(GraphBuilder b)
        {
            var picker = Picker(out var pyre);
            if (picker == null || picker.pyreHeartSelectionUI == null) return;
            b.PushContext(pyre != null ? StartingConditionsNodes.HeaderOf(pyre) : null, Strings.RoleList);
            foreach (var item in picker.pyreHeartSelectionUI.PyreHeartIcons)
            {
                var option = item;
                if (option == null || !option.gameObject.activeInHierarchy || option.PyreHeartOptionData == null || option.Button == null) continue;
                var id = ControlId.Structural("pyres:" + (option.PyreHeartOptionData.GetID() ?? RunSetupPyreHeartSelectionLayoutUI.RandomId));
                b.AddItem(id, StartingConditionsNodes.PyreHeartOption(option, () => GameText.Term(picker.randomPyreHeartDescriptionKey)));
                if (option.selectedIndicator != null && option.selectedIndicator.activeSelf) b.SetStart(id);
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }
    }

    /// <summary>
    /// Soul Savior's difficulty picker (<see cref="RunSetupDifficultyTierSelectionUI"/>), headed
    /// "Difficulty": each tier by its name, "Locked" with what unlocks it, "selected" on the one in use,
    /// then what it means; landing on the one in use. Enter picks through the game, which closes the
    /// picker (a locked tier only says it is); Escape closes it unchanged.
    /// </summary>
    public sealed class RunSetupDifficultyPickerScreen : Screen
    {
        public override string Key => "game.soulsavior.run_setup.difficulty";
        public override int Layer => 5;
        public override bool Exclusive => true;

        private static RunSetupDifficultyTierSelectionUI Picker
        {
            get
            {
                var screen = RunSetupNodes.SoulSavior;
                var picker = screen != null ? screen.difficultyTierSelectionDialog : null;
                return picker != null && picker.IsOpen ? picker : null;
            }
        }

        public override bool IsActive() => Picker != null;

        public override void Build(GraphBuilder b)
        {
            var screen = RunSetupNodes.SoulSavior;
            var picker = Picker;
            if (screen == null || picker == null || picker.tierSelectionItems == null) return;
            b.PushContext(ChallengeProgressScreen.HeaderOf(screen.difficultyTierInfo, "Text Difficulty Tier header"), Strings.RoleList);
            foreach (var item in picker.tierSelectionItems)
            {
                var tier = item;
                if (tier == null || !tier.gameObject.activeInHierarchy || tier.Button == null) continue;
                var id = ControlId.Structural("difficulty:" + tier.DifficutyTier);
                var vt = GameWidgets.Button(tier.Button, () => RunSetupNodes.Text(tier.difficultyTierNameLabel),
                    () => GameNodes.Sentences(RunSetupNodes.Text(tier.difficultyTierDescriptionLabel), tier.IsLocked ? ProgressionReader.Line(tier.progressionObjectiveUI) : null),
                    new NodeAnnouncement(() => tier.IsLocked ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
                    GameNodes.SelectedPart(() => tier.selectedIndicator != null && tier.selectedIndicator.activeSelf));
                b.AddItem(id, vt);
                if (tier.selectedIndicator != null && tier.selectedIndicator.activeSelf) b.SetStart(id);
            }
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }
    }

    /// <summary>
    /// The run setup's first-time Covenant choice (<see cref="RunSetupFtueCovenantSelectionUI"/>, shown
    /// once per profile): the game's prompt, then its two choices ("No Covenant", "Covenant Rank 1")
    /// with the description under each. Enter chooses through the game; Escape closes it, as in the
    /// game, leaving the rank as it was.
    /// </summary>
    public sealed class RunSetupFtueCovenantScreen : Screen
    {
        public override string Key => "game.run_setup.first_covenant";
        public override int Layer => 6;
        public override bool Exclusive => true;

        private static RunSetupFtueCovenantSelectionUI Dialog
        {
            get
            {
                var screen = RunSetupScreen.Game;
                var dialog = screen != null ? screen.ftueCovenantSelectionDialog : null;
                return dialog != null && dialog.IsConsumingInputs() ? dialog : null;
            }
        }

        public override bool IsActive() => Dialog != null;

        public override void Build(GraphBuilder b)
        {
            var dialog = Dialog;
            if (dialog == null) return;
            b.PushContext(null, Strings.RoleDialog, positions: false);
            int i = 0;
            foreach (var text in dialog.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.Content()) || text.GetComponentInParent<GameUISelectableButton>() != null) continue;
                var shown = text;
                b.AddItem(ControlId.Structural("firstcovenant:text:" + i++), GameNodes.Text(() => shown.Content()));
            }
            Choice(b, "0", dialog.buttonCov0);
            Choice(b, "1", dialog.buttonCov1);
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A choice: its caption, then the description the game draws inside it.
        private static void Choice(GraphBuilder b, string key, GameUISelectableButton button)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("firstcovenant:" + key), GameWidgets.Button(button,
                () => PauseMenuScreen.Caption(button),
                () => ChallengeProgressScreen.HeaderOf(button, "Description Label")));
        }
    }
}
