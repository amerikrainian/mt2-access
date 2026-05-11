using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSummaryBattleHistory : UIElement
    {
        private static readonly FieldInfo BattleNameLabelField = AccessTools.Field(typeof(global::BattleHistoryItem), "battleNameLabel")!;
        private static readonly FieldInfo BattleEventLabelField = AccessTools.Field(typeof(global::BattleScoreUI), "eventLabel")!;
        private static readonly FieldInfo BattleScoreLabelField = AccessTools.Field(typeof(global::BattleScoreUI), "scoreLabel")!;

        private readonly global::BattleHistoryItem _item;

        public ProxyRunSummaryBattleHistory(global::BattleHistoryItem item)
        {
            _item = item;
        }

        public override bool IsVisible => _item != null && _item.gameObject.activeInHierarchy && GetLabel() != null;
        public override Message GetLabel()
        {
            TMP_Text name = Get<TMP_Text>(_item, BattleNameLabelField);
            global::BattleScoreUI score = _item?.ScoreEventUI;
            TMP_Text eventLabel = Get<TMP_Text>(score, BattleEventLabelField);
            TMP_Text scoreLabel = Get<TMP_Text>(score, BattleScoreLabelField);
            List<Message> parts = new List<Message>();
            AddTextPart(parts, name);
            AddTextPart(parts, eventLabel);
            AddTextPart(parts, scoreLabel);
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        public override Message GetTooltip()
        {
            global::BattleScoreUI score = _item?.ScoreEventUI;
            if (score == null || score.Tooltips == null || score.Tooltips.Count == 0)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            for (int i = 0; i < score.Tooltips.Count; i++)
            {
                TooltipContent tooltip = score.Tooltips[i];
                if (!string.IsNullOrWhiteSpace(tooltip.body))
                {
                    parts.Add(Message.RawCleaned(tooltip.body));
                }
            }

            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static void AddTextPart(List<Message> parts, TMP_Text text)
        {
            string value = AccessibilityText.ReadLocalizedText(text);
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add(Message.RawCleaned(value));
            }
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
