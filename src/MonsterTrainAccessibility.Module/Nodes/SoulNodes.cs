using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Contracts;
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
    /// A soul, in the Soulforge (<see cref="SoulSelectionItemUI"/>) or chosen for a Soul Savior run
    /// (<see cref="RunSetupSoulSelectionItemUI"/>): its name as the item draws it, the game's "Locked"
    /// while it is, for a run a toggle "on" while chosen and "off" while not (as a mutator is; said again
    /// as Enter changes it), its tier while the item marks one ("Tier 2"; the
    /// first tier carries no mark), then its description and the progress the game shows for it (the
    /// unlock, or the next tier: "Eat units to unlock. 0/15", <see cref="ProgressionReader"/>). Landing
    /// hovers it, as the mouse does, which is what fills the game's soul panel; for a run, Enter chooses
    /// or drops it through the game. Its buffer holds its tooltips, a soul being an artifact to the game
    /// (<c>TooltipGenerator.GetRelicTooltips</c>).
    /// </summary>
    internal static class SoulNodes
    {
        public static NodeVtable Soul(SoulSelectionItemUI item, global::SoulforgeScreen screen)
            => Build(item.soulNameLabel, () => item.SoulState, () => item.IsLocked, null, item.Button,
                screen.soulNameLabel, screen.progressionObjectiveUI, choose: false);

        public static NodeVtable Choice(RunSetupSoulSelectionItemUI item, SoulSelectionDialog dialog)
            => Build(item.soulNameLabel, () => item.SoulState, () => item.IsLocked, () => item.Chosen, item.Button,
                dialog.soulNameLabel, dialog.progressionObjectiveUI, choose: true);

        /// <summary>A soul a run carries, as a panel lists it (<see cref="SoulInfoUI"/>: the run's
        /// opening): its name and description as drawn; its tooltips in the buffer, less the soul's own
        /// (the panel asks for it first, and it says what the description does).</summary>
        public static NodeVtable Info(SoulInfoUI soul)
        {
            Func<string> name = () => soul.soulNameText != null && !string.IsNullOrWhiteSpace(soul.soulNameText.Content())
                ? soul.soulNameText.Content() : soul.GetSoulData()?.GetName();
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.TooltipPart(() => soul.soulDescriptionText != null && soul.soulDescriptionText.gameObject.activeInHierarchy
                        ? soul.soulDescriptionText.Content() : null),
                },
                SearchText = name,
                Details = () =>
                {
                    var tips = new List<TooltipContent>();
                    if (soul.Tooltips != null)
                        foreach (var tip in soul.Tooltips)
                            if (TextFilter.Clean(tip.title) != TextFilter.Clean(name())) tips.Add(tip);
                    return GameNodes.Lines(TooltipReader.Lines(tips));
                },
            };
        }

        private static NodeVtable Build(TMP_Text label, Func<SoulState> state, Func<bool> locked, Func<bool> chosen,
            GameUISelectableButton button, TMP_Text panelName, ProgressionObjectiveUI panelProgress, bool choose)
        {
            Func<string> name = () => Name(label, state());
            Action hover = () => GameInput.Hover(button);
            var parts = new List<NodeAnnouncement>
            {
                GameNodes.LabelPart(name),
                new NodeAnnouncement(() => locked() ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
            };
            Func<string> onOff = chosen != null ? () => chosen() ? Strings.StateOn : Strings.StateOff : (Func<string>)null;
            if (onOff != null) parts.Add(new NodeAnnouncement(onOff, live: true, kind: AnnouncementKinds.Value));
            parts.Add(new NodeAnnouncement(() => Tier(state()), kind: AnnouncementKinds.Value));
            parts.Add(GameNodes.TooltipPart(() => state()?.GetSoulData()?.GetDescription()));
            parts.Add(GameNodes.TooltipPart(() => Progress(name(), panelName, panelProgress)));
            return new NodeVtable
            {
                ControlType = choose ? ControlTypes.Toggle : ControlTypes.Item,
                Announcements = parts,
                StateText = onOff,
                SearchText = name,
                OnFocus = hover,
                OnActivate = choose ? (Action)(() => GameInput.Submit(button)) : hover,
                Details = () => GameNodes.Lines(TooltipReader.Lines(Tooltips(state()))),
            };
        }

        private static string Name(TMP_Text label, SoulState state)
        {
            string drawn = label != null ? label.Content() : null;
            return !string.IsNullOrWhiteSpace(drawn) ? drawn : state?.GetName();
        }

        // The item marks the second and third tiers only.
        private static string Tier(SoulState state)
        {
            int tier = state?.GetSoulData() != null ? state.GetSoulData().GetTierLevel() : 0;
            return tier > 1 ? string.Format(GameText.Term("SoulTier_Format"), tier) : null;
        }

        // The game's panel says what unlocks the hovered soul, or its next tier; read while it shows this one.
        private static string Progress(string name, TMP_Text panelName, ProgressionObjectiveUI panel)
        {
            if (panel == null || !panel.gameObject.activeSelf || panelName == null || panelName.Content() != name) return null;
            return ProgressionReader.Line(panel);
        }

        private static List<TooltipContent> Tooltips(SoulState state)
        {
            var tooltips = new List<TooltipContent>();
            if (state == null) return tooltips;
            var managers = AllGameManagers.Instance;
            TooltipGenerator.GetRelicTooltips(tooltips, state, managers?.GetStatusEffectManager(), managers?.GetRelicManager(),
                TooltipGenerator.OptionalFlags.Lore, managers?.GetSaveManager());
            return tooltips;
        }
    }
}
