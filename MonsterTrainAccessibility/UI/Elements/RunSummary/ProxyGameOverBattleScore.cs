using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Screens;
using MonsterTrainAccessibility.Util;
using TMPro;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGameOverBattleScore : UIElement, INavigationTargetElement
    {
        private static readonly FieldInfo BattleEventLabelField = AccessTools.Field(typeof(global::BattleScoreUI), "eventLabel")!;
        private static readonly FieldInfo BattleScoreLabelField = AccessTools.Field(typeof(global::BattleScoreUI), "scoreLabel")!;

        private readonly global::BattleScoreUI _battle;

        public ProxyGameOverBattleScore(global::BattleScoreUI battle)
        {
            _battle = battle;
        }

        public override bool IsVisible => _battle != null && _battle.gameObject.activeInHierarchy && _battle.interactable && ProxyGameOverText.HasMessage(GetLabel());
        public override Message GetLabel()
        {
            List<Message> parts = new List<Message>();
            AddTextPart(parts, Get<TMP_Text>(_battle, BattleEventLabelField));
            AddTextPart(parts, Get<TMP_Text>(_battle, BattleScoreLabelField));
            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        public override Message GetTooltip()
        {
            if (_battle?.Tooltips == null || _battle.Tooltips.Count == 0)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            for (int i = 0; i < _battle.Tooltips.Count; i++)
            {
                Message text = MessageList.Tooltip(_battle.Tooltips[i]);
                if (text != null)
                {
                    parts.Add(text);
                }
            }

            parts = MessageList.Dedupe(parts);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        public void SelectForNavigation()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null);
        }

        public static void AppendSignature(System.Text.StringBuilder sb, global::BattleScoreUI battle)
        {
            sb.Append(battle != null && battle.gameObject.activeInHierarchy).Append(':');
            ProxyGameOverText.AppendSignature(sb, Get<TMP_Text>(battle, BattleEventLabelField));
            ProxyGameOverText.AppendSignature(sb, Get<TMP_Text>(battle, BattleScoreLabelField));
        }

        private static void AddTextPart(List<Message> parts, TMP_Text text)
        {
            Message value = AccessibleScreenText.Text(text);
            if (ProxyGameOverText.HasMessage(value))
            {
                parts.Add(value);
            }
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
