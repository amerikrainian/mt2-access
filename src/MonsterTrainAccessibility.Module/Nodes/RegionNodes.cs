using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// Soul Savior's region choice (<see cref="global::RegionSelectionScreen"/>), its parts in the game's
    /// words: a region by its boss's name as the button draws it ("Maera the Dutiful"), "defeated" once
    /// its region is cleared (the game draws that as an overlay), then the boss's story and power, its
    /// tooltips; the final boss the same, "Locked" with the game's line on what unlocks it; a reward by
    /// its tooltip ("Light Forge: Upgrade your Champion."), "upgraded" where a cleared region has improved
    /// it (the game shows that by its icon); the region's mutator by its name and effect. The bosses' tooltips are read whether or not the game shows
    /// them (it hides them while navigating by keyboard).
    /// </summary>
    internal static class RegionNodes
    {
        /// <summary>A region: landing on it selects it as the game's keyboard and pad navigation do, which
        /// shows its rewards; Enter does the same. A defeated region can no longer be selected.</summary>
        public static NodeVtable Region(RegionSelectionButton region, Action select)
        {
            Func<string> name = () => BossName(region);
            Action choose = () =>
            {
                if (region.Button != null && region.Button.CanBeActivated()) select();
            };
            return Boss(region, name, new List<NodeAnnouncement>
            {
                new NodeAnnouncement(() => region.defeatedIndicatorRoot != null && region.defeatedIndicatorRoot.activeSelf ? Strings.StateDefeated : null,
                    kind: AnnouncementKinds.Value),
            }, choose);
        }

        /// <summary>The final boss: "Locked" until every region is defeated, with the game's line on what
        /// unlocks it; then selected on landing as a region is.</summary>
        public static NodeVtable FinalBoss(RegionSelectionButton boss, TooltipProviderComponent locked, Action select)
        {
            Func<string> name = () => BossName(boss);
            Func<bool> isLocked = () => boss.Button == null || !boss.Button.interactable;
            Action choose = () =>
            {
                if (!isLocked() && boss.Button.CanBeActivated()) select();
            };
            return Boss(boss, name, new List<NodeAnnouncement>
            {
                new NodeAnnouncement(() => isLocked() ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
            }, choose, () => isLocked() && locked != null ? GameNodes.Sentences(TooltipReader.Lines(locked)) : null);
        }

        /// <summary>A region's reward by its tooltip, "upgraded" where the game has improved it.</summary>
        public static NodeVtable Reward(RegionSelectionRewardDisplay display, Func<MapNodeData> data)
        {
            var vt = TooltipReader.Item(display);
            vt.Announcements = new List<NodeAnnouncement>(vt.Announcements)
            {
                new NodeAnnouncement(() => data()?.GetIsSoulSaviorUpgradedNode() == true ? Strings.StateUpgraded : null, kind: AnnouncementKinds.Value),
            };
            return vt;
        }

        /// <summary>The region's mutator: its name and what it does, its first tooltip; the keywords it
        /// uses (its other tooltips) in the buffer.</summary>
        public static NodeVtable Mutator(RegionSelectionRewardDisplay display)
        {
            Func<string> name = () => TooltipReader.FirstTitle(display);
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    GameNodes.TooltipPart(() => TooltipReader.FirstBody(display)),
                },
                SearchText = name,
                Details = () => AfterFirst(TooltipReader.Lines(display)),
            };
        }

        // A boss button: its name, its state, its story and power, then any note; landing and Enter
        // select it.
        private static NodeVtable Boss(RegionSelectionButton button, Func<string> name, List<NodeAnnouncement> state, Action choose,
            Func<string> note = null)
        {
            var parts = new List<NodeAnnouncement> { GameNodes.LabelPart(name) };
            parts.AddRange(state);
            parts.Add(GameNodes.TooltipPart(() => TooltipReader.FirstBody(button.tooltipProvider)));
            parts.Add(GameNodes.TooltipPart(() =>
            {
                var lines = TooltipReader.Lines(button.tooltipProvider);
                return lines.Count > 1 ? GameNodes.Sentences(lines.GetRange(1, lines.Count - 1)) : null;
            }));
            if (note != null) parts.Add(GameNodes.TooltipPart(note));
            return new NodeVtable
            {
                ControlType = ControlTypes.Button,
                Announcements = parts,
                SearchText = name,
                OnFocus = choose,
                OnActivate = choose,
                Details = () => AfterFirst(TooltipReader.Lines(button.tooltipProvider)),
            };
        }

        // A widget's tooltips after its first, which its name and description already say.
        private static IEnumerable<string> AfterFirst(List<string> lines)
            => GameNodes.Lines(lines.Count > 1 ? lines.GetRange(1, lines.Count - 1) : new List<string>());

        private static string BossName(RegionSelectionButton button)
        {
            var label = button.bossNameLabel;
            string drawn = label != null ? label.Content() : null;
            return !string.IsNullOrWhiteSpace(drawn) ? drawn : TooltipReader.FirstTitle(button.tooltipProvider);
        }
    }
}
