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
    /// A champion upgrade path as the game draws it (<see cref="UpgradeTreeUI"/>): its title as the
    /// context, then one row of its level nodes ("Upgrade level 2, selected"; the game's "Locked" for a
    /// level not yet discovered), then the upgraded champion card the game shows for the selected
    /// level. Landing on a level shows it, as the pointer does (the game sets the level on hover).
    /// </summary>
    internal static class UpgradeTreeNodes
    {
        public static void Tree(GraphBuilder b, string key, UpgradeTreeUI tree)
        {
            if (tree == null || !tree.gameObject.activeInHierarchy) return;
            b.PushContext(tree.titleLabel != null ? tree.titleLabel.text : null, Strings.RoleList);
            // A vertical move into the row lands on the level shown: landing shows a level.
            b.StartRow(entry: () => System.Math.Max(0, tree.currentLevel));
            for (int i = 0; i < tree.levelNodes.Count; i++)
            {
                var node = tree.levelNodes[i];
                if (node == null || !node.gameObject.activeInHierarchy) continue;
                b.AddItem(ControlId.Structural(key + ":level:" + i), Level(tree, node, i));
            }
            var card = tree.cardUI;
            if (card != null && card.IsCardVisible() && card.GetCardState() != null)
                b.AddItem(ControlId.Structural(key + ":card"), CardNodes.Card(card, CardTooltipContainer.LoreTooltipSetting.Always, null));
            b.EndRow();
            b.PopContext();
        }

        private static NodeVtable Level(UpgradeTreeUI tree, UpgradeLevelNode node, int index)
        {
            Func<string> label = () => Strings.UpgradeLevel(index + 1);
            Action show = () =>
            {
                if (Discovered(tree, index) && tree.currentLevel != index) GameInput.Hover(node.Button);
            };
            return new NodeVtable
            {
                ControlType = ControlTypes.RadioButton,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    GameNodes.SelectedPart(() => tree.currentLevel == index),
                    new NodeAnnouncement(() => Discovered(tree, index) ? null : GameText.Term("Locked"), kind: AnnouncementKinds.Enabled),
                },
                SearchText = label,
                OnFocus = show,
                OnActivate = show,
            };
        }

        private static bool Discovered(UpgradeTreeUI tree, int index)
            => index < tree.discoveryStatuses.Count && tree.discoveryStatuses[index];
    }
}
