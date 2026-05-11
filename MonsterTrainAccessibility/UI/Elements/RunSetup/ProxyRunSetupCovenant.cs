using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using MonsterTrainAccessibility.Util;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRunSetupCovenant : GameObjectElement, INavigationActionHandler
    {
        private static readonly FieldInfo MaxLevelField = AccessTools.Field(typeof(global::CovenantSelectionUI), "maxLevel")!;
        private static readonly FieldInfo EnabledDlcsField = AccessTools.Field(typeof(global::CovenantSelectionUI), "enabledDlcs")!;
        private static readonly FieldInfo CovenantUiField = AccessTools.Field(typeof(global::CovenantSelectionUI), "covenantUI")!;
        private static readonly FieldInfo AllGameDataField = AccessTools.Field(typeof(global::ChallengeCovenantUI), "allGameData")!;
        private static readonly FieldInfo TooltipProviderField = AccessTools.Field(typeof(global::ChallengeCovenantUI), "tooltipProvider")!;
        private static readonly FieldInfo TooltipProviderSelectableField = AccessTools.Field(typeof(global::ChallengeCovenantUI), "tooltipProviderSelectable")!;

        private readonly GameUISelectableButton _button;
        private readonly global::CovenantSelectionUI _covenant;
        private readonly Func<Message> _label;

        public ProxyRunSetupCovenant(GameUISelectableButton button, global::CovenantSelectionUI covenant, Func<Message> label)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "slider",
                label: null)
        {
            _button = button;
            _covenant = covenant;
            _label = label;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => _label != null ? _label() : null;
        public override Message GetStatusString() => GameButtonElement.StateMessage(_button);
        public override Message GetTooltip() => CovenantTooltip(_covenant);

        public bool HandleAction(InputAction action)
        {
            switch (action?.Key)
            {
                case "ui_left":
                    return ChangeBy(-1);
                case "ui_right":
                    return ChangeBy(1);
                default:
                    return false;
            }
        }

        private bool ChangeBy(int delta)
        {
            if (_covenant == null)
            {
                return false;
            }

            int current = _covenant.CurrentLevel;
            int max = (int)MaxLevelField.GetValue(_covenant);
            int next = Mathf.Clamp(current + delta, 0, max);
            if (next == current)
            {
                return true;
            }

            _covenant.AscensionLevelChanged.Dispatch(next);
            UIManager.ForceReannounceCurrentFocus();
            return true;
        }

        private static Message CovenantTooltip(global::CovenantSelectionUI covenant)
        {
            global::ChallengeCovenantUI covenantUi = covenant != null
                ? CovenantUiField.GetValue(covenant) as global::ChallengeCovenantUI
                : null;
            if (covenantUi == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            AddProvider(parts, TooltipProviderField.GetValue(covenantUi) as TooltipProviderComponent);
            AddProvider(parts, TooltipProviderSelectableField.GetValue(covenantUi) as TooltipProviderComponent);
            if (parts.Count == 0)
            {
                AddGeneratedCovenantDescriptions(parts, covenant, covenantUi);
            }
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static void AddGeneratedCovenantDescriptions(
            List<Message> parts,
            global::CovenantSelectionUI covenant,
            global::ChallengeCovenantUI covenantUi)
        {
            global::AllGameData allGameData = AllGameDataField.GetValue(covenantUi) as global::AllGameData;
            if (allGameData == null)
            {
                return;
            }

            int level = covenant.CurrentLevel;
            if (level <= 0)
            {
                return;
            }

            global::ChallengeCovenantDisplayData display = allGameData.GetChallengeCovenantDisplayData();
            IReadOnlyList<global::CovenantData> covenants = allGameData.GetAllCovenantsForLevel(level);
            IReadOnlyList<DLC> enabledDlcs = EnabledDlcsField.GetValue(covenant) as IReadOnlyList<DLC>;
            AddTooltipLines(parts, display.GetTooltipContent(covenants, enabledDlcs));
        }

        private static void AddProvider(List<Message> parts, TooltipProviderComponent provider)
        {
            if (parts == null || provider?.Tooltips == null)
            {
                return;
            }

            for (int i = 0; i < provider.Tooltips.Count; i++)
            {
                TooltipContent tooltip = provider.Tooltips[i];
                AddTooltipPart(parts, MessageList.TooltipTitle(tooltip));
                AddTooltipLines(parts, tooltip.body);
            }
        }

        private static void AddTooltipLines(List<Message> parts, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                AddTooltipPart(parts, Message.FromText(lines[i]));
            }
        }

        private static void AddTooltipPart(List<Message> parts, Message message)
        {
            if (message != null)
            {
                MessageList.Add(parts, message);
            }
        }
    }
}
