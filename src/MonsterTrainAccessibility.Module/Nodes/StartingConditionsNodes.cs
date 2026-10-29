using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A run's starting conditions as the game lays them out wherever it shows them (a Dimensional
    /// Challenge's details, the run setup and its pickers): the clans, the pyre heart, the Covenant
    /// rank, the mutators. Lines in the game's words, each with its hover tooltips in the control
    /// buffer; the run setup's choices act through the game's own buttons.
    /// </summary>
    internal static class StartingConditionsNodes
    {
        /// <summary>"Primary Clan, Hellhorned": the clan's header, then its name.</summary>
        public static NodeVtable Clan(ClassLevelInfoUI clan)
        {
            return Line(() => HeaderOf(clan), () => Text(clan.titleLabel), clan);
        }

        /// <summary>"Pyre Heart, Proto Heartcage, Pyre attack 45, Pyre health 80".</summary>
        public static NodeVtable PyreHeart(PyreHeartInfoUI pyre)
        {
            return Line(() => HeaderOf(pyre), () => PyreHeartValue(pyre), pyre);
        }

        /// <summary>The heart's name and stats as drawn: "Proto Heartcage, Pyre attack 45, Pyre health 80".</summary>
        public static string PyreHeartValue(PyreHeartInfoUI pyre)
        {
            return Commas(
                Text(pyre.titleLabel),
                Stat(Strings.PyreAttack, AttackOf(pyre.abilityUI)),
                pyre.healthUIObject != null && pyre.healthUIObject.activeInHierarchy ? Stat(Strings.PyreHealth, Text(pyre.healthUILabel)) : null);
        }

        /// <summary>"Covenant Rank 1", the game's own tooltip title, its effects in the buffer.</summary>
        public static NodeVtable Covenant(ChallengeCovenantUI covenant)
        {
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(() => CovenantTitle(covenant, null) ?? Text(covenant.rankLabel)) },
                SearchText = () => CovenantTitle(covenant, null),
                Details = () => GameNodes.Lines(TooltipReader.Lines(CovenantTooltips(covenant, null))),
            };
        }

        /// <summary>The run setup's Covenant rank: "Covenant Rank, slider, 5" ("No Covenant" at none).
        /// Left and right step it through the game's own level buttons (which save it and refresh the
        /// win streak and the clan pair's rank); its effects are in the buffer.</summary>
        public static NodeVtable CovenantSelector(CovenantSelectionUI selector, Func<string> label)
        {
            var covenant = selector.covenantUI;
            Func<string> value = () => covenant.CurrentLevel > 0
                ? Text(covenant.rankLabel) ?? CovenantTitle(covenant, selector.enabledDlcs)
                : CovenantTitle(covenant, selector.enabledDlcs);
            return new NodeVtable
            {
                ControlType = ControlTypes.Slider,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(value, live: true, kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                OnAdjust = (sign, large) =>
                {
                    var step = sign > 0 ? selector.levelUpButton : selector.levelDownButton;
                    if (step != null && step.state != GameUISelectableButton.State.Disabled) GameInput.Submit(step);
                },
                StateText = value,
                Details = () => GameNodes.Lines(TooltipReader.Lines(CovenantTooltips(covenant, selector.enabledDlcs))),
            };
        }

        /// <summary>The highest Covenant rank won with the chosen clan pair, as the game words it around
        /// the two clan icons it draws ("{0} Rank: 1", the icons read as the clans' names); what it
        /// means is its tooltip, in the buffer.</summary>
        public static NodeVtable ClanPairRank(ClanCovenantRankUI rank, Func<ClassData> main, Func<ClassData> sub)
        {
            Func<string> text = () =>
            {
                var mainClan = main();
                var subClan = sub();
                if (mainClan == null || subClan == null) return null;
                string line = (rank.preIconLabel != null ? rank.preIconLabel.Content() : "") + mainClan.GetTitle() + " " + subClan.GetTitle()
                    + (rank.postIconLabel != null ? rank.postIconLabel.Content() : "");
                if (rank.covenantLevelRoot != null && rank.covenantLevelRoot.activeInHierarchy) line += " " + Text(rank.covenantLevelLabel);
                return System.Text.RegularExpressions.Regex.Replace(line, @"\s+", " ").Trim();
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(text) },
                SearchText = text,
                Details = () => GameNodes.Lines(TooltipReader.Lines(rank)),
            };
        }

        /// <summary>The Covenant rank's tooltip as the game builds it (<c>ChallengeCovenantUI.AddCovenantTooltip</c>):
        /// the widget's own where it carries a provider; the run setup's carries none, so the same title
        /// and effects come from the same display data calls.</summary>
        public static List<TooltipContent> CovenantTooltips(ChallengeCovenantUI covenant, IReadOnlyList<DLC> dlcs)
        {
            var provider = covenant.TooltipProvider;
            if (provider != null && provider.Tooltips != null && provider.Tooltips.Count > 0) return new List<TooltipContent>(provider.Tooltips);
            var data = covenant.allGameData;
            var display = data != null ? data.GetChallengeCovenantDisplayData() : null;
            if (display == null) return new List<TooltipContent>();
            int level = covenant.CurrentLevel;
            string title = level > 0 ? display.GetChallengeLevelString(level) : GameText.Term(covenant.noCovenantTooltipText);
            string body = display.GetTooltipContent(data.GetAllCovenantsForLevel(level), dlcs ?? new List<DLC>());
            return new List<TooltipContent> { new TooltipContent(title, body) };
        }

        /// <summary>"Highly Reactive", then what it does: as drawn, else (a challenge's details draw only
        /// the icon) the mutator's own name and description.</summary>
        public static NodeVtable Mutator(MutatorButtonUI mutator)
        {
            var vt = Line(() => Text(mutator.titleLabel), null, mutator);
            Func<string> name = () => Text(mutator.titleLabel) ?? mutator.MutatorState?.GetName();
            return new NodeVtable
            {
                ControlType = vt.ControlType,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.TooltipPart(() => Text(mutator.descriptionLabel) ?? mutator.MutatorState?.GetDescription()),
                },
                SearchText = name,
                Details = vt.Details,
            };
        }

        /// <summary>A mutator in the run setup's picker: "Highly Reactive, toggle, on", then what it
        /// does. Enter chooses or drops it through the game (past the limit the game drops the
        /// earliest chosen).</summary>
        public static NodeVtable MutatorChoice(MutatorButtonUI mutator)
        {
            Func<string> name = () => Text(mutator.titleLabel);
            Func<string> state = () => mutator.Chosen ? Strings.StateOn : Strings.StateOff;
            return new NodeVtable
            {
                ControlType = ControlTypes.Toggle,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(state, live: true, kind: AnnouncementKinds.Value),
                    GameNodes.TooltipPart(() => Text(mutator.descriptionLabel)),
                },
                SearchText = name,
                OnActivate = () => GameInput.Submit(mutator.Button),
                StateText = state,
            };
        }

        /// <summary>A pyre heart in the run setup's picker: its name, "selected" on the one in use, the
        /// game's "Locked" with its unlock condition and progress, else its attack and health and what
        /// it does (the description the game shows for it). Enter picks it through the game, which
        /// closes the picker; a locked one stays unpicked.</summary>
        public static NodeVtable PyreHeartOption(RunSetupPyreHeartSelectionItemUI item, Func<string> randomDescription)
        {
            Func<string> name = () => Text(item.pyreHeartNameLabel);
            Func<string> description = () =>
            {
                var option = item.PyreHeartOptionData;
                if (option == null) return null;
                if (item.IsLocked) return ProgressionReader.Line(item.progressionObjectiveUI);
                if (option.IsRandom) return randomDescription();
                return RunSetupPyreHeartSelectionUI.GetCurrentDescription(option.pyreCharacterData).message;
            };
            var vt = GameWidgets.Button(item.Button, name, description,
                GameNodes.SelectedPart(() => item.selectedIndicator != null && item.selectedIndicator.activeSelf),
                new NodeAnnouncement(() => item.IsLocked ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
                new NodeAnnouncement(() => Commas(
                    Stat(Strings.PyreAttack, AttackOf(item.abilityUI)),
                    item.healthUIGameObject != null && item.healthUIGameObject.activeInHierarchy ? Stat(Strings.PyreHealth, Text(item.healthUILabel)) : null),
                    kind: AnnouncementKinds.Value));
            return vt;
        }

        // A read-only line: a label, a value, and the widget's tooltips as its details.
        private static NodeVtable Line(Func<string> label, Func<string> value, Component widget)
        {
            var parts = new List<NodeAnnouncement> { GameNodes.LabelPart(label) };
            if (value != null) parts.Add(new NodeAnnouncement(value, kind: AnnouncementKinds.Value));
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = parts,
                SearchText = label,
                Details = () => GameNodes.Lines(TooltipReader.Lines(widget)),
            };
        }

        internal static string CovenantTitle(ChallengeCovenantUI covenant, IReadOnlyList<DLC> dlcs)
        {
            var tips = CovenantTooltips(covenant, dlcs);
            return tips.Count > 0 && !string.IsNullOrWhiteSpace(tips[0].title) ? CardTextHelper.RemoveBold(tips[0].title) : null;
        }

        // The "Text Header" the game labels these widgets with ("Primary Clan", "Pyre Heart"): no field
        // holds it.
        internal static string HeaderOf(Component widget)
        {
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
                if (text.gameObject.name == "Text Header" && !string.IsNullOrWhiteSpace(text.Content())) return text.Content();
            return null;
        }

        // The attack number of a pyre heart's ability display.
        private static string AttackOf(PrimaryAbilityDisplay ability)
        {
            if (ability == null || !ability.gameObject.activeInHierarchy) return null;
            return Text(ability.GetComponentInChildren<TMP_Text>(false));
        }

        private static string Stat(Func<string, string> format, string value)
            => string.IsNullOrWhiteSpace(value) ? null : format(value);

        private static string Commas(params string[] parts)
        {
            var kept = new List<string>();
            foreach (var part in parts) if (!string.IsNullOrWhiteSpace(part)) kept.Add(part);
            return kept.Count > 0 ? string.Join(", ", kept) : null;
        }

        private static string Text(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;
    }
}
