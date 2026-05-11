using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeCovenant : UIElement, INavigationActionHandler
    {
        private static readonly FieldInfo ExternalButtonField = AccessTools.Field(typeof(global::CovenantSelectionUI), "externalButton")!;
        private static readonly FieldInfo MaxLevelField = AccessTools.Field(typeof(global::CovenantSelectionUI), "maxLevel")!;
        private static readonly FieldInfo EnabledDlcsField = AccessTools.Field(typeof(global::CovenantSelectionUI), "enabledDlcs")!;

        private readonly global::CovenantSelectionUI _covenant;

        public ProxyChallengeCovenant(global::CovenantSelectionUI covenant)
        {
            _covenant = covenant;
        }

        public override bool IsVisible => _covenant != null && _covenant.gameObject.activeInHierarchy;
        public override string GetTypeKey() => IsEditable ? "slider" : null;

        public override Message GetLabel()
        {
            return ChallengePresentation.Covenant(_covenant != null ? _covenant.CurrentLevel : 0);
        }

        public override Message GetTooltip()
        {
            if (_covenant == null)
            {
                return null;
            }

            Message tooltip = ChallengePresentation.CovenantTooltip(_covenant.CurrentLevel, null);
            return tooltip ?? TooltipText.ForComponent(_covenant.transform);
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            LineBuffer uiBuffer = buffers?.GetBuffer("ui");
            if (uiBuffer != null)
            {
                uiBuffer.Clear();
                uiBuffer.Add(GetLabel());
                foreach (Message line in DetailLines())
                {
                    uiBuffer.Add(line);
                }
                buffers.EnableBuffer("ui", true);
            }

            return "ui";
        }

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
            if (!IsEditable)
            {
                return false;
            }

            int current = _covenant.GetLevel();
            int max = (int)MaxLevelField.GetValue(_covenant);
            int next = Mathf.Clamp(current + delta, 0, max);
            if (next == current)
            {
                return true;
            }

            IReadOnlyList<global::ShinyShoe.DLC> enabledDlcs =
                EnabledDlcsField.GetValue(_covenant) as IReadOnlyList<global::ShinyShoe.DLC> ??
                new List<global::ShinyShoe.DLC>();
            _covenant.SetLevel(next, enabledDlcs, max, editable: true);
            _covenant.StateChangedSignal.Dispatch();
            UIManager.ForceReannounceCurrentFocus();
            return true;
        }

        private bool IsEditable
        {
            get
            {
                GameUISelectableButton button = _covenant != null
                    ? ExternalButtonField.GetValue(_covenant) as GameUISelectableButton
                    : null;
                return button != null && button.interactable;
            }
        }

        private IReadOnlyList<Message> DetailLines()
        {
            if (_covenant == null)
            {
                return new List<Message>();
            }

            IReadOnlyList<Message> lines = ChallengePresentation.CovenantTooltipLines(_covenant.CurrentLevel, null);
            if (lines.Count > 0)
            {
                return lines;
            }

            Message fallback = TooltipText.ForComponent(_covenant.transform);
            return fallback != null
                ? new List<Message> { fallback }
                : new List<Message>();
        }
    }
}
