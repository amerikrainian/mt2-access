using System;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;
using ShinyShoe;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// What the two run setups share (the base one, <see cref="global::RunSetupScreen"/>, and Soul
    /// Savior's, <see cref="global::SoulSaviorRunSetupScreen"/>: the same clan and pyre heart widgets,
    /// pickers and buttons), laid out once in <see cref="Layout"/> so a change reaches both. The one
    /// list, top to bottom as drawn: each clan as a button by its header ("Primary Clan, Pyreborne", the
    /// description the game shows on hover, its header's tooltip in the buffer; Enter opens the clan
    /// picker), its champion (Swap Champion with the champion's name, Enter swapping through the game;
    /// the name alone while the other is locked) and the starter card the game previews for it; the pyre
    /// heart (Enter opens its picker); then the mode's own part (the Covenant rank, or the difficulty)
    /// and its extras (the mutators, or the souls); Depart; Settings and Back.
    /// </summary>
    internal static class RunSetupNodes
    {
        /// <summary>A run setup's parts, from whichever screen it is.</summary>
        public sealed class Parts
        {
            public RunSetupClassLevelInfoUI Main, Sub;
            public PyreHeartInfoUI PyreHeart;
            public GameUISelectableButton PyreHeartButton, Start, Settings, Back;
            public Action<GraphBuilder> Mode, Extras;
        }

        public static global::RunSetupScreen Base => GameScreens.Active<global::RunSetupScreen>(global::ScreenName.RunSetup);

        public static global::SoulSaviorRunSetupScreen SoulSavior
            => GameScreens.Active<global::SoulSaviorRunSetupScreen>(global::ScreenName.SoulSaviorRunSetup);

        /// <summary>The open setup's clan picker, and the clan it is choosing for.</summary>
        public static RunSetupClanSelectionUI ClanPicker(out RunSetupClassLevelInfoUI editing)
        {
            var base_ = Base;
            var soul = SoulSavior;
            var picker = base_ != null ? base_.clanSelectionDialog : soul != null ? soul.clanSelectionDialog : null;
            editing = null;
            if (picker == null) return null;
            editing = picker.isMainClass ? (base_ != null ? base_.mainClassInfo : soul.mainClassInfo) : (base_ != null ? base_.subClassInfo : soul.subClassInfo);
            return picker;
        }

        /// <summary>The open setup's pyre heart picker, and its pyre heart panel.</summary>
        public static RunSetupPyreHeartSelectionUI PyreHeartPicker(out PyreHeartInfoUI pyre)
        {
            var base_ = Base;
            var soul = SoulSavior;
            pyre = base_ != null ? base_.pyreHeartInfo : soul != null ? soul.pyreHeartInfo : null;
            return base_ != null ? base_.pyreHeartSelectionDialog : soul != null ? soul.pyreHeartSelectionDialog : null;
        }

        public static void Layout(GraphBuilder b, Parts parts)
        {
            // The panel draws no title: no context word to speak.
            b.PushContext(null);
            Clan(b, "main", parts.Main);
            Clan(b, "sub", parts.Sub);
            PyreHeart(b, parts.PyreHeart, parts.PyreHeartButton);
            parts.Mode?.Invoke(b);
            parts.Extras?.Invoke(b);
            Button(b, "depart", parts.Start, () => Screens.PauseMenuScreen.Caption(parts.Start));
            Button(b, "settings", parts.Settings, () => GameText.Term("ScreenMainMenu_Settings"));
            Button(b, "back", parts.Back, () => Screens.PauseMenuScreen.Caption(parts.Back));
            b.SetStart(ControlId.Structural("setup:clan:main"));
            b.PopContext();
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
            Func<string> name = () =>
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
        private static void PyreHeart(GraphBuilder b, PyreHeartInfoUI pyre, GameUISelectableButton button)
        {
            if (pyre == null || button == null || !button.gameObject.activeInHierarchy) return;
            var vt = GameWidgets.Button(button, () => StartingConditionsNodes.HeaderOf(pyre), () => TooltipBody(button),
                new NodeAnnouncement(() => StartingConditionsNodes.PyreHeartValue(pyre), kind: AnnouncementKinds.Value));
            vt.Details = () => GameNodes.Lines(TooltipReader.Lines(button));
            b.AddItem(ControlId.Structural("setup:pyre"), vt);
        }

        public static void WinStreak(GraphBuilder b, string key, WinStreakUI streak)
        {
            if (streak == null || !streak.gameObject.activeInHierarchy || TooltipReader.Lines(streak).Count == 0) return;
            b.AddItem(ControlId.Structural("setup:" + key), TooltipReader.Item(streak));
        }

        public static void Button(GraphBuilder b, string key, GameUISelectableButton button, Func<string> label)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            b.AddItem(ControlId.Structural("setup:" + key), GameWidgets.Button(button, label));
        }

        /// <summary>The body of a widget's first tooltip (the pyre heart's effect), as the game shows it.</summary>
        public static string TooltipBody(Component widget)
        {
            var provider = widget.GetComponent<ITooltipProvider>();
            var tips = provider != null ? provider.Tooltips : null;
            return tips != null && tips.Count > 0 && !string.IsNullOrWhiteSpace(tips[0].body)
                ? CardTextHelper.ScrubUpgradeHighlights(tips[0].body) : null;
        }

        public static string Text(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content() : null;
    }
}
