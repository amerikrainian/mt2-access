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
    /// Enemies: a battle intro's (<see cref="Preview"/>), and the Logbook's:
    /// <list type="bullet">
    /// <item>an enemy in the list (<see cref="CharacterButtonUI"/>) is a radio button by the name the
    /// game draws ("???" until discovered), chosen by landing on it through the game's own press, which
    /// loads its details;</item>
    /// <item>a variant button (drawn as a bare numeral) is "Variant 2", "selected" on the shown one;</item>
    /// <item>the details panel (<see cref="CompendiumEnemyDetailsUI"/>) as the game fills it: the name
    /// with the stats beside their icons ("10 attack, 40 health"), the sin's effect for a boss, one
    /// line per ability (the game's own tooltip blocks, title and body), the lore, the artist.</item>
    /// </list>
    /// </summary>
    internal static class EnemyNodes
    {
        public static NodeVtable Enemy(CharacterButtonUI enemy)
        {
            Func<string> name = () => Shown(enemy.nameLabel);
            Action choose = () => { if (!enemy.IsChosen) GameInput.Submit(enemy.Button); };
            return new NodeVtable
            {
                ControlType = ControlTypes.RadioButton,
                Announcements = new List<NodeAnnouncement> { GameNodes.LabelPart(name), GameNodes.SelectedPart(() => enemy.IsChosen) },
                SearchText = name,
                OnFocus = choose,
                OnActivate = choose,
            };
        }

        public static NodeVtable Variant(GameUISelectableButton button, int index)
        {
            Func<string> label = () => Strings.EnemyVariant((index + 1).ToString());
            return GameWidgets.Button(button, label, null,
                GameNodes.SelectedPart(() => button.state == GameUISelectableButton.State.Activated));
        }

        /// <summary>The details panel, one item per part the game shows.</summary>
        public static void Details(GraphBuilder b, string key, CompendiumEnemyDetailsUI details)
        {
            if (details == null || !details.gameObject.activeInHierarchy) return;
            string name = Shown(details.nameLabel);
            if (name == null) return; // still loading
            // One panel: what one line defines is never added again to another's buffer.
            Func<IEnumerable<string>> panel = () => PanelLines(details);
            b.AddItem(ControlId.Structural(key + ":name"), new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(() => Shown(details.nameLabel)),
                    new NodeAnnouncement(() => Stats(details), kind: AnnouncementKinds.Value),
                },
                SearchText = () => Shown(details.nameLabel),
                Panel = panel,
            });
            if (details.sinContainer != null && details.sinContainer.activeInHierarchy)
                Line(b, key + ":sin", () => Shown(details.sinDetailsLabel), panel);
            // An undiscovered enemy's panel repeats "???" in every text: once is enough.
            var blocks = Blocks(Shown(details.detailsLabel));
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] == name) continue;
                int index = i;
                Line(b, key + ":ability:" + i, () => { var now = Blocks(Shown(details.detailsLabel)); return index < now.Count ? now[index] : null; }, panel);
            }
            if (Shown(details.loreLabel) != name) Line(b, key + ":lore", () => Shown(details.loreLabel), panel);
            if (details.artistAttributionRoot != null && details.artistAttributionRoot.activeInHierarchy)
                Line(b, key + ":artist", () => { var artist = Shown(details.artistAttribution); return artist != null ? Strings.EnemyArtist(artist) : null; });
        }

        // The panel's texts, as its lines read them: the sin, each ability block, the lore.
        private static IEnumerable<string> PanelLines(CompendiumEnemyDetailsUI details)
        {
            if (details.sinContainer != null && details.sinContainer.activeInHierarchy) yield return Shown(details.sinDetailsLabel);
            foreach (var block in Blocks(Shown(details.detailsLabel))) yield return block;
            yield return Shown(details.loreLabel);
        }

        // The numbers beside the stat icons, each only while the game shows its icon.
        private static string Stats(CompendiumEnemyDetailsUI details)
        {
            if (details.statsContainer == null || !details.statsContainer.activeInHierarchy) return null;
            var parts = new List<string>();
            if (Active(details.attackContainer)) Add(parts, details.attackDamageLabel, Strings.EnemyAttack);
            if (Active(details.healContainer)) Add(parts, details.healAmountLabel, Strings.EnemyHeal);
            Add(parts, details.healthLabel, Strings.EnemyHealth);
            if (Active(details.armorContainer)) Add(parts, details.armorLabel, Strings.EnemyArmor);
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        private static void Add(List<string> parts, TMP_Text label, Func<string, string> format)
        {
            string value = Shown(label);
            if (value != null) parts.Add(format(value));
        }

        // The abilities text as the game builds it: tooltip blocks (a title line, a body) apart by blank
        // lines. Each block is one line, its title without the icon its name repeats (the Spikes icon
        // before "Spikes"), as every tooltip title is read.
        private static List<string> Blocks(string text)
        {
            var blocks = new List<string>();
            if (text == null) return blocks;
            var lines = new List<string>();
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                if (raw.Trim().Length > 0) { lines.Add(raw.Trim()); continue; }
                Flush(blocks, lines);
            }
            Flush(blocks, lines);
            return blocks;
        }

        private static void Flush(List<string> blocks, List<string> lines)
        {
            if (lines.Count == 0) return;
            string title = GameSprites.WithoutEchoedIcons(lines[0]);
            blocks.Add(lines.Count == 1 ? title : title + ": " + GameNodes.Sentences(lines.GetRange(1, lines.Count - 1)));
            lines.Clear();
        }

        private static void Line(GraphBuilder b, string key, Func<string> text, Func<IEnumerable<string>> panel = null)
        {
            if (string.IsNullOrWhiteSpace(text())) return;
            var vt = GameNodes.Text(text);
            vt.Panel = panel;
            b.AddItem(ControlId.Structural(key), vt);
        }

        private static bool Active(UnityEngine.GameObject root) => root != null && root.activeInHierarchy;

        /// <summary>An enemy a battle's intro shows (<see cref="BattleIntroEnemy"/>), by the game's own
        /// preview of it (<c>TooltipGenerator.GetCharacterPreviewTooltips</c>): "Astreal the Lost, Sap 6.
        /// Relentless. 8 attack 180 health"; the keywords it uses, the rest of the preview, in the
        /// buffer.</summary>
        public static NodeVtable Preview(BattleIntroEnemy enemy)
        {
            var preview = enemy.tooltipProvider;
            Func<string> name = () => TooltipReader.FirstTitle(preview);
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => TooltipReader.FirstBody(preview), kind: AnnouncementKinds.Value),
                },
                SearchText = name,
                // The preview's first tooltip is the line itself: the buffer holds the rest.
                Details = () =>
                {
                    var lines = TooltipReader.Lines(preview);
                    return GameNodes.Lines(lines.Count > 1 ? lines.GetRange(1, lines.Count - 1) : new List<string>());
                },
            };
        }

        private static string Shown(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.Content()) ? text.Content().Trim() : null;
    }
}
