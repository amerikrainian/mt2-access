using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Interop;
using MonsterTrainAccessibility.Module.UI;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A soul in the Soulforge (<see cref="SoulSelectionItemUI"/>): its name as the item draws it, the
    /// game's "Locked" while it is, its tier while the item marks one ("Tier 2"; the first tier carries no
    /// mark), then its description and the progress the game shows for it (the unlock, or the next tier:
    /// "Eat units to unlock. 0/15", <see cref="ProgressionReader"/>). Landing hovers it, as the mouse
    /// does, which is what fills the game's soul panel. Its buffer holds its tooltips, a soul being an
    /// artifact to the game (<c>TooltipGenerator.GetRelicTooltips</c>).
    /// </summary>
    internal static class SoulNodes
    {
        public static NodeVtable Soul(SoulSelectionItemUI item, global::SoulforgeScreen screen)
        {
            System.Func<string> name = () => Name(item);
            System.Action hover = () => GameInput.Hover(item.Button);
            return new NodeVtable
            {
                ControlType = ControlTypes.Item,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => item.IsLocked ? GameText.Term("Locked") : null, kind: AnnouncementKinds.Enabled),
                    new NodeAnnouncement(() => Tier(item), kind: AnnouncementKinds.Value),
                    GameNodes.TooltipPart(() => item.SoulData != null ? item.SoulData.GetDescription() : null),
                    GameNodes.TooltipPart(() => Progress(item, screen)),
                },
                SearchText = name,
                OnFocus = hover,
                OnActivate = hover,
                Details = () => GameNodes.Lines(TooltipReader.Lines(Tooltips(item))),
            };
        }

        private static string Name(SoulSelectionItemUI item)
        {
            string drawn = item.soulNameLabel != null ? item.soulNameLabel.Content() : null;
            return !string.IsNullOrWhiteSpace(drawn) ? drawn : item.SoulState?.GetName();
        }

        // The item marks the second and third tiers only.
        private static string Tier(SoulSelectionItemUI item)
        {
            int tier = item.SoulData != null ? item.SoulData.GetTierLevel() : 0;
            return tier > 1 ? string.Format(GameText.Term("SoulTier_Format"), tier) : null;
        }

        // The game's panel says what unlocks the hovered soul, or its next tier; read while it shows this one.
        private static string Progress(SoulSelectionItemUI item, global::SoulforgeScreen screen)
        {
            var panel = screen.progressionObjectiveUI;
            if (panel == null || !panel.gameObject.activeSelf || screen.soulNameLabel == null || screen.soulNameLabel.Content() != Name(item)) return null;
            return ProgressionReader.Line(panel);
        }

        private static List<TooltipContent> Tooltips(SoulSelectionItemUI item)
        {
            var tooltips = new List<TooltipContent>();
            if (item.SoulState == null) return tooltips;
            var managers = AllGameManagers.Instance;
            TooltipGenerator.GetRelicTooltips(tooltips, item.SoulState, managers?.GetStatusEffectManager(), managers?.GetRelicManager(),
                TooltipGenerator.OptionalFlags.Lore, managers?.GetSaveManager());
            return tooltips;
        }
    }
}
