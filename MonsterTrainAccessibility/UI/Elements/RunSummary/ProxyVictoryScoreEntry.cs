using System.Reflection;
using System.Text;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyVictoryScoreEntry : GameObjectElement
    {
        private static readonly FieldInfo TextContentLabelField = AccessTools.Field(typeof(global::ScoreEntryUI), "textContentLabel")!;
        private static readonly FieldInfo ScoreValueLabelField = AccessTools.Field(typeof(global::ScoreEntryUI), "scoreValueLabel")!;
        private static readonly FieldInfo TooltipProviderField = AccessTools.Field(typeof(global::ScoreEntryUI), "tooltipProvider")!;

        private readonly global::ScoreEntryUI _entry;

        public ProxyVictoryScoreEntry(global::ScoreEntryUI entry)
            : base(
                target: entry != null ? entry.gameObject : null,
                label: null)
        {
            _entry = entry;
        }

        public override bool IsVisible => _entry != null && _entry.gameObject.activeInHierarchy;
        public override Message GetLabel() => Label(_entry);
        public override Message GetTooltip() => _entry != null ? AccessibleScreenText.Tooltip(_entry) : null;

        public bool HasContent
        {
            get
            {
                string label = GetLabel()?.Resolve();
                string tooltip = GetTooltip()?.Resolve();
                return !string.IsNullOrWhiteSpace(label) || !string.IsNullOrWhiteSpace(tooltip);
            }
        }

        public GameObject LabelTarget => TextLabel != null ? TextLabel.gameObject : null;
        public GameObject ValueTarget => ValueLabel != null ? ValueLabel.gameObject : null;
        public GameObject TooltipTarget
        {
            get
            {
                TooltipProviderComponent tooltipProvider = _entry != null ? TooltipProviderField.GetValue(_entry) as TooltipProviderComponent : null;
                return tooltipProvider != null ? tooltipProvider.gameObject : null;
            }
        }

        private TMP_Text TextLabel => _entry != null ? TextContentLabelField.GetValue(_entry) as TMP_Text : null;
        private TMP_Text ValueLabel => _entry != null ? ScoreValueLabelField.GetValue(_entry) as TMP_Text : null;

        public static void AppendSignature(StringBuilder sb, global::ScoreEntryUI entry)
        {
            if (sb == null || entry == null || !entry.gameObject.activeInHierarchy)
            {
                return;
            }

            TMP_Text text = TextContentLabelField.GetValue(entry) as TMP_Text;
            TMP_Text value = ScoreValueLabelField.GetValue(entry) as TMP_Text;
            sb.Append("score:")
                .Append(AccessibilityText.ReadLocalizedText(text))
                .Append(':')
                .Append(AccessibilityText.ReadLocalizedText(value))
                .Append('|');
        }

        private static Message Label(global::ScoreEntryUI entry)
        {
            if (entry == null)
            {
                return null;
            }

            string label = AccessibilityText.ReadLocalizedText(TextContentLabelField.GetValue(entry) as TMP_Text);
            string value = AccessibilityText.ReadLocalizedText(ScoreValueLabelField.GetValue(entry) as TMP_Text);
            Message name = Message.FromText(label);
            Message status = Message.FromText(value);
            if (name == null)
            {
                return status;
            }

            return status != null ? Message.Join(", ", name, status) : name;
        }
    }
}
