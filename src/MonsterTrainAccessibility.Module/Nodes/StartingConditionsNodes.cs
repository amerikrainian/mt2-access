using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A run's starting conditions as the game lays them out wherever it shows them (a Dimensional
    /// Challenge's details, the run setup): the clans, the pyre heart, the Covenant rank, the mutators.
    /// Read-only lines in the game's words, each with its hover tooltips in the control buffer.
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
            return Line(() => HeaderOf(pyre), () => Commas(
                Text(pyre.titleLabel),
                Stat(Strings.PyreAttack, AttackOf(pyre)),
                pyre.healthUIObject != null && pyre.healthUIObject.activeInHierarchy ? Stat(Strings.PyreHealth, Text(pyre.healthUILabel)) : null), pyre);
        }

        /// <summary>"Covenant Rank 1", the game's own tooltip title, its effects in the buffer.</summary>
        public static NodeVtable Covenant(ChallengeCovenantUI covenant)
        {
            return Line(() =>
            {
                var tips = covenant.TooltipProvider != null ? covenant.TooltipProvider.Tooltips : null;
                string title = tips != null && tips.Count > 0 ? tips[0].title : null;
                return !string.IsNullOrWhiteSpace(title) ? title : Text(covenant.rankLabel);
            }, null, covenant);
        }

        /// <summary>"Highly Reactive", then what it does.</summary>
        public static NodeVtable Mutator(MutatorButtonUI mutator)
        {
            var vt = Line(() => Text(mutator.titleLabel), null, mutator);
            return new NodeVtable
            {
                ControlType = vt.ControlType,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(() => Text(mutator.titleLabel)),
                    GameNodes.TooltipPart(() => Text(mutator.descriptionLabel)),
                },
                SearchText = () => Text(mutator.titleLabel),
                Details = vt.Details,
            };
        }

        // A read-only line: a label, a value, and the widget's tooltips as its details.
        private static NodeVtable Line(System.Func<string> label, System.Func<string> value, Component widget)
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

        // The "Text Header" the game labels these widgets with ("Primary Clan", "Pyre Heart"): no field
        // holds it.
        private static string HeaderOf(Component widget)
        {
            foreach (var text in widget.GetComponentsInChildren<TMP_Text>(false))
                if (text.gameObject.name == "Text Header" && !string.IsNullOrWhiteSpace(text.text)) return text.text;
            return null;
        }

        // The attack number of the pyre heart's ability display.
        private static string AttackOf(PyreHeartInfoUI pyre)
        {
            if (pyre.abilityUI == null || !pyre.abilityUI.gameObject.activeInHierarchy) return null;
            return Text(pyre.abilityUI.GetComponentInChildren<TMP_Text>(false));
        }

        private static string Stat(System.Func<string, string> format, string value)
            => string.IsNullOrWhiteSpace(value) ? null : format(value);

        private static string Commas(params string[] parts)
        {
            var kept = new List<string>();
            foreach (var part in parts) if (!string.IsNullOrWhiteSpace(part)) kept.Add(part);
            return kept.Count > 0 ? string.Join(", ", kept) : null;
        }

        private static string Text(TMP_Text text)
            => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.text) ? text.text : null;
    }
}
