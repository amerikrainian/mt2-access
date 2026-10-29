using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// Clans and their champions as the game shows them outside a run:
    /// <list type="bullet">
    /// <item>a clan choice (<see cref="ClanOptionButton"/>, an icon) is a radio button by the clan's
    /// name, "Locked" with the game's reason while its DLC is missing;</item>
    /// <item>a champion choice (<see cref="ChampionOptionButton"/>) is a radio button by the name the
    /// game draws, the game's "Locked" alone while it hides a locked champion's name;</item>
    /// <item>a clan's progress (<see cref="ClanChecklistSection"/>): its name and level, or its unlock
    /// condition and count; its champions, "mastered" where the game frames the portrait; one line per
    /// clan pairing victory and the card mastery meter, each by its own tooltip.</item>
    /// </list>
    /// A choice is made by landing on it, through the game's own press.
    /// </summary>
    internal static class ClanNodes
    {
        public static NodeVtable ClanOption(ClanOptionButton clan, Func<bool> selected)
        {
            Func<string> name = () => clan.ClassData != null ? clan.ClassData.GetTitle() : null;
            Action select = () => { if (clan.dlcInstalled && !selected()) GameInput.Submit(clan.Button); };
            return new NodeVtable
            {
                ControlType = ControlTypes.RadioButton,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.SelectedPart(selected),
                    new NodeAnnouncement(() => clan.dlcInstalled ? null : GameText.Term("Locked"), kind: AnnouncementKinds.Enabled),
                    GameNodes.TooltipPart(() => clan.dlcInstalled ? null : TooltipBody(clan.tooltipProvider)),
                },
                SearchText = name,
                OnFocus = select,
                OnActivate = select,
            };
        }

        public static NodeVtable ChampionOption(ChampionOptionButton champion)
        {
            Func<string> name = () => champion.unlocked ? Shown(champion.championName) : GameText.Term("Locked");
            Func<bool> selected = () => ChampionSelected(champion);
            Action select = () => { if (champion.unlocked && !selected()) GameInput.Submit(champion.Button); };
            return new NodeVtable
            {
                ControlType = ControlTypes.RadioButton,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(name), GameNodes.SelectedPart(selected) },
                SearchText = name,
                OnFocus = select,
                OnActivate = select,
            };
        }

        /// <summary>A clan in the run setup's picker (<see cref="RunSetupClanSelectionItemUI"/>): its name
        /// (or the game's Random title), "selected" on the one in use, the game's "Locked" with its
        /// unlock condition and progress, else its level ("Level: 9", then the meter's "120/300" or
        /// its max-level word) and the description the picker shows for it; the level meter's tooltips
        /// (what the next level unlocks) in the buffer. Enter picks it through the game, which closes
        /// the picker; a locked one stays unpicked.</summary>
        public static NodeVtable RunSetupOption(RunSetupClanSelectionItemUI item, Func<string> description)
        {
            Func<string> name = () => Shown(item.classTitleLabel) ?? Shown(item.classRandomTitleLabel);
            var vt = GameWidgets.Button(item.Button, name,
                () => item.IsLocked ? ProgressionReader.Line(item.progressionObjectiveUI) : description(),
                GameNodes.SelectedPart(() => (item.selectedIndicator != null && item.selectedIndicator.activeSelf)
                    || (item.randomSelectedIndicator != null && item.randomSelectedIndicator.activeSelf)),
                new NodeAnnouncement(() => item.IsLocked ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
                new NodeAnnouncement(() => item.IsLocked ? null : Level(item.classLevelMeter), kind: AnnouncementKinds.Value));
            vt.Details = () => GameNodes.Lines(TooltipReader.Lines(item.classLevelMeter));
            return vt;
        }

        // "Level: 9, 120/300": the meter's level, then its count or its message ("Max Level").
        private static string Level(ClassLevelMeterUI meter)
        {
            if (meter == null || !meter.gameObject.activeInHierarchy) return null;
            string level = Shown(meter.levelLabel);
            if (level == null) return null;
            string line = string.Format(GameText.Term("TextFormat_Colon"), GameText.Term("Leaderboard_Level"), level);
            // The game sets the count or the message (on its own object) whether or not the panel
            // holding them is on screen: it shows on hover.
            var bar = meter.xpMeter;
            string progress = bar != null ? Set(bar.countLabel) ?? Set(bar.messageLabel) : null;
            return progress != null ? line + ", " + progress : line;
        }

        /// <summary>The champion the game shows chosen.</summary>
        public static bool ChampionSelected(ChampionOptionButton champion)
            => champion.Button.state == GameUISelectableButton.State.Activated;

        /// <summary>One clan's progress, as items under the clan's name.</summary>
        public static void Progress(GraphBuilder b, string key, ClanChecklistSection clan, SaveManager save)
        {
            if (clan == null || !clan.gameObject.activeInHierarchy || clan.classData == null) return;
            var data = clan.classData;
            b.PushContext(Shown(clan.clanNameLabel), Strings.RoleList);
            b.AddItem(ControlId.Structural(key + ":name"), new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(() => Shown(clan.clanNameLabel)),
                    new NodeAnnouncement(() => Standing(clan), kind: AnnouncementKinds.Value),
                },
                SearchText = () => Shown(clan.clanNameLabel),
            });
            for (int i = 0; i < clan.championPortraits.Count; i++)
            {
                var portrait = clan.championPortraits[i];
                if (portrait == null || !portrait.gameObject.activeInHierarchy) continue;
                int index = i;
                Func<string> name = () => save.IsChampionUnlockedWhenStartingRun(data, index, shouldIgnoreActiveRunType: true)
                    ? data.GetChampionCard(index)?.GetName() : GameText.Term("Locked");
                b.AddItem(ControlId.Structural(key + ":champion:" + i), new NodeVtable
                {
                    ControlType = ControlTypes.Text,
                    Announcements = new List<NodeAnnouncement>
                    {
                        GameNodes.LabelPart(name),
                        new NodeAnnouncement(() => portrait.masteryFrame != null && portrait.masteryFrame.gameObject.activeSelf ? Strings.StateMastered : null,
                            kind: AnnouncementKinds.Value),
                    },
                    SearchText = name,
                });
            }
            int v = 0;
            foreach (var victory in clan.victoryItems)
            {
                if (victory == null || !victory.gameObject.activeInHierarchy) continue;
                b.AddItem(ControlId.Structural(key + ":victory:" + v++), TooltipReader.Item(victory));
            }
            if (clan.cardMasteryMeter != null && clan.cardMasteryMeter.gameObject.activeInHierarchy)
                b.AddItem(ControlId.Structural(key + ":mastery"), TooltipReader.Item(clan.cardMasteryMeter));
            b.PopContext();
        }

        // "Level 10" (the drawn level), or the unlock condition and its count ("3/5").
        private static string Standing(ClanChecklistSection clan)
        {
            if (clan.levelIndicator != null && clan.levelIndicator.gameObject.activeInHierarchy)
                return GameNodes.Sentences(TextsOf(clan.levelIndicator));
            if (clan.unlockConditionsRoot != null && clan.unlockConditionsRoot.activeInHierarchy)
            {
                string meter = clan.unlockConditionsMeter != null && clan.unlockConditionsMeter.gameObject.activeInHierarchy
                    ? Shown(clan.unlockConditionsMeter.countLabel) ?? Shown(clan.unlockConditionsMeter.messageLabel) : null;
                return GameNodes.Sentences(Shown(clan.unlockConditionsLabel), meter);
            }
            return null;
        }

        // A widget's shown texts in order ("Level", "10" reads "Level: 10").
        private static IEnumerable<string> TextsOf(UnityEngine.Component widget)
        {
            var texts = new List<string>();
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
            {
                string t = Shown(text);
                if (t != null) texts.Add(t);
            }
            if (texts.Count == 2) return new[] { texts[0] + ": " + texts[1] };
            return texts;
        }

        private static string TooltipBody(ITooltipProvider provider)
        {
            var tips = provider?.Tooltips;
            return tips != null && tips.Count > 0 && !string.IsNullOrWhiteSpace(tips[0].body) ? CardTextHelper.ScrubUpgradeHighlights(tips[0].body) : null;
        }

        private static string Set(TMP_Text text)
            => text != null && text.gameObject.activeSelf && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;

        private static string Shown(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;
    }
}
