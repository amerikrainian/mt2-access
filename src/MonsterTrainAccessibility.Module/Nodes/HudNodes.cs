using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;
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
    /// The run's HUD widgets as lines, in the game's words: a counter by the game's own title for it
    /// and the number it draws ("Pyre Health, 50 / 50", "Coin, 50"); a stat of the train by what its
    /// tooltip says it is and its number; an artifact by its tooltips. Every line keeps the widget's
    /// tooltips in the buffer. A widget the game lets the player press (the deck, the hoard, the game
    /// speed) is a button pressed through the game.
    /// </summary>
    internal static class HudNodes
    {
        /// <summary>"Coin, 50": a counter by the game's title term and its drawn number; a button when
        /// pressing it does something.</summary>
        public static NodeVtable Count(Component widget, string titleTerm, IGameUIComponent press = null)
        {
            // Some titles draw the counter's icon before its name ("[hoard icon] Dragon's Hoard").
            Func<string> label = () => GameSprites.WithoutEchoedIcons(GameText.Term(titleTerm))?.Trim();
            var value = new NodeAnnouncement(() => Drawn(widget), kind: AnnouncementKinds.Value);
            NodeVtable vt;
            if (press is GameUISelectableButton button) vt = GameWidgets.Button(button, label, null, value);
            else
            {
                vt = new NodeVtable
                {
                    ControlType = ControlTypes.Text,
                    Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(label), value },
                    SearchText = label,
                };
                if (press != null) vt.OnActivate = () => GameInput.Submit(press);
            }
            vt.Details = () => GameNodes.Lines(TooltipReader.Lines(widget));
            return vt;
        }

        /// <summary>"Forge Points, toggle, 2, off": the battle's forge points and whether the next card
        /// played spends them. Enter flips it through the game, as its key does; the new state is
        /// spoken by <see cref="Combat.ForgeToggle"/>, which hears both.</summary>
        public static NodeVtable ForgePoints(ForgePointsUI forge)
        {
            Func<string> label = () => GameText.Term("HudTooltip_ForgePoints_Title")?.Trim();
            return new NodeVtable
            {
                ControlType = ControlTypes.Toggle,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(() => Drawn(forge), kind: AnnouncementKinds.Value),
                    new NodeAnnouncement(() => Combat.ForgeToggle.State(), kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                OnActivate = () => GameInput.Submit(forge.forgeButton),
                Details = () => GameNodes.Lines(TooltipReader.Lines(forge)),
            };
        }

        /// <summary>"Game Speed, button, Current speed: Ultra": a toggle the game names in its tooltip.</summary>
        public static NodeVtable Tooltipped(Component widget, IGameUIComponent press)
        {
            Func<string> label = () => TooltipReader.FirstTitle(widget);
            var vt = new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(() => TooltipReader.FirstBody(widget), live: true, kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                Details = () => GameNodes.Lines(TooltipReader.Lines(widget)),
            };
            if (press != null) vt.OnActivate = () => GameInput.Submit(press);
            vt.StateText = () => TooltipReader.FirstBody(widget);
            return vt;
        }

        /// <summary>A train stat: "Ember gained at the start of each turn: 3", its tooltip and the number
        /// the game puts beside its icon.</summary>
        public static NodeVtable TrainStat(TMP_Text number)
        {
            Func<string> label = () =>
            {
                var stat = number.transform.parent;
                string what = TooltipReader.FirstBody(stat) ?? TooltipReader.FirstTitle(stat);
                return what != null ? what.TrimEnd('.', ' ') : null;
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(() => number.Content(), kind: AnnouncementKinds.Value),
                },
                SearchText = label,
            };
        }

        /// <summary>An artifact, a mutator, an enemy's artifact: by its own tooltips, read off the icon
        /// whether or not its page of the collection is on screen.</summary>
        public static NodeVtable Relic(RelicTooltipProvider icon)
            => TooltipReader.Item(() => TooltipReader.Lines(icon.Tooltips ?? new List<TooltipContent>()));

        /// <summary>
        /// A Soul Savior run's soul (<see cref="SoulHudItemUI"/>): "Ghastbud, button, Attached To: Greed
        /// Dragon", then what it does; where it stands is the game's own tooltip lines after the soul's
        /// ("Attached To: ...", that no card can take it), said again as it changes. The game's "Soul Not
        /// Attached" is left out: no card named says as much.
        /// Enter is a click, through the game: on the map it attaches the soul (the deck opens to choose
        /// its card), in a battle it shows the deck while attached, else the game refuses; a press that
        /// opens the deck calls <paramref name="leave"/> first. Backspace detaches it where the game offers
        /// that (an attached soul, on the map), as the game's own detach does.
        /// </summary>
        public static NodeVtable Soul(SoulHudItemUI item, SoulEquipUI equip, Action leave)
        {
            var button = item.SoulItemButton;
            Func<string> name = () => item.GetSoulState()?.GetName();
            var vt = new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => Standing(item), live: true, kind: AnnouncementKinds.Value),
                    GameNodes.TooltipPart(() => TooltipReader.FirstBody(item.tooltipProvider)),
                },
                SearchText = name,
                OnFocus = () => GameInput.Hover(button),
                OnActivate = () =>
                {
                    var action = PressAction(item, equip);
                    if (action == SoulHudItemUI.SoulActionType.Attach || action == SoulHudItemUI.SoulActionType.ViewDeck) leave?.Invoke();
                    GameInput.Submit(button);
                },
            };
            if (CanDetach(item, equip))
                vt.OnSecondary = () =>
                {
                    var state = item.GetSoulState();
                    var save = AllGameManagers.Instance != null ? AllGameManagers.Instance.GetSaveManager() : null;
                    if (state == null || save == null) return;
                    SoulHelper.DetachSoulEffect(state, save);
                    equip.OnSoulDetached.Dispatch(item);
                };
            return vt;
        }

        // The soul's tooltip lines after its own, less "Soul Not Attached": where it stands.
        private static string Standing(SoulHudItemUI item)
        {
            var lines = TooltipReader.Lines(item.tooltipProvider);
            string unattached = string.IsNullOrEmpty(item.notEquippedKey) ? null : TextFilter.Clean(GameText.Term(item.notEquippedKey));
            var kept = new List<string>();
            for (int i = 1; i < lines.Count; i++)
                if (TextFilter.Clean(lines[i]) != unattached) kept.Add(lines[i]);
            return kept.Count > 0 ? GameNodes.Sentences(kept) : null;
        }

        // What a click on the soul would do now, by the game's own test (which changes nothing).
        private static SoulHudItemUI.SoulActionType PressAction(SoulHudItemUI item, SoulEquipUI equip)
        {
            var save = AllGameManagers.Instance != null ? AllGameManagers.Instance.GetSaveManager() : null;
            if (save == null || item.SoulItemButton == null) return SoulHudItemUI.SoulActionType.None;
            return item.TryGetAction(item.SoulItemButton, global::InputManager.Controls.Submit, save, CanEquip(equip, save), out var action)
                ? action : SoulHudItemUI.SoulActionType.None;
        }

        // The game offers the detach (its preview key, a pad's context key) for an attached soul that
        // could be attached again, where it shows the equip prompts (the map).
        private static bool CanDetach(SoulHudItemUI item, SoulEquipUI equip)
        {
            var save = AllGameManagers.Instance != null ? AllGameManagers.Instance.GetSaveManager() : null;
            var state = item.GetSoulState();
            return save != null && state != null && CanEquip(equip, save)
                && SoulHelper.GetIsSoulEffectAttached(state, save, out _) && SoulHelper.CanPlayerAttachSoulEffect(state, save);
        }

        private static bool CanEquip(SoulEquipUI equip, SaveManager save)
            => equip.ShouldShowEquipUI(save.GetGameSequence(), AllGameManagers.Instance.GetScreenManager());

        // The texts a widget draws, in order: its number, however it is laid out. A current-over-maximum
        // count is two lines with the divider drawn between them ("50", "50"), read as "50 / 50".
        private static string Drawn(Component widget)
        {
            var texts = new List<string>();
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.Content()) || text.GetComponentInParent<GameUIControlMapping>() != null) continue;
                foreach (var line in text.Content().Split('\n'))
                    if (!string.IsNullOrWhiteSpace(line)) texts.Add(line.Trim());
            }
            return texts.Count > 0 ? string.Join(" / ", texts) : null;
        }
    }
}
