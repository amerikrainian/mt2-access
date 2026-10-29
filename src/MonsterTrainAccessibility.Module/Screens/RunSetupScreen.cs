using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.Nodes;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The run setup (<see cref="global::RunSetupScreen"/>), one list top to bottom as drawn: each clan
    /// as a button by its header ("Primary Clan, Pyreborne", the description the game shows on hover,
    /// its header's tooltip in the buffer; Enter opens <see cref="RunSetupClanPickerScreen"/>), its
    /// champion (Swap Champion with the champion's name, Enter swapping through the game; the name
    /// alone while the other is locked) and the starter card the game previews for it; the pyre heart
    /// (Enter opens <see cref="RunSetupPyreHeartPickerScreen"/>); the Covenant rank as a slider stepped
    /// through the game's level buttons; the clan pair's best rank or the game's no-progression note;
    /// the win streaks; the chosen mutators (Enter opens <see cref="RunSetupMutatorsScreen"/>); Add or
    /// Remove Mutators; Depart; then Settings and Back.
    /// Escape is the game's: it opens Settings, as on the Covenant Outpost.
    /// </summary>
    public sealed class RunSetupScreen : Screen
    {
        public override string Key => "game.run_setup";
        public override bool KeepStateOnPop => true;

        internal static global::RunSetupScreen Game
            => GameScreens.Active<global::RunSetupScreen>(global::ScreenName.RunSetup);

        public override bool IsActive() => Game != null;

        public override void Build(GraphBuilder b)
        {
            var screen = Game;
            if (screen == null || screen.mainClassInfo == null) return;
            // The panel draws no title: no context word to speak.
            b.PushContext(null);

            Clan(b, "main", screen.mainClassInfo);
            Clan(b, "sub", screen.subClassInfo);
            PyreHeart(b, screen);
            Covenant(b, screen);
            Mutators(b, screen);
            Button(b, "mutators", screen.toggleEnableMutatorsButton, () => Text(screen.toggleEnableMutatorsButtonLabel));
            Button(b, "depart", screen.startButton, () => PauseMenuScreen.Caption(screen.startButton));
            Button(b, "settings", screen.settingsButton, () => GameText.Term("ScreenMainMenu_Settings"));
            Button(b, "back", screen.backButton, () => PauseMenuScreen.Caption(screen.backButton));
            b.SetStart(ControlId.Structural("setup:clan:main"));
            b.PopContext();
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, GameText.Term("ScreenMainMenu_Settings"),
                _ => GameInput.Send(global::InputManager.Controls.Escape));
        }

        // A clan: its button, its champion, then the starter card the game previews for it.
        private static void Clan(GraphBuilder b, string key, RunSetupClassLevelInfoUI info)
        {
            if (info == null || !info.gameObject.activeInHierarchy) return;
            var button = info.editClassButton;
            if (button != null && button.gameObject.activeInHierarchy)
            {
                var vt = GameWidgets.Button(button, () => StartingConditionsNodes.HeaderOf(info), () => Description(info),
                    new NodeAnnouncement(() => Text(info.titleLabel), kind: AnnouncementKinds.Value));
                vt.Details = () => GameNodes.Lines(TooltipReader.Lines(button));
                b.AddItem(ControlId.Structural("setup:clan:" + key), vt);
            }
            Champion(b, key, info);
            var preview = info.externalCardPreviewTooltip;
            if (preview != null && preview.gameObject.activeInHierarchy && !info.IsRandom)
            {
                foreach (var card in preview.GetComponentsInChildren<CardUI>(false))
                {
                    var details = card.GetComponentInParent<RewardDetailsUI>();
                    var lore = details != null ? details.loreTooltipSetting : CardTooltipContainer.LoreTooltipSetting.Always;
                    b.AddItem(ControlId.Structural("setup:card:" + key), CardNodes.Card(card, lore, null));
                }
            }
        }

        // What the game shows on hovering the clan: the primary clan's description, or the allied one's.
        private static string Description(RunSetupClassLevelInfoUI info)
        {
            var clan = info.ClassData;
            if (clan == null) return null;
            return info.IsMainClass ? clan.GetDescription() : clan.GetSubclassDescription();
        }

        // The clan's champion: Swap Champion with the champion's name while the game offers the swap
        // (the name re-read on the swap), else "Champion: name".
        private static void Champion(GraphBuilder b, string key, RunSetupClassLevelInfoUI info)
        {
            if (info.ClassData == null) return;
            System.Func<string> name = () =>
            {
                var champion = info.ClassData != null ? info.ClassData.GetChampionData(info.ChampionIndex) : null;
                return champion != null && champion.championCardData != null ? champion.championCardData.GetName() : null;
            };
            var id = ControlId.Structural("setup:champion:" + key);
            var swap = info.swapChampionButton;
            if (swap != null && swap.gameObject.activeInHierarchy)
            {
                var vt = GameWidgets.Button(swap, () => GameText.Term("ScreenMainMenu_SwapChampion"), null,
                    new NodeAnnouncement(name, live: true, kind: AnnouncementKinds.Value));
                vt.StateText = name;
                b.AddItem(id, vt);
            }
            else if (name() != null)
            {
                b.AddItem(id, GameNodes.Text(() => string.Format(GameText.Term("TextFormat_Colon"), GameText.Term("ScreenClassSelection_ChampionLabel"), name())));
            }
        }

        // The pyre heart: "Pyre Heart, button, Herzal's Horde, Pyre attack 35, Pyre health 65", then what
        // it does; its tooltips in the buffer.
        private static void PyreHeart(GraphBuilder b, global::RunSetupScreen screen)
        {
            var pyre = screen.pyreHeartInfo;
            var button = screen.pyreHeartButton;
            if (pyre == null || button == null || !button.gameObject.activeInHierarchy) return;
            var vt = GameWidgets.Button(button, () => StartingConditionsNodes.HeaderOf(pyre), () => TooltipBody(button),
                new NodeAnnouncement(() => StartingConditionsNodes.PyreHeartValue(pyre), kind: AnnouncementKinds.Value));
            vt.Details = () => GameNodes.Lines(TooltipReader.Lines(button));
            b.AddItem(ControlId.Structural("setup:pyre"), vt);
        }

        // The Covenant rank slider, then what the game shows under it: the clan pair's best rank (or,
        // with mutators on, its note that the run won't count) and the win streaks.
        private static void Covenant(GraphBuilder b, global::RunSetupScreen screen)
        {
            var selector = screen.covenantSelectionUI;
            if (selector == null || !selector.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("setup:covenant"), StartingConditionsNodes.CovenantSelector(selector,
                () => ChallengeProgressScreen.HeaderOf(selector.transform.parent, "Text Covenant header")));
            var rank = screen.clanCovenantRankUI;
            if (rank != null && rank.gameObject.activeInHierarchy && rank.contentRoot != null && rank.contentRoot.activeSelf)
                b.AddItem(ControlId.Structural("setup:pairrank"), StartingConditionsNodes.ClanPairRank(rank,
                    () => screen.mainClassInfo.ClassData, () => screen.subClassInfo.ClassData));
            if (screen.covenantDisabledRoot != null && screen.covenantDisabledRoot.activeInHierarchy)
            {
                int i = 0;
                foreach (var text in screen.covenantDisabledRoot.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (string.IsNullOrWhiteSpace(text.Content())) continue;
                    var shown = text;
                    b.AddItem(ControlId.Structural("setup:noprogress:" + i++), GameNodes.Text(() => shown.Content()));
                }
            }
            WinStreak(b, "streak", screen.winStreakUI);
            WinStreak(b, "tfbstreak", screen.trueFinalBossWinStreakUI);
        }

        private static void WinStreak(GraphBuilder b, string key, WinStreakUI streak)
        {
            if (streak == null || !streak.gameObject.activeInHierarchy || TooltipReader.Lines(streak).Count == 0) return;
            b.AddItem(ControlId.Structural("setup:" + key), TooltipReader.Item(streak));
        }

        // The mutator row, one button over the whole row in the game: "Mutators, button, A Simple Plan,
        // Acid Rain", Enter opening the picker. Each chosen one's description is a buffer line, from the
        // preview the game keeps filled for hovering the row (the row's icons carry no tooltips here).
        private static void Mutators(GraphBuilder b, global::RunSetupScreen screen)
        {
            var edit = screen.editMutatorsButton;
            if (screen.mutatorOptionsParent == null || !screen.mutatorOptionsParent.gameObject.activeInHierarchy
                || edit == null || !edit.gameObject.activeInHierarchy) return;
            var vt = GameWidgets.Button(edit, () => StartingConditionsNodes.HeaderOf(screen.mutatorOptionsParent), null,
                new NodeAnnouncement(() =>
                {
                    var names = new List<string>();
                    foreach (var item in Chosen(screen)) names.Add(item.nameLabel.Content());
                    return names.Count > 0 ? string.Join(", ", names) : null;
                }, kind: AnnouncementKinds.Value));
            vt.Details = () =>
            {
                var lines = new List<string>();
                foreach (var item in Chosen(screen))
                {
                    string description = item.descriptionLabel != null ? item.descriptionLabel.Content() : null;
                    lines.Add(string.IsNullOrWhiteSpace(description) ? item.nameLabel.Content() : item.nameLabel.Content() + ": " + description);
                }
                return lines;
            };
            b.AddItem(ControlId.Structural("setup:editmutators"), vt);
        }

        // The chosen mutators' preview entries, in order.
        private static IEnumerable<MutatorPreviewItemUI> Chosen(global::RunSetupScreen screen)
        {
            if (screen.mutatorPreviewListRoot == null) yield break;
            foreach (var item in screen.mutatorPreviewListRoot.GetComponentsInChildren<MutatorPreviewItemUI>(true))
                if (item.gameObject.activeSelf && item.nameLabel != null && !string.IsNullOrWhiteSpace(item.nameLabel.Content())) yield return item;
        }

        private static void Button(GraphBuilder b, string key, GameUISelectableButton button, System.Func<string> label)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("setup:" + key), GameWidgets.Button(button, label));
        }

        // The body of a widget's first tooltip (the pyre heart's effect), as the game shows it.
        private static string TooltipBody(Component widget)
        {
            var provider = widget.GetComponent<ITooltipProvider>();
            var tips = provider != null ? provider.Tooltips : null;
            return tips != null && tips.Count > 0 && !string.IsNullOrWhiteSpace(tips[0].body)
                ? CardTextHelper.ScrubUpgradeHighlights(tips[0].body) : null;
        }

        private static string Text(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;
    }
}
